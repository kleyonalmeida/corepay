using System.Text.Json;
using BuildingBlocks.Pagination;
using BuildingBlocks.Results;
using Core.Application.Collaborators;
using Core.Application.Notifications;
using Core.Application.Payrolls;
using Core.Auth;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Payrolls;

public sealed class PayrollStore : IPayrollStore
{
    private readonly AppDbContext _dbContext;
    private readonly PayrollCalculator _calculator;
    private readonly INotificationStore _notificationStore;
    private readonly TimeProvider _timeProvider;
    private readonly EntryCalculationContextLoader _contextLoader;

    public PayrollStore(
        AppDbContext dbContext,
        PayrollCalculator calculator,
        INotificationStore notificationStore,
        TimeProvider timeProvider,
        EntryCalculationContextLoader contextLoader)
    {
        _dbContext = dbContext;
        _calculator = calculator;
        _notificationStore = notificationStore;
        _timeProvider = timeProvider;
        _contextLoader = contextLoader;
    }

    public async Task<Result<Payroll>> SaveAsync(Payroll payroll, CancellationToken cancellationToken = default)
    {
        var departmentExists = await _dbContext.Departments
            .AnyAsync(d => d.Id == payroll.DepartmentId, cancellationToken);
        if (!departmentExists)
        {
            return Result<Payroll>.Failure(
                Error.NotFound("payrolls.department_not_found", "Department not found."));
        }

        var competenceDuplicate = await _dbContext.Payrolls.AnyAsync(
            p => p.DepartmentId == payroll.DepartmentId
                 && p.Month == payroll.Month
                 && p.Year == payroll.Year
                 && p.Id != payroll.Id,
            cancellationToken);
        if (competenceDuplicate)
        {
            return Result<Payroll>.Failure(
                Error.Conflict("payrolls.competence_duplicate", "A payroll already exists for this department, month and year."));
        }

        var existing = await _dbContext.Payrolls
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == payroll.Id, cancellationToken);

        if (existing is null)
        {
            _dbContext.Payrolls.Add(payroll);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(payroll);
            SyncEntries(existing, payroll, _dbContext);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueCompetenceViolation(ex))
        {
            return Result<Payroll>.Failure(
                Error.Conflict("payrolls.competence_duplicate", "A payroll already exists for this department, month and year."));
        }

        return await GetByIdAsync(payroll.Id, cancellationToken);
    }

    public async Task<Result<Payroll>> GetByIdAsync(Guid payrollId, CancellationToken cancellationToken = default)
    {
        var payroll = await _dbContext.Payrolls
            .AsNoTracking()
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);

        if (payroll is null)
        {
            return Result<Payroll>.Failure(
                Error.NotFound("payrolls.not_found", "Payroll not found."));
        }

        return Result<Payroll>.Success(payroll);
    }

    public async Task<Result<PayrollsListResponse>> GetPayrollsAsync(
        PayrollListFilters filters,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        if (filters.Month is < 1 or > 12)
        {
            return Result<PayrollsListResponse>.Failure(
                Error.Validation("payrolls.invalid_month", "Month must be between 1 and 12."));
        }

        if (filters.Year is < 2000 or > 2100)
        {
            return Result<PayrollsListResponse>.Failure(
                Error.Validation("payrolls.invalid_year", "Year is out of allowed range."));
        }

        var scopeResult = ResolveDepartmentScope(filters.DepartmentId, access);
        if (scopeResult.IsFailure)
        {
            return Result<PayrollsListResponse>.Failure(scopeResult.Error!);
        }

        var query = BuildListQuery(scopeResult.Value!, filters);
        var totalCount = await query.CountAsync(cancellationToken);
        var (page, pageSize, skip) = Pagination.Normalize(filters.Page, filters.PageSize);

        var items = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ThenBy(p => p.Department.Name)
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new PayrollListItemResponse(
                p.Id,
                p.DepartmentId,
                p.Department.Name,
                p.Month,
                p.Year,
                p.Status,
                p.TotalAmount,
                _dbContext.PayrollCollaboratorEntries.Count(e => e.PayrollId == p.Id),
                p.SubmittedBy))
            .ToListAsync(cancellationToken);

        return Result<PayrollsListResponse>.Success(
            new PayrollsListResponse(items, totalCount, page, pageSize));
    }

    public async Task<Result<PayrollSummaryResponse>> GetSummaryByIdAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payroll = await _dbContext.Payrolls
            .AsNoTracking()
            .Include(p => p.Department)
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);

        if (payroll is null)
        {
            return Result<PayrollSummaryResponse>.Failure(
                Error.NotFound("payrolls.not_found", "Payroll not found."));
        }

        if (!CanAccessDepartment(payroll.DepartmentId, access))
        {
            return Result<PayrollSummaryResponse>.Failure(
                Error.Forbidden("payrolls.department_forbidden", "Payroll is outside your department scope."));
        }

        return Result<PayrollSummaryResponse>.Success(MapSummary(payroll));
    }

    public async Task<Result<PayrollSummaryResponse>> DuplicateToNextMonthAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var source = await _dbContext.Payrolls
            .AsNoTracking()
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);

        if (source is null)
        {
            return Result<PayrollSummaryResponse>.Failure(
                Error.NotFound("payrolls.not_found", "Payroll not found."));
        }

        var sourceEntriesValidation = ValidatePayrollHasEntries(source.Entries.Count);
        if (sourceEntriesValidation.IsFailure)
        {
            return Result<PayrollSummaryResponse>.Failure(sourceEntriesValidation.Error!);
        }

        if (!CanAccessDepartment(source.DepartmentId, access))
        {
            return Result<PayrollSummaryResponse>.Failure(
                Error.Forbidden("payrolls.department_forbidden", "Payroll is outside your department scope."));
        }

        var (targetMonth, targetYear) = GetNextCompetence(source.Month, source.Year);

        var duplicateExists = await _dbContext.Payrolls.AnyAsync(
            p => p.DepartmentId == source.DepartmentId
                 && p.Month == targetMonth
                 && p.Year == targetYear,
            cancellationToken);
        if (duplicateExists)
        {
            return Result<PayrollSummaryResponse>.Failure(
                Error.Conflict("payrolls.competence_duplicate", "A payroll already exists for this department, month and year."));
        }

        var duplicate = new Payroll
        {
            Id = Guid.NewGuid(),
            DepartmentId = source.DepartmentId,
            Month = targetMonth,
            Year = targetYear,
            Status = PayrollStatus.Draft,
            TotalAmount = source.TotalAmount,
            RejectionComment = null,
            SubmittedBy = null,
            ApprovedBy = null,
            ApprovedAt = null,
            Entries = source.Entries.Select(entry => new PayrollCollaboratorEntry
            {
                Id = Guid.NewGuid(),
                CollaboratorId = entry.CollaboratorId,
                CollaboratorName = entry.CollaboratorName,
                PixKey = entry.PixKey,
                AdmissionDate = entry.AdmissionDate,
                CareerLevelName = entry.CareerLevelName,
                CalculationProfile = entry.CalculationProfile,
                DepartmentId = entry.DepartmentId,
                CareerLevelId = entry.CareerLevelId,
                FullBaseSalary = entry.FullBaseSalary,
                GoalTier = entry.GoalTier,
                FinalSalary = entry.FinalSalary,
                BetanoInternaCount = entry.BetanoInternaCount,
                BetanoMundoBetCount = entry.BetanoMundoBetCount,
                SupervisorAnalystRevenue = entry.SupervisorAnalystRevenue,
                CommissionPayingProjectId = entry.CommissionPayingProjectId,
                TrafficSeniorLevelId = entry.TrafficSeniorLevelId,
                IsApproved = false,
                IsPaid = false,
                NfSent = false,
                Payload = ClonePayload(entry.Payload)
            }).ToList()
        };

        var saveResult = await SaveAsync(duplicate, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<PayrollSummaryResponse>.Failure(saveResult.Error!);
        }

        return await GetSummaryByIdAsync(duplicate.Id, access, cancellationToken);
    }

    public async Task<Result<PayrollFormOptionsResponse>> GetFormOptionsAsync(
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Departments.AsNoTracking().AsQueryable();

        if (CollaboratorAccessResolver.IsDepartmentScoped(access.Roles))
        {
            var allowed = access.AllowedDepartmentIds ?? [];
            query = query.Where(d => allowed.Contains(d.Id));
        }

        var departments = await query
            .OrderBy(d => d.Name)
            .Select(d => new PayrollFormDepartmentOption(d.Id, d.Name))
            .ToListAsync(cancellationToken);

        return Result<PayrollFormOptionsResponse>.Success(new PayrollFormOptionsResponse(departments));
    }

    public async Task<Result<PayrollDetailResponse>> CreatePayrollAsync(
        CreatePayrollRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateCompetence(request.Month, request.Year);
        if (validationResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(validationResult.Error!);
        }

        if (!CanAccessDepartment(request.DepartmentId, access))
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Forbidden("payrolls.department_forbidden", "Department is outside your scope."));
        }

        var departmentExists = await _dbContext.Departments
            .AnyAsync(d => d.Id == request.DepartmentId, cancellationToken);
        if (!departmentExists)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.NotFound("payrolls.department_not_found", "Department not found."));
        }

        if (request.CollaboratorIds is null || request.CollaboratorIds.Count == 0)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Validation(
                    "payrolls.entries_required",
                    "Payroll must include at least one collaborator entry."));
        }

        var payrollId = Guid.NewGuid();
        var payroll = new Payroll
        {
            Id = payrollId,
            DepartmentId = request.DepartmentId,
            Month = request.Month,
            Year = request.Year,
            Status = PayrollStatus.Draft,
            TotalAmount = 0m
        };

        var entriesResult = await BuildEntriesForCollaboratorsAsync(
            payroll,
            request.CollaboratorIds,
            cancellationToken);
        if (entriesResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(entriesResult.Error!);
        }

        payroll.Entries = entriesResult.Value!;

        var saveResult = await SaveAsync(payroll, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(saveResult.Error!);
        }

        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollDetailResponse>> GetDetailByIdAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payroll = await _dbContext.Payrolls
            .AsNoTracking()
            .Include(p => p.Department)
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);

        if (payroll is null)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.NotFound("payrolls.not_found", "Payroll not found."));
        }

        if (!CanAccessDepartment(payroll.DepartmentId, access))
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Forbidden("payrolls.department_forbidden", "Payroll is outside your department scope."));
        }

        var editorOptions = await LoadEditorOptionsAsync(access, cancellationToken);
        var detailResult = await MapDetailAsync(payroll, access, editorOptions, cancellationToken);
        if (detailResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(detailResult.Error!);
        }

        return Result<PayrollDetailResponse>.Success(detailResult.Value!);
    }

    public async Task<Result<PayrollDetailResponse>> UpdatePayrollShellAsync(
        Guid payrollId,
        UpdatePayrollRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payroll = await _dbContext.Payrolls
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);

        if (payroll is null)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.NotFound("payrolls.not_found", "Payroll not found."));
        }

        if (!CanAccessDepartment(payroll.DepartmentId, access))
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Forbidden("payrolls.department_forbidden", "Payroll is outside your department scope."));
        }

        if (payroll.Status is not PayrollStatus.Draft and not PayrollStatus.Rejected)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Conflict("payrolls.status_not_editable", "Payroll cannot be edited in its current status."));
        }

        var hideApprovedFromManager = ShouldHideApprovedEntries(payroll, access);
        var protectedCollaboratorIds = hideApprovedFromManager
            ? payroll.Entries.Where(e => e.IsApproved).Select(e => e.CollaboratorId).ToHashSet()
            : [];

        var requestedIds = request.CollaboratorIds.ToHashSet();
        if (hideApprovedFromManager && requestedIds.Overlaps(protectedCollaboratorIds))
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Forbidden("payrolls.approved_entry_readonly", "Approved entries cannot be modified."));
        }

        var targetCollaboratorIds = hideApprovedFromManager
            ? protectedCollaboratorIds.Union(requestedIds).ToHashSet()
            : requestedIds;

        var removableEntries = payroll.Entries
            .Where(e => !targetCollaboratorIds.Contains(e.CollaboratorId))
            .Where(e => !hideApprovedFromManager || !e.IsApproved)
            .ToList();

        foreach (var entry in removableEntries)
        {
            payroll.Entries.Remove(entry);
            _dbContext.PayrollCollaboratorEntries.Remove(entry);
        }

        var existingCollaboratorIds = payroll.Entries.Select(e => e.CollaboratorId).ToHashSet();
        var collaboratorsToAdd = targetCollaboratorIds
            .Where(id => !existingCollaboratorIds.Contains(id))
            .ToList();

        var newEntriesResult = await BuildEntriesForCollaboratorsAsync(
            payroll,
            collaboratorsToAdd,
            cancellationToken);
        if (newEntriesResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(newEntriesResult.Error!);
        }

        foreach (var entry in newEntriesResult.Value!)
        {
            entry.PayrollId = payroll.Id;
            payroll.Entries.Add(entry);
            _dbContext.PayrollCollaboratorEntries.Add(entry);
        }

        if (request.Entries is not null)
        {
            var applyResult = ApplyEntryUpdates(payroll, request.Entries, access);
            if (applyResult.IsFailure)
            {
                return Result<PayrollDetailResponse>.Failure(applyResult.Error!);
            }
        }

        var entriesValidation = ValidatePayrollHasEntries(payroll.Entries.Count);
        if (entriesValidation.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(entriesValidation.Error!);
        }

        var recalcResult = await RecalculatePayrollTotalAsync(payroll, cancellationToken);
        if (recalcResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(recalcResult.Error!);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueCollaboratorViolation(ex))
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Conflict(
                    "payrolls.collaborator_already_in_payroll",
                    "Collaborator is already included in this payroll."));
        }

        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollDetailResponse>> SubmitPayrollAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payroll = await _dbContext.Payrolls
            .Include(p => p.Department)
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);

        if (payroll is null)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.NotFound("payrolls.not_found", "Payroll not found."));
        }

        if (!CanAccessDepartment(payroll.DepartmentId, access))
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Forbidden("payrolls.department_forbidden", "Payroll is outside your department scope."));
        }

        if (payroll.Status is not PayrollStatus.Draft and not PayrollStatus.Rejected)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Conflict("payrolls.status_not_editable", "Payroll cannot be edited in its current status."));
        }

        var entriesValidation = ValidatePayrollHasEntries(payroll.Entries.Count);
        if (entriesValidation.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(entriesValidation.Error!);
        }

        var recalcResult = await RecalculatePayrollTotalAsync(payroll, cancellationToken);
        if (recalcResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(recalcResult.Error!);
        }

        payroll.Status = PayrollStatus.PendingApproval;
        payroll.SubmittedByUserId = access.UserId;
        payroll.SubmittedBy = string.IsNullOrWhiteSpace(access.DisplayName)
            ? access.UserId
            : access.DisplayName;

        var createdAt = _timeProvider.GetUtcNow();
        _notificationStore.StagePayrollSubmitted(payroll, createdAt);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollDetailResponse>> ApprovePayrollAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payrollResult = await LoadPayrollForMutationAsync(payrollId, access, cancellationToken);
        if (payrollResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payrollResult.Error!);
        }

        var payroll = payrollResult.Value!;
        var statusResult = PayrollWorkflowTransitions.ValidateCanApprove(payroll.Status);
        if (statusResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(statusResult.Error!);
        }

        var entriesValidation = ValidatePayrollHasEntries(payroll.Entries.Count);
        if (entriesValidation.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(entriesValidation.Error!);
        }

        var recalcResult = await RecalculatePayrollTotalAsync(payroll, cancellationToken);
        if (recalcResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(recalcResult.Error!);
        }

        PayrollWorkflowTransitions.ApprovePayroll(
            payroll,
            ResolveActorDisplayName(access),
            _timeProvider.GetUtcNow());

        _notificationStore.StagePayrollApproved(payroll, _timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollDetailResponse>> RejectPayrollAsync(
        Guid payrollId,
        RejectPayrollRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var commentResult = PayrollWorkflowTransitions.ValidateRejectionComment(request.RejectionComment);
        if (commentResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(commentResult.Error!);
        }

        var payrollResult = await LoadPayrollForMutationAsync(payrollId, access, cancellationToken);
        if (payrollResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payrollResult.Error!);
        }

        var payroll = payrollResult.Value!;
        var statusResult = PayrollWorkflowTransitions.ValidateCanReject(payroll.Status);
        if (statusResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(statusResult.Error!);
        }

        PayrollWorkflowTransitions.RejectPayroll(payroll, request.RejectionComment);
        _notificationStore.StagePayrollRejected(payroll, _timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollDetailResponse>> RecalculatePayrollAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payrollResult = await LoadPayrollForMutationAsync(payrollId, access, cancellationToken);
        if (payrollResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payrollResult.Error!);
        }

        var payroll = payrollResult.Value!;
        var statusResult = PayrollWorkflowTransitions.ValidateCanRecalculate(payroll.Status);
        if (statusResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(statusResult.Error!);
        }

        var recalcResult = await RecalculatePayrollTotalAsync(payroll, cancellationToken);
        if (recalcResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(recalcResult.Error!);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result> DeletePayrollAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payroll = await _dbContext.Payrolls
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);

        if (payroll is null)
        {
            return Result.Failure(
                Error.NotFound("payrolls.not_found", "Payroll not found."));
        }

        if (!CanAccessDepartment(payroll.DepartmentId, access))
        {
            return Result.Failure(
                Error.Forbidden("payrolls.department_forbidden", "Payroll is outside your department scope."));
        }

        _dbContext.Payrolls.Remove(payroll);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PayrollDetailResponse>> PayPayrollAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payrollResult = await LoadPayrollForMutationAsync(payrollId, access, cancellationToken);
        if (payrollResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payrollResult.Error!);
        }

        var payroll = payrollResult.Value!;
        var payResult = PayrollWorkflowTransitions.ValidateCanPay(payroll.Status);
        if (payResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payResult.Error!);
        }

        if (payroll.Entries.Count == 0)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Validation("payrolls.no_entries", "Payroll has no entries to mark as paid."));
        }

        PayrollWorkflowTransitions.PayAllEntries(payroll);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollDetailResponse>> ApproveEntryAsync(
        Guid payrollId,
        Guid entryId,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payrollResult = await LoadPayrollForMutationAsync(payrollId, access, cancellationToken);
        if (payrollResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payrollResult.Error!);
        }

        var payroll = payrollResult.Value!;
        var entry = FindEntry(payroll, entryId);
        if (entry is null)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.NotFound("payrolls.entry_not_found", "Payroll entry not found."));
        }

        entry.IsApproved = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollDetailResponse>> PayEntryAsync(
        Guid payrollId,
        Guid entryId,
        SetEntryPaidRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payrollResult = await LoadPayrollForMutationAsync(payrollId, access, cancellationToken);
        if (payrollResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payrollResult.Error!);
        }

        var payroll = payrollResult.Value!;
        var payResult = PayrollWorkflowTransitions.ValidateCanPay(payroll.Status);
        if (payResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payResult.Error!);
        }

        var entry = FindEntry(payroll, entryId);
        if (entry is null)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.NotFound("payrolls.entry_not_found", "Payroll entry not found."));
        }

        entry.IsPaid = request.IsPaid;
        PayrollWorkflowTransitions.SyncPayrollPaidStatus(payroll);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollDetailResponse>> SetEntryNfAsync(
        Guid payrollId,
        Guid entryId,
        SetEntryNfRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payrollResult = await LoadPayrollForMutationAsync(payrollId, access, cancellationToken);
        if (payrollResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payrollResult.Error!);
        }

        var payroll = payrollResult.Value!;
        var payResult = PayrollWorkflowTransitions.ValidateCanPay(payroll.Status);
        if (payResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payResult.Error!);
        }

        var entry = FindEntry(payroll, entryId);
        if (entry is null)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.NotFound("payrolls.entry_not_found", "Payroll entry not found."));
        }

        entry.NfSent = request.NfSent;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollDetailResponse>> AddCollaboratorEntryAsync(
        Guid payrollId,
        AddCollaboratorEntryRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        if (!HasPermission(access, AppPermissions.PayrollsWrite))
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Forbidden("payrolls.write_forbidden", "You do not have permission to edit payrolls."));
        }

        var payrollResult = await LoadPayrollForMutationAsync(payrollId, access, cancellationToken);
        if (payrollResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payrollResult.Error!);
        }

        var payroll = payrollResult.Value!;
        if (payroll.Status == PayrollStatus.PendingApproval)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Conflict("payrolls.status_not_editable", "Payroll cannot be edited in its current status."));
        }

        var entriesResult = await BuildEntriesForCollaboratorsAsync(
            payroll,
            [request.CollaboratorId],
            cancellationToken,
            isApproved: true);
        if (entriesResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(entriesResult.Error!);
        }

        var newEntry = entriesResult.Value!.Single();
        var calcResult = await CalculateEntryAsync(
            payroll,
            newEntry,
            PayrollEntryInputMapper.ToPreviewRequest(newEntry),
            cancellationToken);
        if (calcResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(calcResult.Error!);
        }

        var (result, projectTotals) = calcResult.Value!;
        newEntry.Payload.CalculatedResult = result;
        newEntry.Payload.DisplayProjectTotals = projectTotals.ToList();
        newEntry.PayrollId = payroll.Id;
        payroll.Entries.Add(newEntry);
        _dbContext.PayrollCollaboratorEntries.Add(newEntry);
        payroll.TotalAmount += result.TotalAmount;
        PayrollWorkflowTransitions.SyncPayrollPaidStatus(payroll);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueCollaboratorViolation(ex))
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Conflict(
                    "payrolls.collaborator_already_in_payroll",
                    "Collaborator is already included in this payroll."));
        }

        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollDetailResponse>> UpdateEntryAsync(
        Guid payrollId,
        Guid entryId,
        UpdatePayrollEntryRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        if (request.EntryId != entryId)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Validation("payrolls.entry_id_mismatch", "Entry id in route and body must match."));
        }

        var payrollResult = await LoadPayrollForMutationAsync(payrollId, access, cancellationToken);
        if (payrollResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(payrollResult.Error!);
        }

        var payroll = payrollResult.Value!;
        var entry = FindEntry(payroll, entryId);
        if (entry is null)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.NotFound("payrolls.entry_not_found", "Payroll entry not found."));
        }

        if (ShouldHideApprovedEntries(payroll, access) && entry.IsApproved)
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Forbidden("payrolls.approved_entry_readonly", "Approved entries cannot be modified."));
        }

        if (payroll.Status is PayrollStatus.Draft or PayrollStatus.Rejected)
        {
            if (!HasPermission(access, AppPermissions.PayrollsWrite))
            {
                return Result<PayrollDetailResponse>.Failure(
                    Error.Forbidden("payrolls.write_forbidden", "You do not have permission to edit payrolls."));
            }

            PayrollEntryInputMapper.ApplyRequest(entry, PayrollEntryInputMapper.ToPreviewRequest(request));
        }
        else if (payroll.Status == PayrollStatus.Approved)
        {
            if (!HasPermission(access, AppPermissions.PayrollsPay))
            {
                return Result<PayrollDetailResponse>.Failure(
                    Error.Forbidden("payrolls.pay_forbidden", "You do not have permission to adjust approved payrolls."));
            }

            ApplyPostApprovalAdjustments(entry, request);
        }
        else
        {
            return Result<PayrollDetailResponse>.Failure(
                Error.Conflict("payrolls.status_not_editable", "Payroll cannot be edited in its current status."));
        }

        var recalcResult = await RecalculatePayrollTotalAsync(payroll, cancellationToken);
        if (recalcResult.IsFailure)
        {
            return Result<PayrollDetailResponse>.Failure(recalcResult.Error!);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDetailByIdAsync(payrollId, access, cancellationToken);
    }

    public async Task<Result<PayrollEntryPreviewResponse>> PreviewEntryAsync(
        Guid payrollId,
        Guid entryId,
        PreviewPayrollEntryRequest request,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var payroll = await _dbContext.Payrolls
            .AsNoTracking()
            .Include(p => p.Department)
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);

        if (payroll is null)
        {
            return Result<PayrollEntryPreviewResponse>.Failure(
                Error.NotFound("payrolls.not_found", "Payroll not found."));
        }

        if (!CanAccessDepartment(payroll.DepartmentId, access))
        {
            return Result<PayrollEntryPreviewResponse>.Failure(
                Error.Forbidden("payrolls.department_forbidden", "Payroll is outside your department scope."));
        }

        if (payroll.Status is not PayrollStatus.Draft and not PayrollStatus.Rejected)
        {
            return Result<PayrollEntryPreviewResponse>.Failure(
                Error.Conflict("payrolls.status_not_editable", "Payroll cannot be edited in its current status."));
        }

        var entry = FindEntry(payroll, entryId);
        if (entry is null)
        {
            return Result<PayrollEntryPreviewResponse>.Failure(
                Error.NotFound("payrolls.entry_not_found", "Payroll entry not found."));
        }

        if (ShouldHideApprovedEntries(payroll, access) && entry.IsApproved)
        {
            return Result<PayrollEntryPreviewResponse>.Failure(
                Error.Forbidden("payrolls.approved_entry_readonly", "Approved entries cannot be modified."));
        }

        var calcResult = await CalculateEntryAsync(payroll, entry, request, cancellationToken);
        if (calcResult.IsFailure)
        {
            return Result<PayrollEntryPreviewResponse>.Failure(calcResult.Error!);
        }

        var (result, projectTotals) = calcResult.Value!;
        return Result<PayrollEntryPreviewResponse>.Success(
            new PayrollEntryPreviewResponse(
                MapEntryResult(result),
                projectTotals
                    .Select(t => new PayrollProjectTotalResponse(t.ProjectId, t.Amount, t.BaseSalary, t.Commission, t.GoalBonus, t.ManualBonus, t.Other))
                    .ToList()));
    }

    private IQueryable<Payroll> BuildListQuery(DepartmentScope scope, PayrollListFilters filters)
    {
        var query = _dbContext.Payrolls
            .AsNoTracking()
            .Include(p => p.Department)
            .AsQueryable();

        if (scope.DepartmentId is not null)
        {
            query = query.Where(p => p.DepartmentId == scope.DepartmentId.Value);
        }
        else if (scope.AllowedDepartmentIds is not null)
        {
            query = query.Where(p => scope.AllowedDepartmentIds.Contains(p.DepartmentId));
        }

        if (filters.Month is not null)
        {
            query = query.Where(p => p.Month == filters.Month.Value);
        }

        if (filters.Year is not null)
        {
            query = query.Where(p => p.Year == filters.Year.Value);
        }

        if (filters.Status is not null)
        {
            query = query.Where(p => p.Status == filters.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.Department.Name, $"%{term}%")
                || p.Entries.Any(e => EF.Functions.Like(e.CollaboratorName, $"%{term}%")));
        }

        return query;
    }

    private static Result<DepartmentScope> ResolveDepartmentScope(
        Guid? departmentId,
        PayrollAccessContext access)
    {
        if (!CollaboratorAccessResolver.IsDepartmentScoped(access.Roles))
        {
            return Result<DepartmentScope>.Success(new DepartmentScope(departmentId, null));
        }

        var allowed = access.AllowedDepartmentIds ?? [];
        if (allowed.Count == 0)
        {
            return Result<DepartmentScope>.Success(new DepartmentScope(null, []));
        }

        if (departmentId is not null && !allowed.Contains(departmentId.Value))
        {
            return Result<DepartmentScope>.Failure(
                Error.Forbidden("payrolls.department_forbidden", "Department is outside your scope."));
        }

        return Result<DepartmentScope>.Success(
            departmentId is not null
                ? new DepartmentScope(departmentId, null)
                : new DepartmentScope(null, allowed));
    }

    private static bool CanAccessDepartment(Guid departmentId, PayrollAccessContext access)
    {
        if (!CollaboratorAccessResolver.IsDepartmentScoped(access.Roles))
        {
            return true;
        }

        return access.AllowedDepartmentIds?.Contains(departmentId) == true;
    }

    private static (int Month, int Year) GetNextCompetence(int month, int year) =>
        month == 12 ? (1, year + 1) : (month + 1, year);

    private static PayrollCollaboratorEntryPayload ClonePayload(PayrollCollaboratorEntryPayload payload)
    {
        var json = JsonSerializer.Serialize(payload, PayrollJsonOptions.Instance);
        return JsonSerializer.Deserialize<PayrollCollaboratorEntryPayload>(json, PayrollJsonOptions.Instance)
               ?? new PayrollCollaboratorEntryPayload();
    }

    private static PayrollSummaryResponse MapSummary(Payroll payroll) =>
        new(
            payroll.Id,
            payroll.DepartmentId,
            payroll.Department.Name,
            payroll.Month,
            payroll.Year,
            payroll.Status,
            payroll.TotalAmount,
            payroll.Entries.Count,
            payroll.SubmittedBy);

    private async Task<Result<PayrollDetailResponse>> MapDetailAsync(
        Payroll payroll,
        PayrollAccessContext access,
        PayrollEditorOptionsResponse editorOptions,
        CancellationToken cancellationToken)
    {
        var entries = payroll.Entries.AsEnumerable();
        if (ShouldHideApprovedEntries(payroll, access))
        {
            entries = entries.Where(e => !e.IsApproved);
        }

        await _contextLoader.EnsureSharedLoadedAsync(cancellationToken);
        await _contextLoader.PreloadCollaboratorsAsync(
            entries.Select(entry => entry.CollaboratorId),
            cancellationToken);

        var mappedEntries = new List<PayrollEntryEditorResponse>();
        foreach (var entry in entries.OrderBy(e => e.CollaboratorName))
        {
            var resolved = await ResolveEntryDisplayAsync(payroll, entry, cancellationToken);
            if (resolved.IsFailure)
            {
                return Result<PayrollDetailResponse>.Failure(resolved.Error!);
            }

            var (result, projectTotals) = resolved.Value!;
            mappedEntries.Add(PayrollEntryInputMapper.MapEditorEntry(entry, result, projectTotals));
        }

        var allowedActions = PayrollCapabilitiesEvaluator.Evaluate(payroll.Status, access);

        return Result<PayrollDetailResponse>.Success(new PayrollDetailResponse(
            payroll.Id,
            payroll.DepartmentId,
            payroll.Department.Name,
            payroll.Month,
            payroll.Year,
            payroll.Status,
            payroll.TotalAmount,
            payroll.Entries.Count,
            payroll.SubmittedBy,
            payroll.RejectionComment,
            payroll.ApprovedBy,
            payroll.ApprovedAt,
            mappedEntries,
            editorOptions,
            allowedActions));
    }

    private async Task<Result<Payroll>> LoadPayrollForMutationAsync(
        Guid payrollId,
        PayrollAccessContext access,
        CancellationToken cancellationToken)
    {
        var payroll = await _dbContext.Payrolls
            .Include(p => p.Department)
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == payrollId, cancellationToken);

        if (payroll is null)
        {
            return Result<Payroll>.Failure(
                Error.NotFound("payrolls.not_found", "Payroll not found."));
        }

        if (!CanAccessDepartment(payroll.DepartmentId, access))
        {
            return Result<Payroll>.Failure(
                Error.Forbidden("payrolls.department_forbidden", "Payroll is outside your department scope."));
        }

        return Result<Payroll>.Success(payroll);
    }

    private static string ResolveActorDisplayName(PayrollAccessContext access) =>
        string.IsNullOrWhiteSpace(access.DisplayName) ? access.UserId : access.DisplayName;

    private static bool HasPermission(PayrollAccessContext access, string permissionKey)
    {
        if (access.Roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return access.Permissions?.Contains(permissionKey, StringComparer.OrdinalIgnoreCase) == true;
    }

    private static void ApplyPostApprovalAdjustments(
        PayrollCollaboratorEntry entry,
        UpdatePayrollEntryRequest request)
    {
        entry.Payload.BonusEntries = request.BonusEntries?.ToList() ?? [];
        entry.Payload.DeductionEntries = request.DeductionEntries?.ToList() ?? [];
    }

    private async Task<Result<(PayrollEntryResultResponse? Result, IReadOnlyList<PayrollProjectTotalResponse>? ProjectTotals)>>
        ResolveEntryDisplayAsync(
            Payroll payroll,
            PayrollCollaboratorEntry entry,
            CancellationToken cancellationToken)
    {
        if (UsesPersistedSnapshotOnly(payroll.Status))
        {
            return Result<(PayrollEntryResultResponse?, IReadOnlyList<PayrollProjectTotalResponse>?)>.Success(
                MapStoredSnapshot(entry));
        }

        if (entry.Payload.CalculatedResult is not null)
        {
            return Result<(PayrollEntryResultResponse?, IReadOnlyList<PayrollProjectTotalResponse>?)>.Success(
                MapStoredSnapshot(entry));
        }

        var calcResult = await CalculateEntryAsync(
            payroll,
            entry,
            PayrollEntryInputMapper.ToPreviewRequest(entry),
            cancellationToken);
        if (calcResult.IsFailure)
        {
            return Result<(PayrollEntryResultResponse?, IReadOnlyList<PayrollProjectTotalResponse>?)>.Failure(
                calcResult.Error!);
        }

        var (result, projectTotals) = calcResult.Value!;
        return Result<(PayrollEntryResultResponse?, IReadOnlyList<PayrollProjectTotalResponse>?)>.Success((
            MapEntryResult(result),
            projectTotals
                .Select(t => new PayrollProjectTotalResponse(t.ProjectId, t.Amount, t.BaseSalary, t.Commission, t.GoalBonus, t.ManualBonus, t.Other))
                .ToList()));
    }

    private static (PayrollEntryResultResponse?, IReadOnlyList<PayrollProjectTotalResponse>?) MapStoredSnapshot(
        PayrollCollaboratorEntry entry)
    {
        if (entry.Payload.CalculatedResult is null)
        {
            return (null, null);
        }

        return (
            MapEntryResult(entry.Payload.CalculatedResult),
            entry.Payload.DisplayProjectTotals
                .Select(t => new PayrollProjectTotalResponse(t.ProjectId, t.Amount, t.BaseSalary, t.Commission, t.GoalBonus, t.ManualBonus, t.Other))
                .ToList());
    }

    private static bool UsesPersistedSnapshotOnly(PayrollStatus status) =>
        status is PayrollStatus.PendingApproval or PayrollStatus.Approved or PayrollStatus.Paid;

    private async Task<Result<(PayrollEntryResult Result, IReadOnlyList<ProjectTotalAllocation> ProjectTotals)>>
        CalculateEntryAsync(
            Payroll payroll,
            PayrollCollaboratorEntry entry,
            PreviewPayrollEntryRequest request,
            CancellationToken cancellationToken)
    {
        var contextResult = await _contextLoader.LoadForEntryAsync(entry, payroll, cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result<(PayrollEntryResult, IReadOnlyList<ProjectTotalAllocation>)>.Failure(contextResult.Error!);
        }

        var context = contextResult.Value!;
        var inputResult = PayrollEntryInputMapper.Map(
            payroll,
            entry,
            context.Collaborator,
            context.Department,
            context.CareerLevel,
            context.TrafficSeniorLevel,
            context.ActiveProjectsById,
            context.DepartmentsById,
            context.CareerLevelsById,
            request);
        if (inputResult.IsFailure)
        {
            return Result<(PayrollEntryResult, IReadOnlyList<ProjectTotalAllocation>)>.Failure(inputResult.Error!);
        }

        var calcResult = _calculator.CalcEntry(inputResult.Value!);
        if (calcResult.IsFailure)
        {
            return Result<(PayrollEntryResult, IReadOnlyList<ProjectTotalAllocation>)>.Failure(calcResult.Error!);
        }

        var affiliatesProjectId = _contextLoader.AffiliatesProjectId;
        var displayResult = _calculator.GetDisplayProjectEntries(inputResult.Value!, affiliatesProjectId);
        if (displayResult.IsFailure)
        {
            return Result<(PayrollEntryResult, IReadOnlyList<ProjectTotalAllocation>)>.Failure(displayResult.Error!);
        }

        return Result<(PayrollEntryResult, IReadOnlyList<ProjectTotalAllocation>)>.Success(
            (calcResult.Value!, displayResult.Value!));
    }

    private async Task<PayrollEditorOptionsResponse> LoadEditorOptionsAsync(
        PayrollAccessContext access,
        CancellationToken cancellationToken)
    {
        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new PayrollEditorProjectOption(
                p.Id,
                p.Name,
                p.Platform,
                p.ExcludesGoalBonus,
                p.IsDefaultAllocationTarget,
                p.ExcludesSupervisorFixedAllocation))
            .ToListAsync(cancellationToken);

        var departmentQuery = _dbContext.Departments
            .AsNoTracking()
            .Where(d => d.IsActive);

        if (CollaboratorAccessResolver.IsDepartmentScoped(access.Roles))
        {
            var allowed = access.AllowedDepartmentIds ?? [];
            departmentQuery = departmentQuery.Where(d => allowed.Contains(d.Id));
        }

        var departments = await departmentQuery
            .OrderBy(d => d.Name)
            .Select(d => new PayrollEditorDepartmentOption(d.Id, d.Name))
            .ToListAsync(cancellationToken);

        var allowedDepartmentIds = departments.Select(d => d.Id).ToHashSet();
        var careerLevelQuery = _dbContext.CareerLevels
            .AsNoTracking()
            .Where(c => c.IsActive);

        if (CollaboratorAccessResolver.IsDepartmentScoped(access.Roles))
        {
            careerLevelQuery = careerLevelQuery.Where(c =>
                c.DepartmentId == null || allowedDepartmentIds.Contains(c.DepartmentId.Value));
        }

        var careerLevels = await careerLevelQuery
            .OrderBy(c => c.Name)
            .Select(c => new PayrollEditorCareerLevelOption(
                c.Id,
                c.Name,
                c.DepartmentId,
                c.Profile))
            .ToListAsync(cancellationToken);

        return new PayrollEditorOptionsResponse(projects, departments, careerLevels);
    }

    private static PayrollEntryResultResponse MapEntryResult(PayrollEntryResult result) =>
        new(
            result.TotalAmount,
            result.BaseSalary,
            result.CommissionAmount,
            result.GoalBonusAmount,
            result.GroupCommissionAmount,
            result.PlatformTotal);

    private static bool ShouldHideApprovedEntries(Payroll payroll, PayrollAccessContext access) =>
        payroll.Status == PayrollStatus.Rejected
        && CollaboratorAccessResolver.IsDepartmentScoped(access.Roles);

    private static Result ApplyEntryUpdates(
        Payroll payroll,
        IReadOnlyList<UpdatePayrollEntryRequest> entries,
        PayrollAccessContext access)
    {
        var hideApprovedFromManager = ShouldHideApprovedEntries(payroll, access);
        var entryIds = entries.Select(e => e.EntryId).ToList();
        if (entryIds.Distinct().Count() != entryIds.Count)
        {
            return Result.Failure(
                Error.Validation("payrolls.entry_duplicate", "Duplicate entry ids in request."));
        }

        foreach (var entryRequest in entries)
        {
            var entry = FindEntry(payroll, entryRequest.EntryId);
            if (entry is null)
            {
                return Result.Failure(
                    Error.NotFound("payrolls.entry_not_found", "Payroll entry not found."));
            }

            if (hideApprovedFromManager && entry.IsApproved)
            {
                return Result.Failure(
                    Error.Forbidden("payrolls.approved_entry_readonly", "Approved entries cannot be modified."));
            }

            PayrollEntryInputMapper.ApplyRequest(entry, PayrollEntryInputMapper.ToPreviewRequest(entryRequest));
        }

        return Result.Success();
    }

    private static PayrollCollaboratorEntry? FindEntry(Payroll payroll, Guid entryId) =>
        payroll.Entries.FirstOrDefault(entry =>
            entry.Id == entryId && entry.PayrollId == payroll.Id);

    private async Task<Result<decimal>> RecalculatePayrollTotalAsync(
        Payroll payroll,
        CancellationToken cancellationToken)
    {
        await _contextLoader.EnsureSharedLoadedAsync(cancellationToken);
        await _contextLoader.PreloadCollaboratorsAsync(
            payroll.Entries.Select(entry => entry.CollaboratorId),
            cancellationToken);

        decimal totalAmount = 0m;
        foreach (var entry in payroll.Entries.OrderBy(e => e.CollaboratorName))
        {
            var calcResult = await CalculateEntryAsync(
                payroll,
                entry,
                PayrollEntryInputMapper.ToPreviewRequest(entry),
                cancellationToken);
            if (calcResult.IsFailure)
            {
                return Result<decimal>.Failure(calcResult.Error!);
            }

            var (result, projectTotals) = calcResult.Value!;
            entry.Payload.CalculatedResult = result;
            entry.Payload.DisplayProjectTotals = projectTotals.ToList();
            totalAmount += result.TotalAmount;
        }

        payroll.TotalAmount = totalAmount;
        return Result<decimal>.Success(payroll.TotalAmount);
    }

    private static Result ValidateCompetence(int month, int year)
    {
        if (month is < 1 or > 12)
        {
            return Result.Failure(
                Error.Validation("payrolls.invalid_month", "Month must be between 1 and 12."));
        }

        if (year is < 2000 or > 2100)
        {
            return Result.Failure(
                Error.Validation("payrolls.invalid_year", "Year is out of allowed range."));
        }

        return Result.Success();
    }

    private static Result ValidatePayrollHasEntries(int entryCount)
    {
        if (entryCount == 0)
        {
            return Result.Failure(
                Error.Validation(
                    "payrolls.entries_required",
                    "Payroll must include at least one collaborator entry."));
        }

        return Result.Success();
    }

    private async Task<Result<List<PayrollCollaboratorEntry>>> BuildEntriesForCollaboratorsAsync(
        Payroll payroll,
        IReadOnlyList<Guid> collaboratorIds,
        CancellationToken cancellationToken,
        bool isApproved = false)
    {
        if (collaboratorIds.Count == 0)
        {
            return Result<List<PayrollCollaboratorEntry>>.Success([]);
        }

        var distinctIds = collaboratorIds.Distinct().ToList();
        if (distinctIds.Count != collaboratorIds.Count)
        {
            return Result<List<PayrollCollaboratorEntry>>.Failure(
                Error.Validation("payrolls.collaborator_duplicate", "Duplicate collaborator ids in request."));
        }

        var collaborators = await _dbContext.Collaborators
            .Include(c => c.Department)
            .Include(c => c.CareerLevel)
            .Where(c => distinctIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (collaborators.Count != distinctIds.Count)
        {
            return Result<List<PayrollCollaboratorEntry>>.Failure(
                Error.NotFound("payrolls.collaborator_not_found", "One or more collaborators were not found."));
        }

        var entries = new List<PayrollCollaboratorEntry>();
        foreach (var collaboratorId in distinctIds)
        {
            var collaborator = collaborators.Single(c => c.Id == collaboratorId);

            if (collaborator.DepartmentId != payroll.DepartmentId)
            {
                return Result<List<PayrollCollaboratorEntry>>.Failure(
                    Error.Validation(
                        "payrolls.collaborator_not_in_department",
                        "Collaborator does not belong to the payroll department."));
            }

            if (!collaborator.IsActive)
            {
                return Result<List<PayrollCollaboratorEntry>>.Failure(
                    Error.Validation(
                        "payrolls.collaborator_inactive",
                        "Inactive collaborators cannot be added to a payroll."));
            }

            if (payroll.Entries.Any(e => e.CollaboratorId == collaboratorId))
            {
                return Result<List<PayrollCollaboratorEntry>>.Failure(
                    Error.Conflict(
                        "payrolls.collaborator_already_in_payroll",
                        "Collaborator is already included in this payroll."));
            }

            entries.Add(PayrollEntrySnapshotBuilder.Build(
                collaborator,
                collaborator.Department,
                collaborator.CareerLevel,
                isApproved));
        }

        return Result<List<PayrollCollaboratorEntry>>.Success(entries);
    }

    private static void SyncEntries(Payroll existing, Payroll updated, AppDbContext dbContext)
    {
        var updatedIds = updated.Entries.Select(e => e.Id).ToHashSet();
        var toRemove = existing.Entries.Where(e => !updatedIds.Contains(e.Id)).ToList();
        foreach (var entry in toRemove)
        {
            dbContext.PayrollCollaboratorEntries.Remove(entry);
        }

        foreach (var entry in updated.Entries)
        {
            var tracked = existing.Entries.FirstOrDefault(e => e.Id == entry.Id);
            if (tracked is null)
            {
                entry.PayrollId = existing.Id;
                existing.Entries.Add(entry);
            }
            else
            {
                dbContext.Entry(tracked).CurrentValues.SetValues(entry);
                tracked.Payload = entry.Payload;
            }
        }
    }

    private static bool IsUniqueCompetenceViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("IX_Payrolls_DepartmentId_Month_Year", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsUniqueCollaboratorViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("IX_PayrollCollaboratorEntries_PayrollId_CollaboratorId", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;

    private sealed record DepartmentScope(Guid? DepartmentId, IReadOnlyList<Guid>? AllowedDepartmentIds);
}

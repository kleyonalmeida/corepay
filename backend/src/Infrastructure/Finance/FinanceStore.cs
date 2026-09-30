using BuildingBlocks.Results;
using Core.Application.Collaborators;
using Core.Application.Finance;
using Core.Application.Payrolls;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using Infrastructure.MasterData;
using Infrastructure.Payrolls;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Finance;

public sealed class FinanceStore : IFinanceStore
{
    private readonly AppDbContext _dbContext;
    private readonly PayrollCalculator _calculator;
    private readonly EntryCalculationContextLoader _contextLoader;
    private readonly MasterDataCache _masterDataCache;

    public FinanceStore(
        AppDbContext dbContext,
        PayrollCalculator calculator,
        EntryCalculationContextLoader contextLoader,
        MasterDataCache masterDataCache)
    {
        _dbContext = dbContext;
        _calculator = calculator;
        _contextLoader = contextLoader;
        _masterDataCache = masterDataCache;
    }

    public async Task<Result<FinanceSummaryResponse>> GetSummaryAsync(
        FinanceSummaryFilters filters,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        if (filters.Month is < 1 or > 12)
        {
            return Result<FinanceSummaryResponse>.Failure(
                Error.Validation("finance.invalid_month", "Month must be between 1 and 12."));
        }

        if (filters.Year is < 2000 or > 2100)
        {
            return Result<FinanceSummaryResponse>.Failure(
                Error.Validation("finance.invalid_year", "Year is out of allowed range."));
        }

        var scopeResult = ResolveDepartmentScope(filters.DepartmentId, access);
        if (scopeResult.IsFailure)
        {
            return Result<FinanceSummaryResponse>.Failure(scopeResult.Error!);
        }

        var query = BuildPayrollQuery(scopeResult.Value!, filters);
        var payrolls = await query.ToListAsync(cancellationToken);

        await _contextLoader.EnsureSharedLoadedAsync(cancellationToken);
        await _contextLoader.PreloadCollaboratorsAsync(
            payrolls.SelectMany(payroll => payroll.Entries.Select(entry => entry.CollaboratorId)),
            cancellationToken);

        var groups = new List<FinancePayrollGroupResponse>();
        foreach (var payroll in payrolls)
        {
            if (!FinanceEntryVisibility.PayrollHasFinanceContent(
                    payroll.Status,
                    payroll.Entries.Select(e => e.IsApproved)))
            {
                continue;
            }

            var visibleEntries = new List<FinanceEntryResponse>();
            foreach (var entry in payroll.Entries.OrderBy(e => e.CollaboratorName))
            {
                if (!FinanceEntryVisibility.IsEntryVisible(payroll.Status, entry.IsApproved))
                {
                    continue;
                }

                var resolved = await ResolveEntryDisplayAsync(payroll, entry, cancellationToken);
                if (resolved.IsFailure)
                {
                    return Result<FinanceSummaryResponse>.Failure(resolved.Error!);
                }

                var (result, projectTotals) = resolved.Value!;
                if (filters.ProjectId is Guid projectId
                    && (projectTotals is null || projectTotals.All(t => t.ProjectId != projectId)))
                {
                    continue;
                }

                var amounts = FinanceEntryAmounts.FromResultResponse(result);

                visibleEntries.Add(new FinanceEntryResponse(
                    entry.Id,
                    entry.CollaboratorId,
                    entry.CollaboratorName,
                    entry.CareerLevelName,
                    entry.PixKey,
                    amounts.TotalAmount,
                    amounts.PlatformTotal,
                    amounts.AmountToReceive,
                    entry.IsApproved,
                    entry.IsPaid,
                    entry.NfSent,
                    projectTotals ?? []));
            }

            if (visibleEntries.Count == 0)
            {
                continue;
            }

            var entryAmounts = visibleEntries.Select(e =>
                new FinanceEntryAmounts(
                    e.TotalAmount,
                    e.PlatformTotal,
                    e.AmountToReceive,
                    e.IsPaid));
            var aggregate = FinanceEntryAmounts.AggregateGroup(entryAmounts);
            var allowedActions = PayrollCapabilitiesEvaluator.Evaluate(payroll.Status, access);

            groups.Add(new FinancePayrollGroupResponse(
                payroll.Id,
                payroll.DepartmentId,
                payroll.Department.Name,
                payroll.Month,
                payroll.Year,
                payroll.Status,
                aggregate.GrossTotal,
                aggregate.PlatformTotal,
                aggregate.AmountToReceive,
                aggregate.PaidAmount,
                aggregate.PaidCount,
                aggregate.EntryCount,
                allowedActions,
                visibleEntries));
        }

        var filterOptions = await LoadFilterOptionsAsync(cancellationToken);
        return Result<FinanceSummaryResponse>.Success(new FinanceSummaryResponse(groups, filterOptions));
    }

    private IQueryable<Payroll> BuildPayrollQuery(DepartmentScope scope, FinanceSummaryFilters filters)
    {
        var query = _dbContext.Payrolls
            .AsNoTracking()
            .Include(p => p.Department)
            .Include(p => p.Entries)
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

        return query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ThenBy(p => p.Department.Name);
    }

    private async Task<FinanceFilterOptionsResponse> LoadFilterOptionsAsync(CancellationToken cancellationToken)
    {
        var departments = (await _masterDataCache.GetActiveDepartmentsAsync(cancellationToken))
            .Select(department => new FinanceDepartmentOption(department.Id, department.Name))
            .ToList();

        var projects = (await _masterDataCache.GetActiveProjectsAsync(cancellationToken))
            .Select(project => new FinanceProjectOption(project.Id, project.Name))
            .ToList();

        return new FinanceFilterOptionsResponse(departments, projects);
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

    private static (PayrollEntryResultResponse? Result, IReadOnlyList<PayrollProjectTotalResponse>? ProjectTotals)
        MapStoredSnapshot(PayrollCollaboratorEntry entry)
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

    private static PayrollEntryResultResponse MapEntryResult(PayrollEntryResult result) =>
        new(
            result.TotalAmount,
            result.BaseSalary,
            result.CommissionAmount,
            result.GoalBonusAmount,
            result.GroupCommissionAmount,
            result.PlatformTotal);

    private static bool UsesPersistedSnapshotOnly(PayrollStatus status) =>
        status is PayrollStatus.PendingApproval or PayrollStatus.Approved or PayrollStatus.Paid;

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
                Error.Forbidden("finance.department_forbidden", "Department is outside your scope."));
        }

        return Result<DepartmentScope>.Success(
            departmentId is not null
                ? new DepartmentScope(departmentId, null)
                : new DepartmentScope(null, allowed));
    }

    private sealed record DepartmentScope(Guid? DepartmentId, IReadOnlyList<Guid>? AllowedDepartmentIds);
}

using BuildingBlocks.Pagination;
using BuildingBlocks.Results;
using Core.Application.Cashflow;
using Core.Domain;
using Core.Security;
using Infrastructure.MasterData;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Cashflow;

public sealed class CashflowStore : ICashflowStore
{
    private const string SemProjetoLabel = "Sem projeto";
    private const int NotesMaxLength = 2048;
    private const int RequesterMaxLength = 256;
    private const int PurchaseLocationMaxLength = 256;
    private const int AttachmentUrlMaxLength = 1024;
    private const int MaxInstallmentTotal = 60;

    private readonly AppDbContext _dbContext;
    private readonly MasterDataCache _masterDataCache;

    public CashflowStore(AppDbContext dbContext, MasterDataCache masterDataCache)
    {
        _dbContext = dbContext;
        _masterDataCache = masterDataCache;
    }

    public async Task<Result<CashflowListResponse>> GetListAsync(
        CashflowListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateListFilters(filters);
        if (validation.IsFailure)
        {
            return Result<CashflowListResponse>.Failure(validation.Error!);
        }

        var query = BuildFilteredQuery(filters);
        var totalCount = await query.CountAsync(cancellationToken);

        var (responsePage, responsePageSize, skip) = Pagination.Normalize(filters.Page, filters.PageSize);

        var items = await query
            .OrderByDescending(entry => entry.Year)
            .ThenByDescending(entry => entry.Month)
            .ThenByDescending(entry => entry.TransactionDate)
            .ThenBy(entry => entry.Type)
            .Skip(skip)
            .Take(responsePageSize)
            .Select(entry => MapResponse(entry))
            .ToListAsync(cancellationToken);

        var entradas = await query
            .Where(entry => entry.Type == ProjectCostType.Entrada)
            .SumAsync(entry => entry.Amount, cancellationToken);
        var saidas = await query
            .Where(entry => entry.Type == ProjectCostType.Saida)
            .SumAsync(entry => entry.Amount, cancellationToken);

        var summary = new CashflowSummaryResponse(
            entradas,
            saidas,
            entradas - saidas,
            totalCount);

        var filterOptions = filters.IncludeFilterOptions
            ? await BuildFilterOptionsAsync(cancellationToken)
            : new CashflowFilterOptionsResponse([], [], []);

        return Result<CashflowListResponse>.Success(
            new CashflowListResponse(
                summary,
                items,
                filterOptions,
                totalCount,
                responsePage,
                responsePageSize));
    }

    public async Task<Result<CashflowEntryResponse>> GetByIdAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await QueryWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == entryId, cancellationToken);

        if (entry is null)
        {
            return Result<CashflowEntryResponse>.Failure(
                Error.NotFound("cashflow.not_found", "Cashflow entry not found."));
        }

        return Result<CashflowEntryResponse>.Success(MapResponse(entry));
    }

    public async Task<Result<CashflowCreateResponse>> CreateAsync(
        CreateCashflowEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var installmentTotal = request.InstallmentTotal <= 0 ? 1 : request.InstallmentTotal;
        if (installmentTotal > MaxInstallmentTotal)
        {
            return Result<CashflowCreateResponse>.Failure(
                Error.Validation("cashflow.invalid_installment_total", "Installment total exceeds allowed maximum."));
        }

        if (request.Type != ProjectCostType.Saida && installmentTotal > 1)
        {
            return Result<CashflowCreateResponse>.Failure(
                Error.Validation("cashflow.installments_saida_only", "Installments are only supported for exit entries."));
        }

        var validation = await ValidateMutationAsync(
            request.Type,
            request.Category,
            request.Amount,
            request.TransactionDate,
            request.Month,
            request.Year,
            request.ProjectId,
            request.PaymentMethodId,
            request.DepartmentId,
            request.Requester,
            request.PurchaseLocation,
            request.AttachmentUrl,
            request.Notes,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result<CashflowCreateResponse>.Failure(validation.Error!);
        }

        var compraId = installmentTotal > 1 ? Guid.NewGuid() : (Guid?)null;
        var plan = CashflowInstallmentPlanner.PlanInstallments(
            request.Amount,
            installmentTotal,
            request.TransactionDate,
            request.Month,
            request.Year);

        var createdEntries = new List<ProjectCost>(installmentTotal);
        foreach (var installment in plan)
        {
            createdEntries.Add(new ProjectCost
            {
                Id = Guid.NewGuid(),
                Type = request.Type,
                Category = request.Category,
                Amount = installment.Amount,
                TransactionDate = installment.TransactionDate,
                Month = installment.Month,
                Year = installment.Year,
                ProjectId = request.ProjectId,
                DepartmentId = request.DepartmentId,
                PaymentMethodId = request.PaymentMethodId,
                Requester = NormalizeOptionalText(request.Requester),
                PurchaseLocation = NormalizeOptionalText(request.PurchaseLocation),
                InstallmentNumber = installmentTotal > 1 ? installment.InstallmentNumber : null,
                InstallmentTotal = installmentTotal > 1 ? installmentTotal : null,
                CompraId = compraId,
                AttachmentUrl = NormalizeAttachmentUrl(request.AttachmentUrl),
                Notes = NormalizeOptionalText(request.Notes)
            });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _dbContext.ProjectCosts.AddRange(createdEntries);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await LoadNavigationPropertiesAsync(createdEntries, cancellationToken);

        var responses = createdEntries.Select(MapResponse).ToList();
        return Result<CashflowCreateResponse>.Success(
            new CashflowCreateResponse(responses[0], responses));
    }

    public async Task<Result<CashflowEntryResponse>> UpdateAsync(
        Guid entryId,
        UpdateCashflowEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = await QueryWithIncludes()
            .FirstOrDefaultAsync(c => c.Id == entryId, cancellationToken);

        if (entry is null)
        {
            return Result<CashflowEntryResponse>.Failure(
                Error.NotFound("cashflow.not_found", "Cashflow entry not found."));
        }

        var validation = await ValidateMutationAsync(
            request.Type,
            request.Category,
            request.Amount,
            request.TransactionDate,
            request.Month,
            request.Year,
            request.ProjectId,
            request.PaymentMethodId,
            request.DepartmentId,
            request.Requester,
            request.PurchaseLocation,
            request.AttachmentUrl,
            request.Notes,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result<CashflowEntryResponse>.Failure(validation.Error!);
        }

        entry.Type = request.Type;
        entry.Category = request.Category;
        entry.Amount = request.Amount;
        entry.TransactionDate = request.TransactionDate;
        entry.Month = request.Month;
        entry.Year = request.Year;
        entry.ProjectId = request.ProjectId;
        entry.DepartmentId = request.DepartmentId;
        entry.PaymentMethodId = request.PaymentMethodId;
        entry.Requester = NormalizeOptionalText(request.Requester);
        entry.PurchaseLocation = NormalizeOptionalText(request.PurchaseLocation);
        entry.AttachmentUrl = NormalizeAttachmentUrl(request.AttachmentUrl);
        entry.Notes = NormalizeOptionalText(request.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await LoadNavigationPropertiesAsync([entry], cancellationToken);

        return Result<CashflowEntryResponse>.Success(MapResponse(entry));
    }

    public async Task<Result> DeleteAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.ProjectCosts
            .FirstOrDefaultAsync(c => c.Id == entryId, cancellationToken);

        if (entry is null)
        {
            return Result.Failure(
                Error.NotFound("cashflow.not_found", "Cashflow entry not found."));
        }

        _dbContext.ProjectCosts.Remove(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<CashflowReportResponse>> GetReportAsync(
        CashflowReportFilters filters,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateReportFilters(filters);
        if (validation.IsFailure)
        {
            return Result<CashflowReportResponse>.Failure(validation.Error!);
        }

        var query = _dbContext.ProjectCosts
            .AsNoTracking()
            .Where(c => c.Month == filters.Month && c.Year == filters.Year);

        var entradas = await query
            .Where(c => c.Type == ProjectCostType.Entrada)
            .SumAsync(c => c.Amount, cancellationToken);
        var saidas = await query
            .Where(c => c.Type == ProjectCostType.Saida)
            .SumAsync(c => c.Amount, cancellationToken);
        var count = await query.CountAsync(cancellationToken);

        var summary = new CashflowSummaryResponse(
            entradas,
            saidas,
            entradas - saidas,
            count);

        var byProjectRaw = await query
            .GroupBy(c => c.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                TotalEntradas = g.Sum(c => c.Type == ProjectCostType.Entrada ? c.Amount : 0m),
                TotalSaidas = g.Sum(c => c.Type == ProjectCostType.Saida ? c.Amount : 0m),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var projectIds = byProjectRaw
            .Where(g => g.ProjectId is not null)
            .Select(g => g.ProjectId!.Value)
            .Distinct()
            .ToList();

        var projectNames = projectIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Projects
                .AsNoTracking()
                .Where(p => projectIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var byProject = byProjectRaw
            .Select(g => new CashflowReportProjectRowResponse(
                g.ProjectId,
                g.ProjectId is null
                    ? SemProjetoLabel
                    : projectNames.GetValueOrDefault(g.ProjectId.Value, SemProjetoLabel),
                g.TotalEntradas,
                g.TotalSaidas,
                g.TotalEntradas - g.TotalSaidas,
                g.Count))
            .OrderBy(g => g.ProjectName)
            .ToList();

        var byPaymentMethodRaw = await query
            .Where(c => c.Type == ProjectCostType.Saida && c.PaymentMethodId != null)
            .GroupBy(c => c.PaymentMethodId)
            .Select(g => new
            {
                PaymentMethodId = g.Key!.Value,
                TotalSaidas = g.Sum(c => c.Amount),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var paymentMethodIds = byPaymentMethodRaw.Select(g => g.PaymentMethodId).ToList();
        var paymentMethodNames = paymentMethodIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.PaymentMethods
                .AsNoTracking()
                .Where(p => paymentMethodIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var byPaymentMethod = byPaymentMethodRaw
            .Select(g => new CashflowReportPaymentMethodRowResponse(
                g.PaymentMethodId,
                paymentMethodNames.GetValueOrDefault(g.PaymentMethodId, "—"),
                g.TotalSaidas,
                g.Count))
            .OrderBy(g => g.PaymentMethodName)
            .ToList();

        return Result<CashflowReportResponse>.Success(
            new CashflowReportResponse(summary, byProject, byPaymentMethod));
    }

    public async Task<Result<FacilitiesCashflowWebhookResponse>> CreateFromFacilitiesWebhookAsync(
        FacilitiesCashflowWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ProjectCosts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.FacilitiesLancamentoId == request.LancamentoId, cancellationToken);

        if (existing is not null)
        {
            return Result<FacilitiesCashflowWebhookResponse>.Success(
                new FacilitiesCashflowWebhookResponse(existing.Id, request.LancamentoId, Idempotent: true));
        }

        var dados = request.Dados;
        var validation = await ValidateMutationAsync(
            dados.Tipo,
            dados.Categoria,
            dados.Valor,
            dados.DataLancamento,
            dados.DataLancamento.Month,
            dados.DataLancamento.Year,
            dados.ProjectId,
            dados.PaymentMethodId,
            dados.DepartmentId,
            dados.Solicitante,
            dados.LocalCompra,
            dados.AttachmentUrl,
            dados.Notes,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result<FacilitiesCashflowWebhookResponse>.Failure(validation.Error!);
        }

        var entry = new ProjectCost
        {
            Id = Guid.NewGuid(),
            Type = dados.Tipo,
            Category = dados.Categoria,
            Amount = dados.Valor,
            TransactionDate = dados.DataLancamento,
            Month = dados.DataLancamento.Month,
            Year = dados.DataLancamento.Year,
            ProjectId = dados.ProjectId,
            DepartmentId = dados.DepartmentId,
            PaymentMethodId = dados.PaymentMethodId,
            Requester = NormalizeOptionalText(dados.Solicitante),
            PurchaseLocation = NormalizeOptionalText(dados.LocalCompra),
            AttachmentUrl = NormalizeAttachmentUrl(dados.AttachmentUrl),
            Notes = NormalizeOptionalText(dados.Notes),
            FacilitiesLancamentoId = request.LancamentoId
        };

        _dbContext.ProjectCosts.Add(entry);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var winner = await _dbContext.ProjectCosts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.FacilitiesLancamentoId == request.LancamentoId, cancellationToken);

            if (winner is not null)
            {
                return Result<FacilitiesCashflowWebhookResponse>.Success(
                    new FacilitiesCashflowWebhookResponse(winner.Id, request.LancamentoId, Idempotent: true));
            }

            throw;
        }

        return Result<FacilitiesCashflowWebhookResponse>.Success(
            new FacilitiesCashflowWebhookResponse(entry.Id, request.LancamentoId, Idempotent: false));
    }

    public async Task<Result<IReadOnlyList<CashflowEntryResponse>>> GetInstallmentGroupAsync(
        Guid compraId,
        CancellationToken cancellationToken = default)
    {
        var items = await QueryWithIncludes()
            .AsNoTracking()
            .Where(c => c.CompraId == compraId)
            .OrderBy(c => c.InstallmentNumber)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return Result<IReadOnlyList<CashflowEntryResponse>>.Failure(
                Error.NotFound("cashflow.installment_group_not_found", "Installment group not found."));
        }

        return Result<IReadOnlyList<CashflowEntryResponse>>.Success(
            items.Select(MapResponse).ToList());
    }

    private IQueryable<ProjectCost> BuildFilteredQuery(CashflowListFilters filters)
    {
        var query = QueryWithIncludes().AsNoTracking();

        if (filters.Month is not null)
        {
            query = query.Where(c => c.Month == filters.Month.Value);
        }

        if (filters.Year is not null)
        {
            query = query.Where(c => c.Year == filters.Year.Value);
        }

        if (filters.Type is not null)
        {
            query = query.Where(c => c.Type == filters.Type.Value);
        }

        if (filters.Category is not null)
        {
            query = query.Where(c => c.Category == filters.Category.Value);
        }

        if (filters.ProjectId is not null)
        {
            query = query.Where(c => c.ProjectId == filters.ProjectId.Value);
        }

        if (filters.DepartmentId is not null)
        {
            query = query.Where(c => c.DepartmentId == filters.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(c =>
                (c.Notes != null && c.Notes.Contains(search)) ||
                (c.Requester != null && c.Requester.Contains(search)) ||
                (c.PurchaseLocation != null && c.PurchaseLocation.Contains(search)));
        }

        return query;
    }

    private IQueryable<ProjectCost> QueryWithIncludes() =>
        _dbContext.ProjectCosts
            .Include(c => c.Project)
            .Include(c => c.Department)
            .Include(c => c.PaymentMethod);

    private async Task<CashflowFilterOptionsResponse> BuildFilterOptionsAsync(CancellationToken cancellationToken)
    {
        var projects = (await _masterDataCache.GetActiveProjectsAsync(cancellationToken))
            .Select(project => new CashflowFilterOption(project.Id, project.Name))
            .ToList();

        var departments = (await _masterDataCache.GetActiveDepartmentsAsync(cancellationToken))
            .Select(department => new CashflowFilterOption(department.Id, department.Name))
            .ToList();

        var paymentMethods = (await _masterDataCache.GetActivePaymentMethodsAsync(cancellationToken))
            .Select(method => new CashflowFilterOption(method.Id, method.Name))
            .ToList();

        return new CashflowFilterOptionsResponse(projects, departments, paymentMethods);
    }

    private static Result ValidateReportFilters(CashflowReportFilters filters)
    {
        if (filters.Month is < 1 or > 12)
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_month", "Month must be between 1 and 12."));
        }

        if (filters.Year is < 2000 or > 2100)
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_year", "Year is out of allowed range."));
        }

        return Result.Success();
    }

    private static Result ValidateListFilters(CashflowListFilters filters)
    {
        if (filters.Month is < 1 or > 12)
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_month", "Month must be between 1 and 12."));
        }

        if (filters.Year is < 2000 or > 2100)
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_year", "Year is out of allowed range."));
        }

        if (filters.Type is not null && !Enum.IsDefined(filters.Type.Value))
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_type", "Cashflow type is invalid."));
        }

        if (filters.Category is not null && !Enum.IsDefined(filters.Category.Value))
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_category", "Cashflow category is invalid."));
        }

        return Result.Success();
    }

    private async Task<Result> ValidateMutationAsync(
        ProjectCostType type,
        ProjectCostCategory category,
        decimal amount,
        DateOnly transactionDate,
        int month,
        int year,
        Guid? projectId,
        Guid? paymentMethodId,
        Guid? departmentId,
        string? requester,
        string? purchaseLocation,
        string? attachmentUrl,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(type))
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_type", "Cashflow type is invalid."));
        }

        if (!Enum.IsDefined(category))
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_category", "Cashflow category is invalid."));
        }

        if (!ProjectCostCategoryRules.IsValidForType(type, category))
        {
            return Result.Failure(
                Error.Validation("cashflow.category_type_mismatch", "Category is not valid for the selected type."));
        }

        if (amount <= 0)
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_amount", "Amount must be greater than zero."));
        }

        if (month is < 1 or > 12)
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_month", "Month must be between 1 and 12."));
        }

        if (year is < 2000 or > 2100)
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_year", "Year is out of allowed range."));
        }

        if (transactionDate.Year < 2000 || transactionDate.Year > 2100)
        {
            return Result.Failure(
                Error.Validation("cashflow.invalid_transaction_date", "Transaction date is out of allowed range."));
        }

        if (notes is not null && notes.Length > NotesMaxLength)
        {
            return Result.Failure(
                Error.Validation("cashflow.notes_too_long", "Notes exceed maximum length."));
        }

        if (requester is not null && requester.Length > RequesterMaxLength)
        {
            return Result.Failure(
                Error.Validation("cashflow.requester_too_long", "Requester exceeds maximum length."));
        }

        if (purchaseLocation is not null && purchaseLocation.Length > PurchaseLocationMaxLength)
        {
            return Result.Failure(
                Error.Validation("cashflow.purchase_location_too_long", "Purchase location exceeds maximum length."));
        }

        var attachmentUrlValidation = SafeExternalUrlValidator.Validate(attachmentUrl);
        if (attachmentUrlValidation.IsFailure)
        {
            return Result.Failure(attachmentUrlValidation.Error!);
        }

        if (projectId is not null &&
            !await _dbContext.Projects.AnyAsync(p => p.Id == projectId.Value && p.IsActive, cancellationToken))
        {
            return Result.Failure(
                Error.Validation("cashflow.project_not_found", "Project not found."));
        }

        if (type == ProjectCostType.Saida)
        {
            if (paymentMethodId is null)
            {
                return Result.Failure(
                    Error.Validation("cashflow.payment_method_required", "Payment method is required for exit entries."));
            }

            if (!await _dbContext.PaymentMethods.AnyAsync(
                    p => p.Id == paymentMethodId.Value && p.IsActive,
                    cancellationToken))
            {
                return Result.Failure(
                    Error.Validation("cashflow.payment_method_not_found", "Payment method not found."));
            }

            if (departmentId is null)
            {
                return Result.Failure(
                    Error.Validation("cashflow.department_required", "Department is required for exit entries."));
            }

            if (!await _dbContext.Departments.AnyAsync(
                    d => d.Id == departmentId.Value && d.IsActive,
                    cancellationToken))
            {
                return Result.Failure(
                    Error.Validation("cashflow.department_not_found", "Department not found."));
            }
        }
        else if (paymentMethodId is not null ||
                 departmentId is not null ||
                 !string.IsNullOrWhiteSpace(requester) ||
                 !string.IsNullOrWhiteSpace(purchaseLocation))
        {
            return Result.Failure(
                Error.Validation("cashflow.entry_exit_fields_forbidden", "Exit-only fields are not allowed for entry records."));
        }

        return Result.Success();
    }

    private async Task LoadNavigationPropertiesAsync(
        IReadOnlyList<ProjectCost> entries,
        CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            if (entry.ProjectId is not null)
            {
                await _dbContext.Entry(entry).Reference(c => c.Project).LoadAsync(cancellationToken);
            }

            if (entry.DepartmentId is not null)
            {
                await _dbContext.Entry(entry).Reference(c => c.Department).LoadAsync(cancellationToken);
            }

            if (entry.PaymentMethodId is not null)
            {
                await _dbContext.Entry(entry).Reference(c => c.PaymentMethod).LoadAsync(cancellationToken);
            }
        }
    }

    private static string? NormalizeAttachmentUrl(string? value) =>
        SafeExternalUrlValidator.Validate(value).Value;

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CashflowEntryResponse MapResponse(ProjectCost entry) =>
        new(
            entry.Id,
            entry.Type,
            entry.Category,
            entry.Amount,
            entry.TransactionDate,
            entry.Month,
            entry.Year,
            entry.ProjectId,
            entry.Project?.Name,
            entry.DepartmentId,
            entry.Department?.Name,
            entry.PaymentMethodId,
            entry.PaymentMethod?.Name,
            entry.Requester,
            entry.PurchaseLocation,
            entry.InstallmentNumber,
            entry.InstallmentTotal,
            entry.CompraId,
            entry.AttachmentUrl,
            entry.Notes);
}

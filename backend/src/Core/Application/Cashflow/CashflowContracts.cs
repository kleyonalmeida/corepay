using Core.Domain;

namespace Core.Application.Cashflow;

public sealed record CashflowEntryResponse(
    Guid Id,
    ProjectCostType Type,
    ProjectCostCategory Category,
    decimal Amount,
    DateOnly TransactionDate,
    int Month,
    int Year,
    Guid? ProjectId,
    string? ProjectName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? PaymentMethodId,
    string? PaymentMethodName,
    string? Requester,
    string? PurchaseLocation,
    int? InstallmentNumber,
    int? InstallmentTotal,
    Guid? CompraId,
    string? AttachmentUrl,
    string? Notes);

public sealed record CashflowSummaryResponse(
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal Saldo,
    int Count);

public sealed record CashflowFilterOption(Guid Id, string Name);

public sealed record CashflowFilterOptionsResponse(
    IReadOnlyList<CashflowFilterOption> Projects,
    IReadOnlyList<CashflowFilterOption> Departments,
    IReadOnlyList<CashflowFilterOption> PaymentMethods);

public sealed record CashflowListResponse(
    CashflowSummaryResponse Summary,
    IReadOnlyList<CashflowEntryResponse> Items,
    CashflowFilterOptionsResponse FilterOptions,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CashflowListFilters(
    int? Month,
    int? Year,
    ProjectCostType? Type,
    ProjectCostCategory? Category,
    Guid? ProjectId,
    Guid? DepartmentId,
    string? Search,
    int? Page = null,
    int? PageSize = null,
    bool IncludeFilterOptions = true);

public sealed record CreateCashflowEntryRequest(
    ProjectCostType Type,
    ProjectCostCategory Category,
    decimal Amount,
    DateOnly TransactionDate,
    int Month,
    int Year,
    Guid? ProjectId,
    Guid? PaymentMethodId,
    Guid? DepartmentId,
    string? Requester,
    string? PurchaseLocation,
    int InstallmentTotal,
    string? AttachmentUrl,
    string? Notes);

public sealed record UpdateCashflowEntryRequest(
    ProjectCostType Type,
    ProjectCostCategory Category,
    decimal Amount,
    DateOnly TransactionDate,
    int Month,
    int Year,
    Guid? ProjectId,
    Guid? PaymentMethodId,
    Guid? DepartmentId,
    string? Requester,
    string? PurchaseLocation,
    string? AttachmentUrl,
    string? Notes);

public sealed record CashflowCreateResponse(
    CashflowEntryResponse PrimaryEntry,
    IReadOnlyList<CashflowEntryResponse> CreatedEntries);

public sealed record CashflowReportFilters(int Month, int Year);

public sealed record CashflowReportProjectRowResponse(
    Guid? ProjectId,
    string ProjectName,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal Saldo,
    int Count);

public sealed record CashflowReportPaymentMethodRowResponse(
    Guid PaymentMethodId,
    string PaymentMethodName,
    decimal TotalSaidas,
    int Count);

public sealed record CashflowReportResponse(
    CashflowSummaryResponse Summary,
    IReadOnlyList<CashflowReportProjectRowResponse> ByProject,
    IReadOnlyList<CashflowReportPaymentMethodRowResponse> ByPaymentMethod);

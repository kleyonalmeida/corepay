namespace WebApp.Blazor.Services;

public enum CashflowApiStatus
{
    Success,
    ValidationError,
    NotFound,
    Conflict,
    Forbidden,
    Error
}

public enum CashflowEntryType
{
    Entrada,
    Saida
}

public enum CashflowEntryCategory
{
    Plataforma,
    Igaming,
    DevolucoesReembolso,
    FolhaPagamento,
    Trafego,
    Acoes,
    RecargasBanca,
    Viagens,
    Imposto,
    Reembolso,
    DespesaAlimentar,
    MoveisEquipamentos,
    Experts,
    Reforma,
    Aeronave,
    Administrativa,
    PlataformasDigitais,
    FestasEventos
}

public sealed record CashflowEntryDto(
    Guid Id,
    CashflowEntryType Type,
    CashflowEntryCategory Category,
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

public sealed record CashflowSummaryDto(
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal Saldo,
    int Count);

public sealed record CashflowFilterOptionDto(Guid Id, string Name);

public sealed record CashflowFilterOptionsDto(
    IReadOnlyList<CashflowFilterOptionDto> Projects,
    IReadOnlyList<CashflowFilterOptionDto> Departments,
    IReadOnlyList<CashflowFilterOptionDto> PaymentMethods);

public sealed record CashflowListResponseDto(
    CashflowSummaryDto Summary,
    IReadOnlyList<CashflowEntryDto> Items,
    CashflowFilterOptionsDto FilterOptions,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CashflowListQuery(
    int? Month = null,
    int? Year = null,
    CashflowEntryType? Type = null,
    CashflowEntryCategory? Category = null,
    Guid? ProjectId = null,
    Guid? DepartmentId = null,
    string? Search = null,
    int? Page = null,
    int? PageSize = null,
    bool IncludeFilterOptions = true);

public sealed record CashflowCreateRequest(
    CashflowEntryType Type,
    CashflowEntryCategory Category,
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

public sealed record CashflowUpdateRequest(
    CashflowEntryType Type,
    CashflowEntryCategory Category,
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

public sealed record CashflowCreateResponseDto(
    CashflowEntryDto PrimaryEntry,
    IReadOnlyList<CashflowEntryDto> CreatedEntries);

public sealed record CashflowListResult(
    CashflowApiStatus Status,
    CashflowListResponseDto? Data = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record CashflowDetailResult(
    CashflowApiStatus Status,
    CashflowEntryDto? Entry = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record CashflowCreateResult(
    CashflowApiStatus Status,
    CashflowCreateResponseDto? Data = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record CashflowMutationResult(
    CashflowApiStatus Status,
    CashflowEntryDto? Entry = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record CashflowInstallmentGroupResult(
    CashflowApiStatus Status,
    IReadOnlyList<CashflowEntryDto>? Entries = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record CashflowDeleteResult(
    CashflowApiStatus Status,
    string? ErrorCode = null,
    string? Message = null);

public sealed record CashflowReportProjectRowDto(
    Guid? ProjectId,
    string ProjectName,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal Saldo,
    int Count);

public sealed record CashflowReportPaymentMethodRowDto(
    Guid PaymentMethodId,
    string PaymentMethodName,
    decimal TotalSaidas,
    int Count);

public sealed record CashflowReportResponseDto(
    CashflowSummaryDto Summary,
    IReadOnlyList<CashflowReportProjectRowDto> ByProject,
    IReadOnlyList<CashflowReportPaymentMethodRowDto> ByPaymentMethod);

public sealed record CashflowReportQuery(int Month, int Year);

public sealed record CashflowReportResult(
    CashflowApiStatus Status,
    CashflowReportResponseDto? Data = null,
    string? ErrorCode = null,
    string? Message = null);

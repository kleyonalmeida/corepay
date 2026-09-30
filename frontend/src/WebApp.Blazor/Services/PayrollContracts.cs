namespace WebApp.Blazor.Services;

public enum PayrollAccessStatus
{
    Found,
    NotFound,
    Forbidden,
    Error
}

public enum PayrollApiStatus
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError,
    Error
}

public enum PayrollFormMode
{
    Create,
    Edit
}

public sealed record PayrollFormDepartmentOptionDto(Guid Id, string Name);

public sealed record PayrollFormOptionsDto(
    IReadOnlyList<PayrollFormDepartmentOptionDto> Departments);

public sealed record PayrollEntryShellDto(
    Guid Id,
    Guid CollaboratorId,
    string CollaboratorName,
    string? CareerLevelName,
    string? PixKey,
    DateOnly? AdmissionDate,
    decimal? FullBaseSalary,
    CalculationProfile CalculationProfile,
    bool IsApproved);

public sealed record PayrollAllowedActionsDto(
    bool ApprovePayroll,
    bool RejectPayroll,
    bool ApproveEntry,
    bool Edit,
    bool PayPayroll,
    bool PayEntry,
    bool ToggleNf,
    bool PostApprovalAdjustments,
    bool Delete,
    bool Recalculate,
    bool AddCollaborator);

public sealed record AddCollaboratorEntryRequestDto(Guid CollaboratorId);

public sealed record PayrollDetailDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    int Month,
    int Year,
    string Status,
    decimal TotalAmount,
    int EntryCount,
    string? SubmittedBy,
    string? RejectionComment,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    IReadOnlyList<PayrollEntryEditorDto> Entries,
    PayrollEditorOptionsDto EditorOptions,
    PayrollAllowedActionsDto AllowedActions);

public sealed record RejectPayrollRequestDto(string RejectionComment);

public sealed record SetEntryPaidRequestDto(bool IsPaid);

public sealed record SetEntryNfRequestDto(bool NfSent);

public sealed record PayrollSummaryDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    int Month,
    int Year,
    string Status,
    decimal TotalAmount,
    int EntryCount,
    string? SubmittedBy,
    string? RejectionComment);

public sealed record PayrollListItemDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    int Month,
    int Year,
    string Status,
    decimal TotalAmount,
    int EntryCount,
    string? SubmittedBy);

public sealed record PayrollsListResponseDto(
    IReadOnlyList<PayrollListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PayrollListQuery(
    string? Search = null,
    int? Month = null,
    int? Year = null,
    string? Status = null,
    Guid? DepartmentId = null,
    int? Page = null,
    int? PageSize = null);

public sealed record CreatePayrollRequestDto(
    Guid DepartmentId,
    int Month,
    int Year,
    IReadOnlyList<Guid> CollaboratorIds);

public sealed record UpdatePayrollEntryRequestDto(
    Guid EntryId,
    GoalTier GoalTier = GoalTier.None,
    decimal? FinalSalary = null,
    int BetanoInternaCount = 0,
    int BetanoMundoBetCount = 0,
    decimal SupervisorAnalystRevenue = 0,
    Guid? CommissionPayingProjectId = null,
    IReadOnlyList<ManagementRevenueEntryDto>? ManagementRevenueEntries = null,
    IReadOnlyList<TrafficProjectEntryDto>? TrafficProjectEntries = null,
    IReadOnlyList<SupervisorProjectEntryDto>? SupervisorProjectEntries = null,
    IReadOnlyList<CommercialAnalystProjectEntryDto>? CommercialProjectEntries = null,
    IReadOnlyList<ProjectEntryDto>? ProjectEntries = null,
    IReadOnlyList<RateioProjectEntryDto>? RateioProjectEntries = null,
    IReadOnlyList<BonusEntryDto>? BonusEntries = null,
    IReadOnlyList<DeductionEntryDto>? DeductionEntries = null,
    IReadOnlyList<ComplementPayingProjectDto>? ComplementPayingProjects = null,
    IReadOnlyList<PayrollRoleChangeEntryDto>? RoleChanges = null);

public sealed record UpdatePayrollRequestDto(
    IReadOnlyList<Guid> CollaboratorIds,
    IReadOnlyList<UpdatePayrollEntryRequestDto>? Entries = null);

public sealed record PayrollListResult(
    PayrollApiStatus Status,
    IReadOnlyList<PayrollListItemDto>? Payrolls = null,
    int TotalCount = 0,
    int Page = 1,
    int PageSize = ListPagination.PageSize,
    string? ErrorCode = null,
    string? Message = null);

public sealed record PayrollDuplicateResult(
    PayrollApiStatus Status,
    PayrollSummaryDto? Payroll = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record PayrollAccessResult(
    PayrollAccessStatus Status,
    PayrollDetailDto? Payroll = null);

public sealed record PayrollFormOptionsResult(
    PayrollApiStatus Status,
    PayrollFormOptionsDto? Options = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record PayrollMutationResult(
    PayrollApiStatus Status,
    PayrollDetailDto? Payroll = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record PayrollDeleteResult(
    PayrollApiStatus Status,
    string? ErrorCode = null,
    string? Message = null);

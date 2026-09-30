using Core.Domain;
using Core.Domain.PayrollCalculation;
using System.Text.Json.Serialization;

namespace Core.Application.Payrolls;

public sealed record PayrollFormDepartmentOption(Guid Id, string Name);

public sealed record PayrollFormOptionsResponse(
    IReadOnlyList<PayrollFormDepartmentOption> Departments);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreatePayrollRequest(
    Guid DepartmentId,
    int Month,
    int Year,
    IReadOnlyList<Guid> CollaboratorIds);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdatePayrollRequest(
    IReadOnlyList<Guid> CollaboratorIds,
    IReadOnlyList<UpdatePayrollEntryRequest>? Entries = null);

public sealed record PayrollEntryShellResponse(
    Guid Id,
    Guid CollaboratorId,
    string CollaboratorName,
    string? CareerLevelName,
    string? PixKey,
    DateOnly? AdmissionDate,
    decimal? FullBaseSalary,
    CalculationProfile CalculationProfile,
    bool IsApproved);

public sealed record PayrollDetailResponse(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    int Month,
    int Year,
    PayrollStatus Status,
    decimal TotalAmount,
    int EntryCount,
    string? SubmittedBy,
    string? RejectionComment,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    IReadOnlyList<PayrollEntryEditorResponse> Entries,
    PayrollEditorOptionsResponse EditorOptions,
    PayrollAllowedActionsResponse AllowedActions);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RejectPayrollRequest(string RejectionComment);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SetEntryPaidRequest(bool IsPaid);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SetEntryNfRequest(bool NfSent);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AddCollaboratorEntryRequest(Guid CollaboratorId);

public sealed record PayrollListItemResponse(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    int Month,
    int Year,
    PayrollStatus Status,
    decimal TotalAmount,
    int EntryCount,
    string? SubmittedBy);

public sealed record PayrollsListResponse(
    IReadOnlyList<PayrollListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PayrollSummaryResponse(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    int Month,
    int Year,
    PayrollStatus Status,
    decimal TotalAmount,
    int EntryCount,
    string? SubmittedBy);

public sealed record PayrollAccessContext(
    string UserId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid>? AllowedDepartmentIds,
    string DisplayName = "",
    IReadOnlyList<string>? Permissions = null);

public sealed record PayrollListFilters(
    string? Search,
    int? Month,
    int? Year,
    PayrollStatus? Status,
    Guid? DepartmentId,
    int? Page = null,
    int? PageSize = null);

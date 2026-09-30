namespace WebApp.Blazor.Services;

public enum CollaboratorApiStatus
{
    Success,
    ValidationError,
    NotFound,
    Forbidden,
    Error
}

public sealed record CollaboratorDto(
    Guid Id,
    string Name,
    Guid DepartmentId,
    string DepartmentName,
    Guid? CareerLevelId,
    string? CareerLevelName,
    string? JobTitle,
    DateOnly? AdmissionDate,
    DateOnly? DismissalDate,
    string? PixKey,
    decimal? BaseSalary,
    string? Email,
    string? PhotoUrl,
    bool IsActive,
    CalculationProfile? CalculationProfileOverride);

public sealed record CollaboratorsListResponseDto(
    IReadOnlyList<CollaboratorDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CollaboratorListQuery(
    Guid? DepartmentId = null,
    string? Search = null,
    bool? IsActive = null,
    int? Page = null,
    int? PageSize = null);

public sealed record CollaboratorListResult(
    CollaboratorApiStatus Status,
    IReadOnlyList<CollaboratorDto>? Collaborators = null,
    int TotalCount = 0,
    int Page = 1,
    int PageSize = ListPagination.PageSize,
    string? ErrorCode = null,
    string? Message = null);

public sealed record CollaboratorGetResult(
    CollaboratorApiStatus Status,
    CollaboratorDto? Collaborator = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record CollaboratorRequest(
    string Name,
    Guid DepartmentId,
    Guid? CareerLevelId,
    string? JobTitle,
    DateOnly? AdmissionDate,
    DateOnly? DismissalDate,
    string? PixKey,
    decimal? BaseSalary,
    string? Email,
    string? PhotoUrl,
    bool IsActive,
    CalculationProfile? CalculationProfileOverride);

public sealed record CollaboratorMutationResult(
    CollaboratorApiStatus Status,
    CollaboratorDto? Collaborator = null,
    string? ErrorCode = null,
    string? Message = null);

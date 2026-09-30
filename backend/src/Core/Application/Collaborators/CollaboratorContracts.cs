using Core.Domain;

namespace Core.Application.Collaborators;

public sealed record CollaboratorsListResponse(
    IReadOnlyList<CollaboratorResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CollaboratorResponse(
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

public sealed record CreateCollaboratorRequest(
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

public sealed record UpdateCollaboratorRequest(
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

public sealed record CollaboratorAccessContext(
    string UserId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid>? AllowedDepartmentIds);

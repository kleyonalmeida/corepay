namespace WebApp.Blazor.Services;

public sealed record UserFormSubmit(
    string Email,
    string? Password,
    string DisplayName,
    IReadOnlyList<string> RoleNames,
    IReadOnlyList<Guid> DepartmentIds);

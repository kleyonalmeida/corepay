using Microsoft.AspNetCore.Authorization;

namespace WebApp.Blazor.Auth;

public sealed class AnyPermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}

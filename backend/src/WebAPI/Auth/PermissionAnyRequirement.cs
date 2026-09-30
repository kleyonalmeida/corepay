using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Auth;

public sealed class PermissionAnyRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}

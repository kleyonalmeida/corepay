using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Auth;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

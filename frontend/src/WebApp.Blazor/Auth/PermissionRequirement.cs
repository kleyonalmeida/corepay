using Microsoft.AspNetCore.Authorization;

namespace WebApp.Blazor.Auth;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

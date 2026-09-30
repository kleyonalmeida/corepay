using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace WebApp.Blazor.Auth;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "permission:";
    public const string AnyPolicyPrefix = "permission-any:";

    private readonly DefaultAuthorizationPolicyProvider _fallback =
        new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(AnyPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissions = policyName[AnyPolicyPrefix.Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new AnyPermissionRequirement(permissions))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();
}

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;

namespace WebApp.Blazor.Tests.Auth;

public class AuthorizationPolicyTests
{
    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, PermissionRequirement requirement) =>
        new([requirement], user, resource: null);

    [Fact]
    public async Task SuperAdmin_BypassesPermissionPolicy()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(AppPermissions.FinanceRead);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, AppRoles.SuperAdmin)
        ],
        authenticationType: "jwt"));

        var context = CreateContext(user, requirement);
        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task User_WithPermissionClaim_IsAuthorized()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(AppPermissions.PayrollsRead);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("permission", AppPermissions.PayrollsRead)
        ],
        authenticationType: "jwt"));

        var context = CreateContext(user, requirement);
        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task User_WithoutPermissionClaim_IsDenied()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(AppPermissions.FinanceRead);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("permission", AppPermissions.PayrollsRead)
        ],
        authenticationType: "jwt"));

        var context = CreateContext(user, requirement);
        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task PolicyProvider_CreatesDynamicPermissionPolicy()
    {
        var services = new ServiceCollection();
        services.AddAuthorizationCore();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        await using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync("permission:payrolls.read");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Single().Permission
            .Should().Be(AppPermissions.PayrollsRead);
    }
}

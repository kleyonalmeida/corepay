using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Auth;

namespace WebApp.Blazor.Tests.Auth;

public class AnyPermissionPolicyTests
{
    private static AuthorizationHandlerContext CreateContext(
        ClaimsPrincipal user,
        AnyPermissionRequirement requirement) =>
        new([requirement], user, resource: null);

    [Fact]
    public async Task SuperAdmin_BypassesAnyPermissionPolicy()
    {
        var handler = new AnyPermissionAuthorizationHandler();
        var requirement = new AnyPermissionRequirement(
        [
            AppPermissions.DepartmentsWrite,
            AppPermissions.CareerLevelsWrite
        ]);
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
    public async Task User_WithOneMatchingPermission_IsAuthorized()
    {
        var handler = new AnyPermissionAuthorizationHandler();
        var requirement = new AnyPermissionRequirement(
        [
            AppPermissions.DepartmentsWrite,
            AppPermissions.PaymentMethodsWrite
        ]);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("permission", AppPermissions.PaymentMethodsWrite)
        ],
        authenticationType: "jwt"));

        var context = CreateContext(user, requirement);
        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task User_WithoutAnyMatchingPermission_IsDenied()
    {
        var handler = new AnyPermissionAuthorizationHandler();
        var requirement = new AnyPermissionRequirement(
        [
            AppPermissions.DepartmentsWrite,
            AppPermissions.ProjectsWrite
        ]);
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
    public async Task PolicyProvider_CreatesDynamicAnyPermissionPolicy()
    {
        var services = new ServiceCollection();
        services.AddAuthorizationCore();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, AnyPermissionAuthorizationHandler>();

        await using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync(AppPolicies.MasterData);

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<AnyPermissionRequirement>().Single().Permissions
            .Should().BeEquivalentTo(
            [
                AppPermissions.DepartmentsWrite,
                AppPermissions.CareerLevelsWrite,
                AppPermissions.ProjectsWrite,
                AppPermissions.PaymentMethodsWrite
            ]);
    }
}

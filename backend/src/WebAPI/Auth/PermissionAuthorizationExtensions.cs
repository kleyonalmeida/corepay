using Core.Auth;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Auth;

public static class PermissionAuthorizationExtensions
{
    public const string PolicyPrefix = "permission:";
    public const string AnyPolicyPrefix = "permission-any:";

    public static IServiceCollection AddCorePayPermissionPolicies(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAnyAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, AdminRoleAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, SuperAdminRoleAuthorizationHandler>();

        var builder = services.AddAuthorizationBuilder();

        foreach (var permissionKey in AppPermissions.All)
        {
            builder.AddPolicy(
                Policy(permissionKey),
                policy => policy.AddRequirements(new PermissionRequirement(permissionKey)));
        }

        builder.AddPolicy(
            AnyPolicy(AppPermissions.PayrollsWrite, AppPermissions.PayrollsPay),
            policy => policy.AddRequirements(
                new PermissionAnyRequirement([AppPermissions.PayrollsWrite, AppPermissions.PayrollsPay])));

        builder.AddPolicy(
            AuthServiceCollectionExtensions.AdminRolePolicy,
            policy => policy.AddRequirements(new AdminRoleRequirement()));

        builder.AddPolicy(
            AuthServiceCollectionExtensions.SuperAdminRolePolicy,
            policy => policy.AddRequirements(new SuperAdminRoleRequirement()));

        return services;
    }

    public static string Policy(string permissionKey) => $"{PolicyPrefix}{permissionKey}";

    public static string AnyPolicy(params string[] permissionKeys) =>
        $"{AnyPolicyPrefix}{string.Join(',', permissionKeys)}";
}

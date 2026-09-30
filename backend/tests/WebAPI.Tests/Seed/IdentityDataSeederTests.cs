using Core.Auth;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Seed;

[Collection("WebApiIntegration")]
public class IdentityDataSeederTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public IdentityDataSeederTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await seeder.SeedAsync();
        var permissionCount = await dbContext.Permissions.CountAsync();
        var rolePermissionCount = await dbContext.RolePermissions.CountAsync();

        await seeder.SeedAsync();

        (await dbContext.Permissions.CountAsync()).Should().Be(permissionCount);
        (await dbContext.RolePermissions.CountAsync()).Should().Be(rolePermissionCount);
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateAllPermissionsAndReferenceRoles()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var permissionKeys = await dbContext.Permissions
            .Select(p => p.Key)
            .ToListAsync();

        permissionKeys.Should().BeEquivalentTo(AppPermissions.All);

        foreach (var roleName in AppRoles.ReferenceRoles)
        {
            (await roleManager.RoleExistsAsync(roleName)).Should().BeTrue(
                because: $"reference role '{roleName}' must exist");
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldMapRolePermissionsAccordingToReferenceMap()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var (roleName, expectedPermissions) in RolePermissionMap.ReferenceRolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            role.Should().NotBeNull();

            var actualPermissions = await dbContext.RolePermissions
                .Where(rp => rp.RoleId == role!.Id)
                .Select(rp => rp.Permission.Key)
                .ToListAsync();

            actualPermissions.Should().BeEquivalentTo(expectedPermissions);
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateSuperAdminWithUserNameEqualToEmail()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = await userManager.FindByEmailAsync(CorePayWebApplicationFactory.SuperAdminEmail);
        user.Should().NotBeNull();
        user!.UserName.Should().Be(CorePayWebApplicationFactory.SuperAdminEmail);
        user.Email.Should().Be(CorePayWebApplicationFactory.SuperAdminEmail);

        var roles = await userManager.GetRolesAsync(user);
        roles.Should().Contain(AppRoles.SuperAdmin);
    }
}

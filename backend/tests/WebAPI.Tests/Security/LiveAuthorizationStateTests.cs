using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Auth;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Security;

[Collection("WebApiIntegration")]
public sealed class LiveAuthorizationStateTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public LiveAuthorizationStateTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Token_AfterAdminRoleRemoved_ShouldReturnForbiddenOnProtectedRoute()
    {
        var client = _factory.CreateClient();
        var (email, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await userManager.FindByEmailAsync(email);
            await userManager.RemoveFromRoleAsync(user!, AppRoles.Admin);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Token_AfterUserDeleted_ShouldReturnUnauthorized()
    {
        var email = $"deleted-{Guid.NewGuid():N}@corepay.test";
        const string password = "TestPassword123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                UserName = email,
                DisplayName = "Deleted User",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, AppRoles.Admin);
        }

        var client = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAsync(client, email, password);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await userManager.FindByEmailAsync(email);
            await userManager.DeleteAsync(user!);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_AfterPermissionRemovedFromRole_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
            var role = dbContext.Roles.First(r => r.Name == AppRoles.Admin);
            var permission = dbContext.Permissions.First(p => p.Key == AppPermissions.RolesRead);
            var mapping = dbContext.RolePermissions.First(rp =>
                rp.RoleId == role.Id && rp.PermissionId == permission.Id);
            dbContext.RolePermissions.Remove(mapping);
            await dbContext.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Token_AfterUserLockedOut_ShouldReturnUnauthorizedImmediately()
    {
        var client = _factory.CreateClient();
        var (email, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await userManager.FindByEmailAsync(email);
            await userManager.SetLockoutEnabledAsync(user!, true);
            await userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddHours(1));
        }

        AuthTestHelper.SetBearerToken(client, token);
        var response = await client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_AfterDepartmentsChanged_ShouldExposeCurrentDepartmentsImmediately()
    {
        var firstDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            Infrastructure.Seed.SeedKeys.Departments.CommercialAnalysts);
        var secondDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            Infrastructure.Seed.SeedKeys.Departments.PaidTraffic);
        var client = _factory.CreateClient();
        var (email, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(
            _factory,
            firstDepartmentId);
        AuthTestHelper.SetBearerToken(client, token);

        var initial = await client.GetFromJsonAsync<CurrentUserApiResponse>("/api/v1/auth/me");
        initial!.DepartmentIds.Should().BeEquivalentTo([firstDepartmentId]);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await userManager.FindByEmailAsync(email);
            dbContext.UserDepartments.RemoveRange(
                dbContext.UserDepartments.Where(item => item.UserId == user!.Id));
            await dbContext.SaveChangesAsync();
        }

        var removed = await client.GetFromJsonAsync<CurrentUserApiResponse>("/api/v1/auth/me");
        removed!.DepartmentIds.Should().BeEmpty();

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await userManager.FindByEmailAsync(email);
            dbContext.UserDepartments.Add(new UserDepartment
            {
                UserId = user!.Id,
                DepartmentId = secondDepartmentId
            });
            await dbContext.SaveChangesAsync();
        }

        var added = await client.GetFromJsonAsync<CurrentUserApiResponse>("/api/v1/auth/me");
        added!.DepartmentIds.Should().BeEquivalentTo([secondDepartmentId]);
    }

    [Fact]
    public async Task Token_WithForgedRoleAndPermissionClaims_ShouldUseDatabaseAuthorization()
    {
        var (email, _, _) = await AuthTestHelper.CreateUserWithoutPermissionsAsync(_factory);
        string userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            userId = (await userManager.FindByEmailAsync(email))!.Id;
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, AppRoles.Admin),
            new Claim("permission", AppPermissions.RolesRead)
        };
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "CorePay",
            audience: "CorePay",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    CorePayWebApplicationFactory.TestSigningKey)),
                SecurityAlgorithms.HmacSha256)));

        var client = _factory.CreateClient();
        AuthTestHelper.SetBearerToken(client, token);
        var response = await client.GetAsync("/api/v1/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record CurrentUserApiResponse(IReadOnlyList<Guid> DepartmentIds);
}

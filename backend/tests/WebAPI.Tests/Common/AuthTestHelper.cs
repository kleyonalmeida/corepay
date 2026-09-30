using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI.Tests.Common;

public static class AuthTestHelper
{
    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginApiResponse>();
        return body!.AccessToken;
    }

    public static void SetBearerToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static async Task<Guid> GetDepartmentIdBySeedKeyAsync(IServiceProvider services, string seedKey)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var departmentId = await dbContext.SeedEntities
            .Where(s => s.Key == seedKey)
            .Select(s => s.EntityId)
            .FirstAsync();

        return departmentId;
    }

    public static async Task<(string Email, string Password, string AccessToken)> CreateAdminUserAsync(
        WebApplicationFactory<Program> factory)
    {
        var email = $"admin-{Guid.NewGuid():N}@corepay.test";
        const string password = "TestPassword123!";

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            DisplayName = "Test Admin",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, Core.Auth.AppRoles.Admin);

        var client = factory.CreateClient();
        var token = await LoginAsync(client, email, password);
        return (email, password, token);
    }

    public static async Task<(string Email, string Password, string AccessToken)> CreateUserWithoutPermissionsAsync(
        WebApplicationFactory<Program> factory)
    {
        var email = $"user-{Guid.NewGuid():N}@corepay.test";
        const string password = "TestPassword123!";

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            DisplayName = "Test User",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, Core.Auth.AppRoles.User);

        var client = factory.CreateClient();
        var token = await LoginAsync(client, email, password);
        return (email, password, token);
    }

    public static async Task<(string Email, string Password, string AccessToken)> CreateDirectorUserAsync(
        WebApplicationFactory<Program> factory)
    {
        var email = $"director-{Guid.NewGuid():N}@corepay.test";
        const string password = "TestPassword123!";

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            DisplayName = "Test Director",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, Core.Auth.AppRoles.Director);

        var client = factory.CreateClient();
        var token = await LoginAsync(client, email, password);
        return (email, password, token);
    }

    public static async Task<(string Email, string Password, string AccessToken)> CreateFinancialUserAsync(
        WebApplicationFactory<Program> factory)
    {
        var email = $"financial-{Guid.NewGuid():N}@corepay.test";
        const string password = "TestPassword123!";

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            DisplayName = "Test Financial",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, Core.Auth.AppRoles.Financial);

        var client = factory.CreateClient();
        var token = await LoginAsync(client, email, password);
        return (email, password, token);
    }

    public static async Task<(string Email, string Password, string AccessToken)> CreateManagerUserAsync(
        WebApplicationFactory<Program> factory) =>
        await CreateManagerWithDepartmentsAsync(factory);

    public static async Task<(string Email, string Password, string AccessToken)> CreateManagerWithDepartmentsAsync(
        WebApplicationFactory<Program> factory,
        params Guid[] departmentIds)
    {
        var email = $"manager-{Guid.NewGuid():N}@corepay.test";
        const string password = "TestPassword123!";

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            DisplayName = "Test Manager",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, Core.Auth.AppRoles.Manager);

        foreach (var departmentId in departmentIds.Distinct())
        {
            dbContext.UserDepartments.Add(new UserDepartment
            {
                UserId = user.Id,
                DepartmentId = departmentId
            });
        }

        await dbContext.SaveChangesAsync();

        var client = factory.CreateClient();
        var token = await LoginAsync(client, email, password);
        return (email, password, token);
    }

    public static async Task<Guid> GetCollaboratorIdBySeedKeyAsync(IServiceProvider services, string seedKey)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var collaboratorId = await dbContext.SeedEntities
            .Where(s => s.Key == seedKey)
            .Select(s => s.EntityId)
            .FirstAsync();

        return collaboratorId;
    }

    private sealed record LoginApiResponse(string AccessToken, DateTime ExpiresAtUtc, object User);
}

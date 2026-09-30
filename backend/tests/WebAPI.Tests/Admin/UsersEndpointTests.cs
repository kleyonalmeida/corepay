using System.Net;
using System.Net.Http.Json;
using Core.Auth;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Admin;

[Collection("WebApiIntegration")]
public class UsersEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public UsersEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateSuperAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAsync(
            client,
            CorePayWebApplicationFactory.SuperAdminEmail,
            CorePayWebApplicationFactory.SuperAdminPassword);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    [Fact]
    public async Task CreateManagerWithTwoDepartments_ShouldPersistAndRoundTrip()
    {
        var client = await CreateSuperAdminClientAsync();
        var departmentId1 = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.Tipster);
        var departmentId2 = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);

        var email = $"manager-{Guid.NewGuid():N}@corepay.test";
        const string password = "TestPassword123!";

        var createResponse = await client.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            password,
            displayName = "Manager Dois Setores",
            roleNames = new[] { AppRoles.Manager },
            departmentIds = new[] { departmentId1, departmentId2 }
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<UserApiResponse>();
        created.Should().NotBeNull();
        Guid.TryParse(created!.Id, out _).Should().BeTrue();
        created.Email.Should().Be(email);
        created.DisplayName.Should().Be("Manager Dois Setores");
        created.RoleNames.Should().Contain(AppRoles.Manager);
        created.DepartmentIds.Should().BeEquivalentTo([departmentId1, departmentId2]);

        var getResponse = await client.GetAsync($"/api/v1/users/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<UserApiResponse>();
        fetched!.DepartmentIds.Should().BeEquivalentTo([departmentId1, departmentId2]);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(created.Id);
        user!.UserName.Should().Be(email);
    }

    [Fact]
    public async Task CreateManager_WithoutDepartments_ShouldReturnBadRequest()
    {
        var client = await CreateSuperAdminClientAsync();
        var email = $"manager-no-dept-{Guid.NewGuid():N}@corepay.test";

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            password = "TestPassword123!",
            displayName = "Manager Sem Setor",
            roleNames = new[] { AppRoles.Manager },
            departmentIds = Array.Empty<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("users.manager_departments_required");
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ShouldReturnConflict()
    {
        var client = await CreateSuperAdminClientAsync();
        var email = $"dup-{Guid.NewGuid():N}@corepay.test";
        var payload = new
        {
            email,
            password = "TestPassword123!",
            displayName = "Dup User",
            roleNames = new[] { AppRoles.User },
            departmentIds = Array.Empty<Guid>()
        };

        (await client.PostAsJsonAsync("/api/v1/users", payload)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/v1/users", payload)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteSuperAdmin_ShouldReturnForbidden()
    {
        var client = await CreateSuperAdminClientAsync();
        var users = await client.GetFromJsonAsync<List<UserApiResponse>>("/api/v1/users");
        var superAdmin = users!.Single(u => u.RoleNames.Contains(AppRoles.SuperAdmin));

        var response = await client.DeleteAsync($"/api/v1/users/{superAdmin.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteSelf_ShouldReturnForbidden()
    {
        var client = await CreateSuperAdminClientAsync();
        var users = await client.GetFromJsonAsync<List<UserApiResponse>>("/api/v1/users");
        var superAdmin = users!.Single(u => u.RoleNames.Contains(AppRoles.SuperAdmin));

        var response = await client.DeleteAsync($"/api/v1/users/{superAdmin.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteCustomUser_ShouldReturnNoContent()
    {
        var client = await CreateSuperAdminClientAsync();
        var email = $"delete-{Guid.NewGuid():N}@corepay.test";

        var createResponse = await client.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            password = "TestPassword123!",
            displayName = "To Delete",
            roleNames = new[] { AppRoles.User },
            departmentIds = Array.Empty<Guid>()
        });

        var created = await createResponse.Content.ReadFromJsonAsync<UserApiResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/users/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/v1/users/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUser_RemovingSuperAdminRole_ShouldReturnForbidden()
    {
        var client = await CreateSuperAdminClientAsync();
        var users = await client.GetFromJsonAsync<List<UserApiResponse>>("/api/v1/users");
        var superAdmin = users!.Single(u => u.RoleNames.Contains(AppRoles.SuperAdmin));

        var response = await client.PutAsJsonAsync($"/api/v1/users/{superAdmin.Id}", new
        {
            email = superAdmin.Email,
            displayName = superAdmin.DisplayName,
            roleNames = new[] { AppRoles.Admin },
            departmentIds = Array.Empty<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_AsAdmin_WithSuperAdminRole_ShouldReturnForbidden()
    {
        var client = await CreateAdminClientAsync();
        var email = $"superadmin-attempt-{Guid.NewGuid():N}@corepay.test";

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            password = "TestPassword123!",
            displayName = "Escalation Attempt",
            roleNames = new[] { AppRoles.SuperAdmin },
            departmentIds = Array.Empty<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateUser_AsAdmin_PromotingToSuperAdmin_ShouldReturnForbidden()
    {
        var superAdminClient = await CreateSuperAdminClientAsync();
        var email = $"promote-{Guid.NewGuid():N}@corepay.test";

        var createResponse = await superAdminClient.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            password = "TestPassword123!",
            displayName = "Regular User",
            roleNames = new[] { AppRoles.User },
            departmentIds = Array.Empty<Guid>()
        });

        var created = await createResponse.Content.ReadFromJsonAsync<UserApiResponse>();

        var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PutAsJsonAsync($"/api/v1/users/{created!.Id}", new
        {
            email,
            displayName = "Regular User",
            roleNames = new[] { AppRoles.SuperAdmin },
            departmentIds = Array.Empty<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_AsSuperAdmin_WithSuperAdminRole_ShouldSucceed()
    {
        var client = await CreateSuperAdminClientAsync();
        var email = $"new-superadmin-{Guid.NewGuid():N}@corepay.test";

        var response = await client.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            password = "TestPassword123!",
            displayName = "Secondary SuperAdmin",
            roleNames = new[] { AppRoles.SuperAdmin },
            departmentIds = Array.Empty<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<UserApiResponse>();
        created!.RoleNames.Should().Contain(AppRoles.SuperAdmin);
    }

    private sealed record UserApiResponse(
        string Id,
        string Email,
        string DisplayName,
        IReadOnlyList<string> RoleNames,
        IReadOnlyList<Guid> DepartmentIds);

    private sealed record ApiErrorResponse(string? Error, string? Message);
}

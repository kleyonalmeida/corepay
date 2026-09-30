using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.AdminUi;

public class UserApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid DepartmentId1 = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid DepartmentId2 = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7");

    public UserApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IUserApiService, UserApiService>();
    }

    [Fact]
    public async Task GetUsersAsync_Success_ReturnsUsers()
    {
        ConfigureResponse(HttpMethod.Get, "/api/v1/users", HttpStatusCode.OK, CreateUserListJson());

        var service = Services.GetRequiredService<IUserApiService>();
        var result = await service.GetUsersAsync();

        result.Status.Should().Be(UserApiStatus.Success);
        result.Users.Should().HaveCount(1);
        result.Users![0].DisplayName.Should().Be("Gerente Regional");
        result.Users[0].RoleNames.Should().Contain("Manager");
    }

    [Fact]
    public async Task GetUsersAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Get, "/api/v1/users", HttpStatusCode.Forbidden);

        var service = Services.GetRequiredService<IUserApiService>();
        var result = await service.GetUsersAsync();

        result.Status.Should().Be(UserApiStatus.Forbidden);
    }

    [Fact]
    public async Task CreateUserAsync_Success_SerializesManagerWithTwoDepartments()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/users");
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateUserJson("Gerente Regional", "Manager", DepartmentId1, DepartmentId2))
            };
        });

        var service = Services.GetRequiredService<IUserApiService>();
        var result = await service.CreateUserAsync(new CreateUserRequest(
            "manager@corepay.test",
            "TestPassword123!",
            "Gerente Regional",
            ["Manager"],
            [DepartmentId1, DepartmentId2]));

        result.Status.Should().Be(UserApiStatus.Success);
        capturedBody.Should().Contain("\"roleNames\":[\"Manager\"]");
        capturedBody.Should().Contain(DepartmentId1.ToString());
        capturedBody.Should().Contain(DepartmentId2.ToString());
    }

    [Fact]
    public async Task UpdateUserAsync_ValidationError_ReturnsValidationStatus()
    {
        ConfigureResponse(
            HttpMethod.Put,
            "/api/v1/users/user-id",
            HttpStatusCode.BadRequest,
            """{"error":"users.email_invalid","message":"Email is invalid."}""");

        var service = Services.GetRequiredService<IUserApiService>();
        var result = await service.UpdateUserAsync(
            "user-id",
            new UpdateUserRequest("invalid", "Nome", ["User"], []));

        result.Status.Should().Be(UserApiStatus.ValidationError);
        result.ErrorCode.Should().Be("users.email_invalid");
    }

    [Fact]
    public async Task DeleteUserAsync_Success_ReturnsSuccess()
    {
        ConfigureResponse(HttpMethod.Delete, "/api/v1/users/user-id", HttpStatusCode.NoContent);

        var service = Services.GetRequiredService<IUserApiService>();
        var result = await service.DeleteUserAsync("user-id");

        result.Status.Should().Be(UserApiStatus.Success);
    }

    [Fact]
    public async Task DeleteUserAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(
            HttpMethod.Delete,
            "/api/v1/users/user-id",
            HttpStatusCode.Forbidden,
            """{"error":"users.cannot_delete_self","message":"You cannot delete your own user account."}""");

        var service = Services.GetRequiredService<IUserApiService>();
        var result = await service.DeleteUserAsync("user-id");

        result.Status.Should().Be(UserApiStatus.Forbidden);
        result.ErrorCode.Should().Be("users.cannot_delete_self");
    }

    [Fact]
    public async Task CreateUserAsync_Conflict_ReturnsConflict()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/users",
            HttpStatusCode.Conflict,
            """{"error":"users.duplicate_email","message":"Duplicate email."}""");

        var service = Services.GetRequiredService<IUserApiService>();
        var result = await service.CreateUserAsync(new CreateUserRequest(
            "dup@corepay.test",
            "TestPassword123!",
            "Dup",
            ["User"],
            []));

        result.Status.Should().Be(UserApiStatus.Conflict);
    }

    private void ConfigureResponse(HttpMethod method, string path, HttpStatusCode status, string? body = null)
    {
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(method);
            request.RequestUri!.AbsolutePath.Should().Be(path);
            return body is null
                ? new HttpResponseMessage(status)
                : new HttpResponseMessage(status)
                {
                    Content = new StringContent(body)
                };
        });
    }

    private static string CreateUserListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = "user-id",
                email = "manager@corepay.test",
                displayName = "Gerente Regional",
                roleNames = new[] { "Manager" },
                departmentIds = new[] { DepartmentId1, DepartmentId2 }
            }
        });

    private static string CreateUserJson(
        string displayName,
        string role,
        params Guid[] departmentIds) =>
        JsonSerializer.Serialize(new
        {
            id = "user-id",
            email = "manager@corepay.test",
            displayName,
            roleNames = new[] { role },
            departmentIds
        });
}

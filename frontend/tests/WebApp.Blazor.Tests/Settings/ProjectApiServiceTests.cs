using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.SettingsUi;

public class ProjectApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid ExistingProjectId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");

    public ProjectApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IProjectApiService, ProjectApiService>();
    }

    [Fact]
    public async Task GetProjectsAsync_Success_ReturnsProjects()
    {
        ConfigureResponse(
            HttpMethod.Get,
            "/api/v1/projects",
            HttpStatusCode.OK,
            CreateProjectListJson());

        var service = Services.GetRequiredService<IProjectApiService>();
        var result = await service.GetProjectsAsync();

        result.Status.Should().Be(ProjectApiStatus.Success);
        result.Projects.Should().HaveCount(1);
        result.Projects![0].Name.Should().Be("Lima Karttos");
        result.Projects[0].Platform.Should().Be(ProjectPlatform.Lastlink);
        result.Projects[0].IsDefaultAllocationTarget.Should().BeTrue();
    }

    [Fact]
    public async Task GetProjectsAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Get, "/api/v1/projects", HttpStatusCode.Forbidden);

        var service = Services.GetRequiredService<IProjectApiService>();
        var result = await service.GetProjectsAsync();

        result.Status.Should().Be(ProjectApiStatus.Forbidden);
    }

    [Fact]
    public async Task CreateProjectAsync_Success_SerializesEnumAsCamelCase()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/projects");
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateProjectJson("Projeto Hubla Demo", "hubla"))
            };
        });

        var service = Services.GetRequiredService<IProjectApiService>();
        var result = await service.CreateProjectAsync(CreateRequest("Projeto Hubla Demo", ProjectPlatform.Hubla));

        result.Status.Should().Be(ProjectApiStatus.Success);
        result.Project!.Platform.Should().Be(ProjectPlatform.Hubla);
        capturedBody.Should().Contain("\"platform\":\"hubla\"");
    }

    [Fact]
    public async Task CreateProjectAsync_ValidationError_ReturnsValidationStatus()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/projects",
            HttpStatusCode.BadRequest,
            """{"error":"projects.name_required","message":"Name is required."}""");

        var service = Services.GetRequiredService<IProjectApiService>();
        var result = await service.CreateProjectAsync(CreateRequest("", ProjectPlatform.Lastlink));

        result.Status.Should().Be(ProjectApiStatus.ValidationError);
        result.ErrorCode.Should().Be("projects.name_required");
    }

    [Fact]
    public async Task CreateProjectAsync_Conflict_ReturnsConflictStatus()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/projects",
            HttpStatusCode.Conflict,
            """{"error":"projects.duplicate","message":"Project already exists."}""");

        var service = Services.GetRequiredService<IProjectApiService>();
        var result = await service.CreateProjectAsync(CreateRequest("Lima Karttos", ProjectPlatform.Lastlink));

        result.Status.Should().Be(ProjectApiStatus.Conflict);
        result.ErrorCode.Should().Be("projects.duplicate");
    }

    [Fact]
    public async Task UpdateProjectAsync_NotFound_ReturnsNotFoundStatus()
    {
        ConfigureResponse(
            HttpMethod.Put,
            $"/api/v1/projects/{ExistingProjectId}",
            HttpStatusCode.NotFound,
            """{"error":"projects.not_found","message":"Project not found."}""");

        var service = Services.GetRequiredService<IProjectApiService>();
        var result = await service.UpdateProjectAsync(
            ExistingProjectId,
            CreateRequest("Lima Karttos", ProjectPlatform.Lastlink));

        result.Status.Should().Be(ProjectApiStatus.NotFound);
        result.ErrorCode.Should().Be("projects.not_found");
    }

    [Fact]
    public async Task UpdateProjectAsync_Success_ReturnsUpdatedProject()
    {
        ConfigureResponse(
            HttpMethod.Put,
            $"/api/v1/projects/{ExistingProjectId}",
            HttpStatusCode.OK,
            CreateProjectJson("Lima Karttos", "lastlink", ExistingProjectId, isDefaultAllocationTarget: true));

        var service = Services.GetRequiredService<IProjectApiService>();
        var result = await service.UpdateProjectAsync(
            ExistingProjectId,
            CreateRequest("Lima Karttos", ProjectPlatform.Lastlink, isDefaultAllocationTarget: true));

        result.Status.Should().Be(ProjectApiStatus.Success);
        result.Project!.Name.Should().Be("Lima Karttos");
        result.Project.IsDefaultAllocationTarget.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProjectAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Post, "/api/v1/projects", HttpStatusCode.Forbidden);

        var service = Services.GetRequiredService<IProjectApiService>();
        var result = await service.CreateProjectAsync(CreateRequest("Projeto Hubla Demo", ProjectPlatform.Hubla));

        result.Status.Should().Be(ProjectApiStatus.Forbidden);
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

    private static ProjectRequest CreateRequest(
        string name,
        ProjectPlatform platform,
        bool isDefaultAllocationTarget = false) =>
        new(
            name,
            Client: null,
            platform,
            IsActive: true,
            IsDefaultAllocationTarget: isDefaultAllocationTarget,
            ExcludesGoalBonus: false,
            ExcludesSupervisorFixedAllocation: false);

    private static string CreateProjectListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = ExistingProjectId,
                name = "Lima Karttos",
                client = (string?)null,
                platform = "lastlink",
                isActive = true,
                isDefaultAllocationTarget = true,
                excludesGoalBonus = false,
                excludesSupervisorFixedAllocation = false
            }
        });

    private static string CreateProjectJson(
        string name,
        string platform,
        Guid? id = null,
        bool isDefaultAllocationTarget = false) =>
        JsonSerializer.Serialize(new
        {
            id = id ?? Guid.NewGuid(),
            name,
            client = (string?)null,
            platform,
            isActive = true,
            isDefaultAllocationTarget,
            excludesGoalBonus = false,
            excludesSupervisorFixedAllocation = false
        });
}

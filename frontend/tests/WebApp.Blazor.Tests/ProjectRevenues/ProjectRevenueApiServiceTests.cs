using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.ProjectRevenues;

public class ProjectRevenueApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid ExistingRevenueId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid ProjectId = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7");

    public ProjectRevenueApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IProjectRevenueApiService, ProjectRevenueApiService>();
    }

    [Fact]
    public async Task GetRevenuesAsync_Success_ReturnsRevenues()
    {
        ConfigureResponse(
            HttpMethod.Get,
            "/api/v1/project-revenues?month=9&year=2026",
            HttpStatusCode.OK,
            CreateRevenueListJson());

        var service = Services.GetRequiredService<IProjectRevenueApiService>();
        var result = await service.GetRevenuesAsync(new ProjectRevenueListQuery(9, 2026));

        result.Status.Should().Be(ProjectRevenueApiStatus.Success);
        result.Revenues.Should().HaveCount(1);
        result.Revenues![0].Value.Should().Be(1500m);
    }

    [Fact]
    public void BuildListUrl_WithFilters_BuildsExpectedQuery()
    {
        var url = ProjectRevenueApiService.BuildListUrl(new ProjectRevenueListQuery(8, 2026, ProjectId));
        url.Should().Be($"api/v1/project-revenues?month=8&year=2026&projectId={ProjectId}");
    }

    [Fact]
    public async Task CreateRevenueAsync_Success_ReturnsCreatedRevenue()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/project-revenues");
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateRevenueJson(1500m))
            };
        });

        var service = Services.GetRequiredService<IProjectRevenueApiService>();
        var result = await service.CreateRevenueAsync(CreateRequest(1000m, 500m));

        result.Status.Should().Be(ProjectRevenueApiStatus.Success);
        result.Revenue!.Value.Should().Be(1500m);
        capturedBody.Should().Contain("\"valueIgaming\":1000");
        capturedBody.Should().Contain("\"valueVendas\":500");
    }

    [Fact]
    public async Task CreateRevenueAsync_Conflict_ReturnsConflictStatus()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/project-revenues",
            HttpStatusCode.Conflict,
            """{"error":"projectrevenues.duplicate","message":"Duplicate."}""");

        var service = Services.GetRequiredService<IProjectRevenueApiService>();
        var result = await service.CreateRevenueAsync(CreateRequest());

        result.Status.Should().Be(ProjectRevenueApiStatus.Conflict);
        result.ErrorCode.Should().Be("projectrevenues.duplicate");
    }

    [Fact]
    public async Task UpdateRevenueAsync_NotFound_ReturnsNotFoundStatus()
    {
        ConfigureResponse(
            HttpMethod.Put,
            $"/api/v1/project-revenues/{ExistingRevenueId}",
            HttpStatusCode.NotFound,
            """{"error":"projectrevenues.not_found","message":"Not found."}""");

        var service = Services.GetRequiredService<IProjectRevenueApiService>();
        var result = await service.UpdateRevenueAsync(ExistingRevenueId, CreateRequest());

        result.Status.Should().Be(ProjectRevenueApiStatus.NotFound);
    }

    [Fact]
    public async Task GetRevenuesAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Get, "/api/v1/project-revenues?month=9&year=2026", HttpStatusCode.Forbidden);

        var service = Services.GetRequiredService<IProjectRevenueApiService>();
        var result = await service.GetRevenuesAsync(new ProjectRevenueListQuery(9, 2026));

        result.Status.Should().Be(ProjectRevenueApiStatus.Forbidden);
    }

    private void ConfigureResponse(HttpMethod method, string path, HttpStatusCode status, string? body = null)
    {
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(method);
            request.RequestUri!.PathAndQuery.Should().Be(path);
            return body is null
                ? new HttpResponseMessage(status)
                : new HttpResponseMessage(status)
                {
                    Content = new StringContent(body)
                };
        });
    }

    private static ProjectRevenueRequest CreateRequest(decimal igaming = 1000m, decimal vendas = 500m) =>
        new(ProjectId, 9, 2026, igaming, vendas, 0m, null);

    private static string CreateRevenueListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = ExistingRevenueId,
                projectId = ProjectId,
                projectName = "Projeto Demo",
                month = 9,
                year = 2026,
                valueIgaming = 1000m,
                valueVendas = 500m,
                value = 1500m,
                groupPercentage = 0m,
                notes = (string?)null
            }
        });

    private static string CreateRevenueJson(decimal value) =>
        JsonSerializer.Serialize(new
        {
            id = ExistingRevenueId,
            projectId = ProjectId,
            projectName = "Projeto Demo",
            month = 9,
            year = 2026,
            valueIgaming = 1000m,
            valueVendas = 500m,
            value,
            groupPercentage = 0m,
            notes = (string?)null
        });
}

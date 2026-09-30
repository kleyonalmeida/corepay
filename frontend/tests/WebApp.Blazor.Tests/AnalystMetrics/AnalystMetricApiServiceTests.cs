using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.AnalystMetrics;

public class AnalystMetricApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid MetricId = Guid.Parse("6fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid DepartmentId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid CollaboratorId = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7");
    private static readonly Guid ProjectId = Guid.Parse("9fa85f64-5717-4562-b3fc-2c963f66afa8");

    public AnalystMetricApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IAnalystMetricApiService, AnalystMetricApiService>();
    }

    [Fact]
    public async Task GetMetricsAsync_WithFilters_ReturnsItems()
    {
        _httpHandler.Configure(request =>
        {
            request.RequestUri!.PathAndQuery.Should().Be(
                $"/api/v1/analyst-metrics?month=9&year=2026&departmentId={DepartmentId}&collaboratorId={CollaboratorId}&projectId={ProjectId}");
            return JsonResponse(HttpStatusCode.OK, CreateMetricListJson());
        });

        var service = Services.GetRequiredService<IAnalystMetricApiService>();
        var result = await service.GetMetricsAsync(
            new AnalystMetricListQuery(9, 2026, DepartmentId, CollaboratorId, ProjectId));

        result.Status.Should().Be(AnalystMetricApiStatus.Success);
        result.Metrics.Should().ContainSingle();
        result.Metrics![0].FtdTotal.Should().Be(120);
    }

    [Fact]
    public async Task CreateMetricAsync_SendsCountsAndMapsConflict()
    {
        string? body = null;
        _httpHandler.Configure(request =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse(
                HttpStatusCode.Conflict,
                """{"error":"analystmetrics.duplicate","message":"Duplicate."}""");
        });

        var service = Services.GetRequiredService<IAnalystMetricApiService>();
        var result = await service.CreateMetricAsync(CreateRequest());

        result.Status.Should().Be(AnalystMetricApiStatus.Conflict);
        result.ErrorCode.Should().Be("analystmetrics.duplicate");
        body.Should().Contain("\"ftdTotal\":120");
        body.Should().Contain("\"cpaCount\":35");
    }

    [Fact]
    public async Task UpdateMetricAsync_Forbidden_ReturnsForbidden()
    {
        _httpHandler.Configure(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));

        var service = Services.GetRequiredService<IAnalystMetricApiService>();
        var result = await service.UpdateMetricAsync(MetricId, CreateRequest());

        result.Status.Should().Be(AnalystMetricApiStatus.Forbidden);
    }

    private static AnalystMetricRequest CreateRequest() =>
        new(CollaboratorId, ProjectId, 9, 2026, 120, 35);

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string content) =>
        new(status) { Content = new StringContent(content) };

    private static string CreateMetricListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = MetricId,
                collaboratorId = CollaboratorId,
                collaboratorName = "Ana",
                departmentId = DepartmentId,
                departmentName = "Comercial",
                projectId = ProjectId,
                projectName = "Projeto Demo",
                month = 9,
                year = 2026,
                ftdTotal = 120,
                cpaCount = 35
            }
        });
}

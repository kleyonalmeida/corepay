using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Traffic;

public class TrafficInvestmentApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid ExistingInvestmentId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid ProjectId = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7");

    public TrafficInvestmentApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<ITrafficInvestmentApiService, TrafficInvestmentApiService>();
    }

    [Fact]
    public async Task GetInvestmentsAsync_Success_ReturnsInvestments()
    {
        ConfigureResponse(
            HttpMethod.Get,
            "/api/v1/traffic-investments?month=9&year=2026",
            HttpStatusCode.OK,
            CreateInvestmentListJson());

        var service = Services.GetRequiredService<ITrafficInvestmentApiService>();
        var result = await service.GetInvestmentsAsync(new TrafficInvestmentListQuery(9, 2026));

        result.Status.Should().Be(TrafficInvestmentApiStatus.Success);
        result.Investments.Should().HaveCount(1);
        result.Investments![0].MonthlyTotals.TaxAmount.Should().Be(121.50m);
    }

    [Fact]
    public void BuildInvestmentsUrl_WithFilters_BuildsExpectedQuery()
    {
        var url = TrafficInvestmentApiService.BuildInvestmentsUrl(new TrafficInvestmentListQuery(8, 2026, ProjectId));
        url.Should().Be($"api/v1/traffic-investments?month=8&year=2026&projectId={ProjectId}");
    }

    [Fact]
    public async Task CreateInvestmentAsync_Success_ReturnsCreatedInvestment()
    {
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/traffic-investments");
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateInvestmentJson())
            };
        });

        var service = Services.GetRequiredService<ITrafficInvestmentApiService>();
        var result = await service.CreateInvestmentAsync(CreateRequest());

        result.Status.Should().Be(TrafficInvestmentApiStatus.Success);
        result.Investment!.Weeks.Single(week => week.WeekNumber == 1).TaxAmount.Should().Be(121.50m);
    }

    [Fact]
    public async Task CreateInvestmentAsync_Conflict_ReturnsConflictStatus()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/traffic-investments",
            HttpStatusCode.Conflict,
            """{"error":"trafficinvestments.duplicate","message":"Duplicate."}""");

        var service = Services.GetRequiredService<ITrafficInvestmentApiService>();
        var result = await service.CreateInvestmentAsync(CreateRequest());

        result.Status.Should().Be(TrafficInvestmentApiStatus.Conflict);
        result.ErrorCode.Should().Be("trafficinvestments.duplicate");
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

    private static TrafficInvestmentRequest CreateRequest() =>
        new(
            ProjectId,
            9,
            2026,
            4000m,
            [
                new TrafficWeekRequest(
                    1,
                    [],
                    [new TrafficWeekChannelSpendRequest(TrafficMediaChannelDto.Telegram, 1000m)])
            ]);

    private static string CreateInvestmentListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = ExistingInvestmentId,
                projectId = ProjectId,
                projectName = "Projeto Demo",
                month = 9,
                year = 2026,
                monthlyTarget = 4000m,
                monthlyTotals = new
                {
                    requestedAmount = 0m,
                    depositedAmount = 0m,
                    spentAmount = 1000m,
                    taxAmount = 121.50m,
                    totalAmount = 1121.50m,
                    balance = -1121.50m
                }
            }
        });

    private static string CreateInvestmentJson() =>
        JsonSerializer.Serialize(new
        {
            id = ExistingInvestmentId,
            projectId = ProjectId,
            projectName = "Projeto Demo",
            month = 9,
            year = 2026,
            monthlyTarget = 4000m,
            weeks = new[]
            {
                new
                {
                    weekNumber = 1,
                    requestedAmount = 0m,
                    depositedAmount = 0m,
                    spentAmount = 1000m,
                    taxAmount = 121.50m,
                    totalAmount = 1121.50m,
                    balance = -1121.50m,
                    suggestedNext = 2121.50m,
                    deposits = Array.Empty<object>(),
                    channelSpends = new[]
                    {
                        new { channel = "telegram", amount = 1000m }
                    }
                }
            },
            monthlyTotals = new
            {
                requestedAmount = 0m,
                depositedAmount = 0m,
                spentAmount = 1000m,
                taxAmount = 121.50m,
                totalAmount = 1121.50m,
                balance = -1121.50m
            }
        });
}

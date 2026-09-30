using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Dashboard;

public sealed class DashboardApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public DashboardApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IDashboardApiService, DashboardApiService>();
    }

    [Fact]
    public void BuildUrl_WithFilters_BuildsExpectedQuery()
    {
        DashboardApiService.BuildUrl(new DashboardQuery(8, 2026))
            .Should().Be("api/v1/dashboard?month=8&year=2026");
    }

    [Fact]
    public void BuildUrl_WithoutFilters_UsesEndpointOnly()
    {
        DashboardApiService.BuildUrl().Should().Be("api/v1/dashboard");
    }

    [Fact]
    public async Task GetAsync_Success_DeserializesPermissionBlocks()
    {
        ConfigureResponse(HttpStatusCode.OK, """
            {
              "month": 8,
              "year": 2026,
              "payrollStats": {
                "totalPayrolls": 4,
                "awaitingApproval": 1,
                "approved": 2,
                "rejected": 1,
                "totalToPay": 4200,
                "totalPaid": 2800
              },
              "activeCollaborators": 12,
              "recentPayrolls": []
            }
            """);

        var result = await Services.GetRequiredService<IDashboardApiService>().GetAsync();

        result.Status.Should().Be(DashboardApiStatus.Success);
        result.Data!.Month.Should().Be(8);
        result.Data.PayrollStats!.TotalToPay.Should().Be(4_200m);
        result.Data.ActiveCollaborators.Should().Be(12);
    }

    [Fact]
    public async Task GetAsync_ValidationError_ReturnsApiMessage()
    {
        ConfigureResponse(HttpStatusCode.BadRequest, """
            { "error": "dashboard.invalid_month", "message": "Invalid month." }
            """);

        var result = await Services.GetRequiredService<IDashboardApiService>()
            .GetAsync(new DashboardQuery(13, 2026));

        result.Status.Should().Be(DashboardApiStatus.ValidationError);
        result.ErrorCode.Should().Be("dashboard.invalid_month");
    }

    [Fact]
    public async Task GetAsync_Cancelled_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var service = Services.GetRequiredService<IDashboardApiService>();

        var act = () => service.GetAsync(cancellationToken: cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private void ConfigureResponse(HttpStatusCode statusCode, string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/dashboard")
            {
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }
}

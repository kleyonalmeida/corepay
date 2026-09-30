using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Cashflow;

public class CashflowApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public CashflowApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<ICashflowApiService, CashflowApiService>();
    }

    [Fact]
    public void BuildListUrl_WithFilters_BuildsExpectedQuery()
    {
        var url = CashflowApiService.BuildListUrl(new CashflowListQuery(9, 2026, CashflowEntryType.Entrada));
        url.Should().Be("api/v1/cashflow?month=9&year=2026&type=entrada");
    }

    [Fact]
    public void BuildReportUrl_WithFilters_BuildsExpectedQuery()
    {
        var url = CashflowApiService.BuildReportUrl(new CashflowReportQuery(9, 2026));
        url.Should().Be("api/v1/cashflow/report?month=9&year=2026");
    }

    [Fact]
    public async Task GetEntriesAsync_Success_ReturnsSummary()
    {
        ConfigureResponse(
            HttpMethod.Get,
            "/api/v1/cashflow",
            HttpStatusCode.OK,
            """
            {
              "summary": { "totalEntradas": 1000, "totalSaidas": 300, "saldo": 700, "count": 2 },
              "items": [],
              "filterOptions": { "projects": [], "departments": [], "paymentMethods": [] }
            }
            """);

        var service = Services.GetRequiredService<ICashflowApiService>();
        var result = await service.GetEntriesAsync(new CashflowListQuery(9, 2026));

        result.Status.Should().Be(CashflowApiStatus.Success);
        result.Data!.Summary.Saldo.Should().Be(700m);
    }

    [Fact]
    public async Task GetEntriesAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Get, "/api/v1/cashflow", HttpStatusCode.Forbidden, "{}");

        var service = Services.GetRequiredService<ICashflowApiService>();
        var result = await service.GetEntriesAsync();

        result.Status.Should().Be(CashflowApiStatus.Forbidden);
    }

    [Fact]
    public async Task GetReportAsync_Success_ReturnsAggregations()
    {
        ConfigureResponse(
            HttpMethod.Get,
            "/api/v1/cashflow/report",
            HttpStatusCode.OK,
            """
            {
              "summary": { "totalEntradas": 1200, "totalSaidas": 450, "saldo": 750, "count": 4 },
              "byProject": [
                {
                  "projectId": null,
                  "projectName": "Sem projeto",
                  "totalEntradas": 200,
                  "totalSaidas": 0,
                  "saldo": 200,
                  "count": 1
                }
              ],
              "byPaymentMethod": [
                {
                  "paymentMethodId": "8fa85f64-5717-4562-b3fc-2c963f66afa7",
                  "paymentMethodName": "Pix",
                  "totalSaidas": 450,
                  "count": 2
                }
              ]
            }
            """);

        var service = Services.GetRequiredService<ICashflowApiService>();
        var result = await service.GetReportAsync(new CashflowReportQuery(3, 2026));

        result.Status.Should().Be(CashflowApiStatus.Success);
        result.Data!.Summary.Saldo.Should().Be(750m);
        result.Data.ByProject.Should().ContainSingle(p => p.ProjectName == "Sem projeto");
        result.Data.ByPaymentMethod.Should().ContainSingle(m => m.PaymentMethodName == "Pix");
    }

    [Fact]
    public async Task GetReportAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Get, "/api/v1/cashflow/report", HttpStatusCode.Forbidden, "{}");

        var service = Services.GetRequiredService<ICashflowApiService>();
        var result = await service.GetReportAsync(new CashflowReportQuery(9, 2026));

        result.Status.Should().Be(CashflowApiStatus.Forbidden);
    }

    [Fact]
    public async Task DeleteEntryAsync_Success_ReturnsSuccess()
    {
        var entryId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Delete
                && request.RequestUri!.AbsolutePath == $"/api/v1/cashflow/{entryId}")
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var service = Services.GetRequiredService<ICashflowApiService>();
        var result = await service.DeleteEntryAsync(entryId);

        result.Status.Should().Be(CashflowApiStatus.Success);
    }

    [Fact]
    public async Task DeleteEntryAsync_Forbidden_ReturnsForbidden()
    {
        var entryId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");
        ConfigureResponse(HttpMethod.Delete, $"/api/v1/cashflow/{entryId}", HttpStatusCode.Forbidden, "{}");

        var service = Services.GetRequiredService<ICashflowApiService>();
        var result = await service.DeleteEntryAsync(entryId);

        result.Status.Should().Be(CashflowApiStatus.Forbidden);
    }

    [Fact]
    public async Task DeleteEntryAsync_NotFound_ReturnsNotFound()
    {
        var entryId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");
        ConfigureResponse(HttpMethod.Delete, $"/api/v1/cashflow/{entryId}", HttpStatusCode.NotFound, "{}");

        var service = Services.GetRequiredService<ICashflowApiService>();
        var result = await service.DeleteEntryAsync(entryId);

        result.Status.Should().Be(CashflowApiStatus.NotFound);
    }

    private void ConfigureResponse(HttpMethod method, string path, HttpStatusCode statusCode, string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == method && request.RequestUri!.AbsolutePath.StartsWith(path, StringComparison.Ordinal))
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

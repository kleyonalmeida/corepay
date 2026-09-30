using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Reports;

public sealed class ReportsApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public ReportsApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IReportsApiService, ReportsApiService>();
    }

    [Fact]
    public void BuildUrl_WithoutFilters_UsesEndpointOnly()
    {
        ReportsApiService.BuildUrl().Should().Be("api/v1/reports/payroll");
    }

    [Fact]
    public void BuildUrl_WithFilters_BuildsExpectedQuery()
    {
        var departmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var projectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        ReportsApiService.BuildUrl(new ReportsQuery(2026, departmentId, projectId))
            .Should().Be(
                $"api/v1/reports/payroll?year=2026&departmentId={departmentId}&projectId={projectId}");
    }

    [Fact]
    public void BuildExportUrl_WithoutFilters_UsesExportEndpointOnly()
    {
        ReportsApiService.BuildExportUrl().Should().Be("api/v1/reports/payroll/export");
    }

    [Fact]
    public void BuildExportUrl_WithFilters_BuildsExpectedQuery()
    {
        var departmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var projectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        ReportsApiService.BuildExportUrl(new ReportsQuery(2026, departmentId, projectId))
            .Should().Be(
                $"api/v1/reports/payroll/export?year=2026&departmentId={departmentId}&projectId={projectId}");
    }

    [Fact]
    public async Task GetPayrollReportAsync_Success_DeserializesReport()
    {
        ConfigureResponse(HttpStatusCode.OK, """
            {
              "year": 2026,
              "summary": {
                "totalYear": 18000,
                "monthlyAverage": 1500,
                "topDepartmentId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                "topDepartmentName": "Comercial",
                "topDepartmentAmount": 12000,
                "topProjectId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                "topProjectName": "Projeto X",
                "topProjectAmount": 7000,
                "collaboratorCount": 2
              },
              "monthlySeries": [{ "month": 1, "amount": 12000 }],
              "byDepartment": [],
              "byProject": [],
              "byCollaborator": [],
              "filterOptions": { "departments": [], "projects": [] }
            }
            """);

        var result = await Services.GetRequiredService<IReportsApiService>().GetPayrollReportAsync();

        result.Status.Should().Be(ReportsApiStatus.Success);
        result.Data!.Year.Should().Be(2026);
        result.Data.Summary.TotalYear.Should().Be(18_000m);
        result.Data.Summary.MonthlyAverage.Should().Be(1_500m);
    }

    [Fact]
    public async Task GetPayrollReportAsync_ValidationError_ReturnsApiMessage()
    {
        ConfigureResponse(HttpStatusCode.BadRequest, """
            { "error": "reports.invalid_year", "message": "Invalid year." }
            """);

        var result = await Services.GetRequiredService<IReportsApiService>()
            .GetPayrollReportAsync(new ReportsQuery(1999));

        result.Status.Should().Be(ReportsApiStatus.ValidationError);
        result.ErrorCode.Should().Be("reports.invalid_year");
    }

    [Fact]
    public async Task GetPayrollReportAsync_Forbidden_ReturnsForbiddenStatus()
    {
        ConfigureResponse(HttpStatusCode.Forbidden, "{}");

        var result = await Services.GetRequiredService<IReportsApiService>().GetPayrollReportAsync();

        result.Status.Should().Be(ReportsApiStatus.Forbidden);
    }

    [Fact]
    public async Task GetPayrollReportAsync_Cancelled_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var service = Services.GetRequiredService<IReportsApiService>();

        var act = () => service.GetPayrollReportAsync(cancellationToken: cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExportPayrollReportAsync_Success_ReturnsFileBytesAndName()
    {
        ConfigureExportResponse(
            HttpStatusCode.OK,
            [0x50, 0x4B, 0x03, 0x04],
            "relatorio-folha-2026.xlsx");

        var result = await Services.GetRequiredService<IReportsApiService>()
            .ExportPayrollReportAsync(new ReportsQuery(2026));

        result.Status.Should().Be(ReportsApiStatus.Success);
        result.FileBytes.Should().NotBeNullOrEmpty();
        result.FileName.Should().Be("relatorio-folha-2026.xlsx");
    }

    [Fact]
    public async Task ExportPayrollReportAsync_ValidationError_ReturnsApiMessage()
    {
        ConfigureExportResponse(
            HttpStatusCode.BadRequest,
            """{ "error": "reports.invalid_year", "message": "Invalid year." }""");

        var result = await Services.GetRequiredService<IReportsApiService>()
            .ExportPayrollReportAsync(new ReportsQuery(1999));

        result.Status.Should().Be(ReportsApiStatus.ValidationError);
        result.ErrorCode.Should().Be("reports.invalid_year");
    }

    [Fact]
    public async Task ExportPayrollReportAsync_Forbidden_ReturnsForbiddenStatus()
    {
        ConfigureExportResponse(HttpStatusCode.Forbidden, "{}");

        var result = await Services.GetRequiredService<IReportsApiService>().ExportPayrollReportAsync();

        result.Status.Should().Be(ReportsApiStatus.Forbidden);
    }

    private void ConfigureResponse(HttpStatusCode statusCode, string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/reports/payroll")
            {
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private void ConfigureExportResponse(HttpStatusCode statusCode, string body) =>
        ConfigureExportResponse(statusCode, System.Text.Encoding.UTF8.GetBytes(body));

    private void ConfigureExportResponse(HttpStatusCode statusCode, byte[] body, string? fileName = null)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/reports/payroll/export")
            {
                var response = new HttpResponseMessage(statusCode)
                {
                    Content = new ByteArrayContent(body)
                };

                if (fileName is not null)
                {
                    response.Content.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    response.Content.Headers.ContentDisposition =
                        new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                        {
                            FileName = fileName
                        };
                }
                else if (body.Length > 0 && body[0] == (byte)'{')
                {
                    response.Content.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                }

                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }
}

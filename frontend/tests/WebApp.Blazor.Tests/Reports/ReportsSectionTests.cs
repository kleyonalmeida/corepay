using System.Net;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Components.Reports;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Reports;

public sealed class ReportsSectionTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private readonly RecordingFileDownloadService _fileDownloadService = new();

    public ReportsSectionTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IReportsApiService, ReportsApiService>();
        Services.AddScoped<IFileDownloadService>(_ => _fileDownloadService);
    }

    [Fact]
    public void ReportsSection_ShowsSummaryCardsAndTables()
    {
        ConfigureResponse(CreateReportJson());

        var cut = Render<ReportsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Total do ano");
            cut.Markup.Should().Contain("Média mensal");
            cut.Markup.Should().Contain("R$ 18.000,00");
            cut.Markup.Should().Contain("R$ 1.500,00");
            cut.Markup.Should().Contain("Comercial");
            cut.Markup.Should().Contain("Projeto X");
            cut.Markup.Should().Contain("Por setor");
            cut.Markup.Should().Contain("Evolução mensal");
        });
    }

    [Fact]
    public void ReportsSection_EmptyReport_ShowsEmptyState()
    {
        ConfigureResponse(CreateEmptyReportJson());

        var cut = Render<ReportsSection>();

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Nenhum dado no período"));
    }

    [Fact]
    public void ReportsSection_Error_ShowsErrorState()
    {
        _httpHandler.Configure(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var cut = Render<ReportsSection>();

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Erro ao carregar relatório"));
    }

    [Fact]
    public void ReportsSection_SwitchesCollaboratorTab()
    {
        ConfigureResponse(CreateReportJson());

        var cut = Render<ReportsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Por colaborador"));
        cut.FindAll("button.ui-tabs__trigger")[2].Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Ana Comercial");
            cut.Markup.Should().Contain("Competências");
        });
    }

    [Fact]
    public void ReportsSection_ShowsExportButtonWhenReportHasData()
    {
        ConfigureResponse(CreateReportJson());

        var cut = Render<ReportsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Exportar Excel"));
    }

    [Fact]
    public void ReportsSection_EmptyReport_DisablesExportButton()
    {
        ConfigureResponse(CreateEmptyReportJson());

        var cut = Render<ReportsSection>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Exportar Excel");
            cut.Find("button.ui-button").HasAttribute("disabled").Should().BeTrue();
        });
    }

    [Fact]
    public void ReportsSection_ExportSuccess_TriggersDownload()
    {
        ConfigureResponse(CreateReportJson());
        ConfigureExportResponse();

        var cut = Render<ReportsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Exportar Excel"));
        cut.FindAll("button.ui-button").First(button => button.TextContent.Contains("Exportar Excel")).Click();

        cut.WaitForAssertion(() =>
        {
            _fileDownloadService.CallCount.Should().Be(1);
            _fileDownloadService.LastFileName.Should().Be("relatorio-folha-2026.xlsx");
            _fileDownloadService.LastContentType.Should().Be(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            _fileDownloadService.LastContent.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void ReportsSection_ExportFailure_ShowsActionError()
    {
        ConfigureResponse(CreateReportJson());
        ConfigureExportResponse(HttpStatusCode.Forbidden);

        var cut = Render<ReportsSection>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Exportar Excel"));
        cut.FindAll("button.ui-button").First(button => button.TextContent.Contains("Exportar Excel")).Click();

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Você não tem permissão para exportar relatórios de folha."));
    }

    private void ConfigureResponse(string body)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/reports/payroll")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private void ConfigureExportResponse(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _httpHandler.Configure(request =>
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/reports/payroll/export")
            {
                if (statusCode == HttpStatusCode.OK)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent([0x50, 0x4B, 0x03, 0x04])
                    };
                    response.Content.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    response.Content.Headers.ContentDisposition =
                        new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                        {
                            FileName = "relatorio-folha-2026.xlsx"
                        };
                    return response;
                }

                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
                };
            }

            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath == "/api/v1/reports/payroll")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateReportJson(), System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
    }

    private static string CreateReportJson() => """
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
          "monthlySeries": [
            { "month": 1, "amount": 12000 },
            { "month": 2, "amount": 6000 }
          ],
          "byDepartment": [
            {
              "departmentId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              "departmentName": "Comercial",
              "amount": 12000,
              "entryCount": 1,
              "payrollCount": 1
            }
          ],
          "byProject": [
            {
              "projectId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
              "projectName": "Projeto X",
              "amount": 7000,
              "entryCount": 1
            }
          ],
          "byCollaborator": [
            {
              "collaboratorId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
              "collaboratorName": "Ana Comercial",
              "departmentId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              "departmentName": "Comercial",
              "amount": 12000,
              "competenceCount": 1
            }
          ],
          "filterOptions": {
            "departments": [
              { "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "name": "Comercial" }
            ],
            "projects": [
              { "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "name": "Projeto X" }
            ]
          }
        }
        """;

    private static string CreateEmptyReportJson() => """
        {
          "year": 2026,
          "summary": {
            "totalYear": 0,
            "monthlyAverage": 0,
            "topDepartmentId": null,
            "topDepartmentName": null,
            "topDepartmentAmount": 0,
            "topProjectId": null,
            "topProjectName": null,
            "topProjectAmount": 0,
            "collaboratorCount": 0
          },
          "monthlySeries": [],
          "byDepartment": [],
          "byProject": [],
          "byCollaborator": [],
          "filterOptions": { "departments": [], "projects": [] }
        }
        """;

    private sealed class RecordingFileDownloadService : IFileDownloadService
    {
        public int CallCount { get; private set; }

        public byte[]? LastContent { get; private set; }

        public string? LastFileName { get; private set; }

        public string? LastContentType { get; private set; }

        public Task SaveFileAsync(byte[] content, string fileName, string contentType)
        {
            CallCount++;
            LastContent = content;
            LastFileName = fileName;
            LastContentType = contentType;
            return Task.CompletedTask;
        }
    }
}

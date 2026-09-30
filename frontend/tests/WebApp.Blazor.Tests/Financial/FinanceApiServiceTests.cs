using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Financial;

public class FinanceApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public FinanceApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IFinanceApiService, FinanceApiService>();
    }

    [Fact]
    public void BuildSummaryUrl_ShouldIncludeAllFilters()
    {
        var departmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var projectId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var url = FinanceApiService.BuildSummaryUrl(new FinanceSummaryQuery(3, 2026, departmentId, projectId));

        url.Should().Be(
            $"api/v1/finance/summary?month=3&year=2026&departmentId={departmentId}&projectId={projectId}");
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldDeserializeResponse()
    {
        var payrollId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        _httpHandler.Configure(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/finance/summary");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateSummaryJson(payrollId))
            };
        });

        var service = Services.GetRequiredService<IFinanceApiService>();
        var result = await service.GetSummaryAsync();

        result.Status.Should().Be(FinanceApiStatus.Success);
        result.Summary!.Payrolls.Should().ContainSingle();
        result.Summary.Payrolls[0].Id.Should().Be(payrollId);
        result.Summary.Payrolls[0].PlatformTotal.Should().Be(400m);
        result.Summary.Payrolls[0].AmountToReceive.Should().Be(800m);
        result.Summary.Payrolls[0].Entries[0].PlatformTotal.Should().Be(400m);
        result.Summary.Payrolls[0].Entries[0].AmountToReceive.Should().Be(800m);
        result.Summary.Payrolls[0].Entries[0].ProjectTotals.Should().ContainSingle()
            .Which.Amount.Should().Be(1200m);
        result.Summary.Payrolls[0].AllowedActions.PayEntry.Should().BeTrue();
        result.Summary.Payrolls[0].AllowedActions.AddCollaborator.Should().BeTrue();
        result.Summary.FilterOptions.Departments.Should().ContainSingle();
        result.Summary.FilterOptions.Projects.Should().ContainSingle();
    }

    [Fact]
    public async Task GetSummaryAsync_Forbidden_ReturnsForbidden()
    {
        _httpHandler.Configure(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("""{"error":"finance.forbidden","message":"Forbidden."}""")
        });

        var service = Services.GetRequiredService<IFinanceApiService>();
        var result = await service.GetSummaryAsync();

        result.Status.Should().Be(FinanceApiStatus.Forbidden);
        result.ErrorCode.Should().Be("finance.forbidden");
    }

    private static string CreateSummaryJson(Guid payrollId) =>
        $$"""
        {
          "payrolls": [
            {
              "id": "{{payrollId}}",
              "departmentId": "44444444-4444-4444-4444-444444444444",
              "departmentName": "Analistas Comerciais",
              "month": 4,
              "year": 2028,
              "status": "approved",
              "grossTotal": 1200.0,
              "platformTotal": 400.0,
              "amountToReceive": 800.0,
              "paidAmount": 0.0,
              "paidCount": 0,
              "entryCount": 1,
              "allowedActions": {
                "approvePayroll": false,
                "rejectPayroll": false,
                "approveEntry": false,
                "edit": false,
                "payPayroll": true,
                "payEntry": true,
                "toggleNf": true,
                "postApprovalAdjustments": true,
                "delete": false,
                "recalculate": false,
                "addCollaborator": true
              },
              "entries": [
                {
                  "id": "55555555-5555-5555-5555-555555555555",
                  "collaboratorId": "66666666-6666-6666-6666-666666666666",
                  "collaboratorName": "Ana Comercial",
                  "careerLevelName": "Analista Júnior",
                  "pixKey": "11999990001",
                  "totalAmount": 1200.0,
                  "platformTotal": 400.0,
                  "amountToReceive": 800.0,
                  "isApproved": true,
                  "isPaid": false,
                  "nfSent": false,
                  "projectTotals": [
                    { "projectId": "77777777-7777-7777-7777-777777777777", "amount": 1200.0 }
                  ]
                }
              ]
            }
          ],
          "filterOptions": {
            "departments": [
              { "id": "44444444-4444-4444-4444-444444444444", "name": "Analistas Comerciais" }
            ],
            "projects": [
              { "id": "77777777-7777-7777-7777-777777777777", "name": "Lastlink Sample" }
            ]
          }
        }
        """;
}

using System.Net;
using System.Text.Json;
using FluentAssertions;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.Payroll;

public class PayrollApiServicePreviewTests
{
    private static readonly Guid PayrollId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EntryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task PreviewEntryAsync_ShouldReturnSuccess()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                result = new
                {
                    totalAmount = 4300m,
                    baseSalary = 3500m,
                    commissionAmount = 0m,
                    goalBonusAmount = 0m,
                    groupCommissionAmount = 0m,
                    platformTotal = 800m
                },
                projectTotals = new[] { new { projectId = Guid.NewGuid(), amount = 100m } }
            }))
        });

        var service = new PayrollApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") });
        var result = await service.PreviewEntryAsync(PayrollId, EntryId, new PreviewPayrollEntryRequestDto());

        result.Status.Should().Be(PayrollApiStatus.Success);
        result.Preview!.Result.TotalAmount.Should().Be(4300m);
        result.Preview.Result.PlatformTotal.Should().Be(800m);
    }

    [Fact]
    public async Task PreviewEntryAsync_ShouldMapForbidden()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("""{"error":"payrolls.department_forbidden","message":"forbidden"}""")
        });

        var service = new PayrollApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") });
        var result = await service.PreviewEntryAsync(PayrollId, EntryId, new PreviewPayrollEntryRequestDto());

        result.Status.Should().Be(PayrollApiStatus.Forbidden);
        result.ErrorCode.Should().Be("payrolls.department_forbidden");
    }

    [Fact]
    public async Task PreviewEntryAsync_ShouldMapValidationError()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"payroll.complement_paying_projects_invalid_sum","message":"invalid"}""")
        });

        var service = new PayrollApiService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") });
        var result = await service.PreviewEntryAsync(PayrollId, EntryId, new PreviewPayrollEntryRequestDto());

        result.Status.Should().Be(PayrollApiStatus.ValidationError);
        result.ErrorCode.Should().Be("payroll.complement_paying_projects_invalid_sum");
    }
}

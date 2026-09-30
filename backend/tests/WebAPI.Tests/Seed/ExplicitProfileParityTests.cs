using System.Net;
using System.Net.Http.Json;
using Core.Domain;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;
using WebAPI.Tests.MasterData;
using WebAPI.Tests.Payroll;

namespace WebAPI.Tests.Seed;

/// <summary>
/// Fase 15.4 — renomear o setor fixture "Tráfego Pago" não altera perfil nem cálculo PaidTraffic.
/// </summary>
public class ExplicitProfileParityTests
{
    [Fact]
    public async Task RenamedPaidTrafficDepartment_ShouldKeepPaidTrafficCalculation_OnPreview()
    {
        await using var factory = new CorePayWebApplicationFactory();
        var client = factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(factory);
        AuthTestHelper.SetBearerToken(client, token);

        Guid trafficDepartmentId;
        Guid projectId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
            trafficDepartmentId = seedEntities.Single(s => s.Key == SeedKeys.Departments.PaidTraffic).EntityId;
            projectId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;
        }

        var collaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.CareerLevels.PaidTrafficSenior,
            "Carlos Tráfego Paridade",
            "11955554444");

        var payroll = await PayrollTestHelper.CreateDraftPayrollWithCollaboratorIdsAsync(
            factory.Services,
            SeedKeys.Departments.PaidTraffic,
            month: 7,
            year: 2036,
            collaboratorId);

        var entryId = payroll.Entries.Single().Id;
        payroll.Entries.Single().CalculationProfile.Should().Be(CalculationProfile.PaidTraffic);

        var previewPayload = new
        {
            trafficProjectEntries = new[]
            {
                new
                {
                    projectId,
                    investedAmount = 10_000m,
                    cpaEntries = new[]
                    {
                        new { houseKey = "betano", kind = "supervised", count = 3 }
                    }
                }
            }
        };

        var beforeRenameResponse = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/preview",
            previewPayload);
        beforeRenameResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var beforeRename = await beforeRenameResponse.Content.ReadFromJsonAsync<PayrollEntryPreviewApiResponse>();
        beforeRename.Should().NotBeNull();
        beforeRename!.Result.CommissionAmount.Should().Be(350m);

        var department = await client.GetFromJsonAsync<DepartmentApiResponse>(
            $"/api/v1/departments/{trafficDepartmentId}",
            MasterDataJsonOptions.Instance);
        department.Should().NotBeNull();

        var renameResponse = await client.PutAsJsonAsync(
            $"/api/v1/departments/{trafficDepartmentId}",
            MasterDataTestHelper.CreateDepartmentPayload(
                name: "Marketing Digital XYZ",
                calculationType: "paidTraffic",
                goalBonusPercentage: department!.GoalBonusPercentage,
                lowRevenueThreshold: department.LowRevenueThreshold,
                lowRevenueBonusPct: department.LowRevenueBonusPct,
                isActive: department.IsActive,
                isAllocatedFixed: department.IsAllocatedFixed,
                routesFixedToLimaKarttos: department.RoutesFixedToLimaKarttos));
        renameResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var renamedDepartment = await renameResponse.Content.ReadFromJsonAsync<DepartmentApiResponse>(
            MasterDataJsonOptions.Instance);
        renamedDepartment!.Name.Should().Be("Marketing Digital XYZ");
        renamedDepartment.CalculationType.Should().Be(CalculationProfile.PaidTraffic);

        var afterRenameResponse = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/preview",
            previewPayload);
        afterRenameResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterRename = await afterRenameResponse.Content.ReadFromJsonAsync<PayrollEntryPreviewApiResponse>();
        afterRename.Should().NotBeNull();
        afterRename!.Result.CommissionAmount.Should().Be(beforeRename.Result.CommissionAmount);
        afterRename.Result.CommissionAmount.Should().Be(350m);

        var detailResponse = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailResponse.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.Entries.Single().CalculationProfile.Should().Be("paidTraffic");
    }

    private sealed record DepartmentApiResponse(
        Guid Id,
        string Name,
        CalculationProfile CalculationType,
        decimal GoalBonusPercentage,
        decimal LowRevenueThreshold,
        decimal LowRevenueBonusPct,
        bool IsActive,
        bool IsAllocatedFixed,
        bool RoutesFixedToLimaKarttos);

    private sealed record PayrollDetailApiResponse(
        Guid Id,
        IReadOnlyList<PayrollEntryDetailApiResponse> Entries);

    private sealed record PayrollEntryDetailApiResponse(
        Guid Id,
        string CalculationProfile);

    private sealed record PayrollEntryPreviewApiResponse(
        PayrollEntryResultApiResponse Result,
        IReadOnlyList<object> ProjectTotals);

    private sealed record PayrollEntryResultApiResponse(
        decimal TotalAmount,
        decimal BaseSalary,
        decimal CommissionAmount,
        decimal GoalBonusAmount,
        decimal GroupCommissionAmount,
        decimal PlatformTotal);
}

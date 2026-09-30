using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;
using WebAPI.Tests.Payroll;

namespace WebAPI.Tests.Finance;

[Collection("WebApiIntegration")]
public class FinanceSummaryEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public FinanceSummaryEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetFinanceSummary_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/finance/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFinanceSummary_WithUserToken_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateUserWithoutPermissionsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/finance/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetFinanceSummary_Manager_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/finance/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Financial")]
    [InlineData("Admin")]
    [InlineData("Director")]
    public async Task GetFinanceSummary_WithFinanceReadRole_ShouldReturnOk(string roleKind)
    {
        var client = await CreateClientForRoleAsync(roleKind);
        var response = await client.GetAsync("/api/v1/finance/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>();
        summary.Should().NotBeNull();
        summary!.FilterOptions.Departments.Should().NotBeEmpty();
        summary.FilterOptions.Projects.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFinanceSummary_DraftWithOneApprovedEntry_ShouldReturnOnlyApprovedEntry()
    {
        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Bruno Comercial",
            "11988887777");

        var payroll = await PayrollTestHelper.CreateDraftPayrollWithCollaboratorIdsAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 4,
            year: 2028,
            await GetCollaboratorIdAsync(SeedKeys.Collaborators.CommercialAnalystActive),
            secondCollaboratorId);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await dbContext.Payrolls
                .Include(p => p.Entries)
                .SingleAsync(p => p.Id == payroll.Id);
            entity.Entries.First().IsApproved = true;
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=4&year=2028");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>();
        summary.Should().NotBeNull();
        summary!.Payrolls.Should().ContainSingle(p => p.Id == payroll.Id);
        summary.Payrolls.Single(p => p.Id == payroll.Id).Entries.Should().ContainSingle();
        summary.Payrolls.Single(p => p.Id == payroll.Id).Entries[0].IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task GetFinanceSummary_ApprovedPayroll_ShouldReturnAllEntries()
    {
        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Carla Comercial",
            "11977776666");

        var payroll = await PayrollTestHelper.CreateDraftPayrollWithCollaboratorIdsAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 5,
            year: 2028,
            await GetCollaboratorIdAsync(SeedKeys.Collaborators.CommercialAnalystActive),
            secondCollaboratorId);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await dbContext.Payrolls
                .Include(p => p.Entries)
                .SingleAsync(p => p.Id == payroll.Id);
            entity.Status = Core.Domain.PayrollStatus.Approved;
            entity.ApprovedBy = "director@test";
            entity.ApprovedAt = DateTimeOffset.UtcNow;
            foreach (var entry in entity.Entries)
            {
                entry.IsApproved = true;
                entry.Payload.CalculatedResult = new Core.Domain.PayrollCalculation.PayrollEntryResult
                {
                    TotalAmount = 1000m
                };
            }

            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=5&year=2028");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>();
        summary!.Payrolls.Single(p => p.Id == payroll.Id).Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFinanceSummary_DraftWithoutApprovedEntries_ShouldOmitPayroll()
    {
        await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 6,
            year: 2028,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=6&year=2028");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>();
        summary!.Payrolls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFinanceSummary_ShouldFilterByDepartmentMonthYearAndProject()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);
        var projectId = await GetProjectIdAsync(SeedKeys.Projects.LastlinkSample);

        await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 7,
            year: 2028,
            totalAmount: 5000m);
        var trafficPayroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.Collaborators.PaidTrafficInactive,
            month: 7,
            year: 2028,
            totalAmount: 3000m);

        var limaKarttosProjectId = await GetProjectIdAsync(SeedKeys.Projects.LimaKarttos);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var trafficEntry = await dbContext.PayrollCollaboratorEntries
                .SingleAsync(e => e.PayrollId == trafficPayroll.Id);
            trafficEntry.Payload.DisplayProjectTotals =
            [
                new Core.Domain.PayrollCalculation.ProjectTotalAllocation(limaKarttosProjectId, 3000m)
            ];
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();

        var departmentResponse = await client.GetAsync(
            $"/api/v1/finance/summary?month=7&year=2028&departmentId={commercialDepartmentId}");
        departmentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await departmentResponse.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Should().OnlyContain(p => p.DepartmentId == commercialDepartmentId);

        var projectResponse = await client.GetAsync(
            $"/api/v1/finance/summary?month=7&year=2028&projectId={projectId}");
        projectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await projectResponse.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Should().ContainSingle()
            .Which.DepartmentId.Should().Be(commercialDepartmentId);

        var monthResponse = await client.GetAsync("/api/v1/finance/summary?month=7&year=2028");
        monthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await monthResponse.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetFinanceSummary_CommercialWithPlatform_ShouldReturnAmountToReceive()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 8,
            year: 2028,
            totalAmount: 5000m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbEntry = await dbContext.PayrollCollaboratorEntries
                .SingleAsync(e => e.PayrollId == payroll.Id);
            dbEntry.IsPaid = false;
            dbEntry.NfSent = false;
            dbEntry.Payload.CalculatedResult = new Core.Domain.PayrollCalculation.PayrollEntryResult
            {
                TotalAmount = 5000m,
                PlatformTotal = 800m
            };
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=8&year=2028");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var group = (await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id);
        var entry = group.Entries.Single();

        entry.TotalAmount.Should().Be(5000m);
        entry.PlatformTotal.Should().Be(800m);
        entry.AmountToReceive.Should().Be(4200m);
        group.GrossTotal.Should().Be(5000m);
        group.PlatformTotal.Should().Be(800m);
        group.AmountToReceive.Should().Be(4200m);
        group.PaidCount.Should().Be(0);
        group.EntryCount.Should().Be(1);
    }

    [Fact]
    public async Task GetFinanceSummary_NonCommercial_ShouldReturnZeroPlatformTotal()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.Collaborators.PaidTrafficInactive,
            month: 9,
            year: 2028,
            totalAmount: 3000m);

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=9&year=2028");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var entry = (await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id)
            .Entries.Single();

        entry.PlatformTotal.Should().Be(0m);
        entry.AmountToReceive.Should().Be(3000m);
        entry.AmountToReceive.Should().Be(entry.TotalAmount);
    }

    [Fact]
    public async Task GetFinanceSummary_ApprovedSnapshot_ShouldIgnoreLaterCareerLevelChanges()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 10,
            year: 2028,
            totalAmount: 4500m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbEntry = await dbContext.PayrollCollaboratorEntries
                .SingleAsync(e => e.PayrollId == payroll.Id);
            dbEntry.Payload.CalculatedResult = new Core.Domain.PayrollCalculation.PayrollEntryResult
            {
                TotalAmount = 4500m,
                PlatformTotal = 500m
            };
            var entity = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
            entity.Status = Core.Domain.PayrollStatus.Approved;
            await dbContext.SaveChangesAsync();

            var level = await dbContext.CareerLevels
                .SingleAsync(c => c.Id == dbEntry.CareerLevelId);
            level.SalesPctBase = 99m;
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=10&year=2028");
        var entry = (await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id)
            .Entries.Single();

        entry.TotalAmount.Should().Be(4500m);
        entry.PlatformTotal.Should().Be(500m);
        entry.AmountToReceive.Should().Be(4000m);
    }

    [Fact]
    public async Task GetFinanceSummary_FinancialApproved_ShouldAllowPayActions()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 11,
            year: 2028,
            totalAmount: 2000m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
            entity.Status = Core.Domain.PayrollStatus.Approved;
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=11&year=2028");
        var group = (await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id);

        group.AllowedActions.PayEntry.Should().BeTrue();
        group.AllowedActions.ToggleNf.Should().BeTrue();
        group.AllowedActions.PayPayroll.Should().BeTrue();
    }

    [Fact]
    public async Task GetFinanceSummary_DirectorApproved_ShouldNotAllowPayActions()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 12,
            year: 2028,
            totalAmount: 2000m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
            entity.Status = Core.Domain.PayrollStatus.Approved;
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateClientForRoleAsync("Director");
        var response = await client.GetAsync("/api/v1/finance/summary?month=12&year=2028");
        var group = (await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id);

        group.AllowedActions.PayEntry.Should().BeFalse();
        group.AllowedActions.ToggleNf.Should().BeFalse();
        group.AllowedActions.PayPayroll.Should().BeFalse();
    }

    [Fact]
    public async Task GetFinanceSummary_PendingApproval_ShouldBlockPayActions()
    {
        var payroll = await PayrollTestHelper.CreatePendingApprovalPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 1,
            year: 2029);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbEntry = await dbContext.PayrollCollaboratorEntries
                .SingleAsync(e => e.PayrollId == payroll.Id);
            dbEntry.IsApproved = true;
            dbEntry.Payload.CalculatedResult = new Core.Domain.PayrollCalculation.PayrollEntryResult
            {
                TotalAmount = 1500m,
                PlatformTotal = 200m
            };
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=1&year=2029");
        var group = (await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id);

        group.AllowedActions.PayEntry.Should().BeFalse();
        group.AllowedActions.ToggleNf.Should().BeFalse();
    }

    [Fact]
    public async Task GetFinanceSummary_ShouldReturnProjectTotals()
    {
        var lastlinkProjectId = await GetProjectIdAsync(SeedKeys.Projects.LastlinkSample);
        var limaKarttosProjectId = await GetProjectIdAsync(SeedKeys.Projects.LimaKarttos);
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 3,
            year: 2029,
            totalAmount: 5000m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbEntry = await dbContext.PayrollCollaboratorEntries
                .SingleAsync(e => e.PayrollId == payroll.Id);
            dbEntry.Payload.CalculatedResult = new Core.Domain.PayrollCalculation.PayrollEntryResult
            {
                TotalAmount = 5000m,
                PlatformTotal = 800m
            };
            dbEntry.Payload.DisplayProjectTotals =
            [
                new Core.Domain.PayrollCalculation.ProjectTotalAllocation(lastlinkProjectId, 3200m),
                new Core.Domain.PayrollCalculation.ProjectTotalAllocation(limaKarttosProjectId, 1800m)
            ];
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=3&year=2029");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var entry = (await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id)
            .Entries.Single();

        entry.ProjectTotals.Should().HaveCount(2);
        entry.ProjectTotals.Should().Contain(t => t.ProjectId == lastlinkProjectId && t.Amount == 3200m);
        entry.ProjectTotals.Should().Contain(t => t.ProjectId == limaKarttosProjectId && t.Amount == 1800m);
        entry.ProjectTotals.Sum(t => t.Amount).Should().Be(5000m);
    }

    [Fact]
    public async Task GetFinanceSummary_CommercialProjectTotals_ShouldReconcileWithTotal()
    {
        var lastlinkProjectId = await GetProjectIdAsync(SeedKeys.Projects.LastlinkSample);
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 4,
            year: 2029,
            totalAmount: 4800m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbEntry = await dbContext.PayrollCollaboratorEntries
                .SingleAsync(e => e.PayrollId == payroll.Id);
            dbEntry.Payload.CalculatedResult = new Core.Domain.PayrollCalculation.PayrollEntryResult
            {
                TotalAmount = 4800m,
                PlatformTotal = 600m
            };
            dbEntry.Payload.DisplayProjectTotals =
            [
                new Core.Domain.PayrollCalculation.ProjectTotalAllocation(lastlinkProjectId, 4800m)
            ];
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=4&year=2029");
        var entry = (await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id)
            .Entries.Single();

        entry.TotalAmount.Should().Be(4800m);
        entry.PlatformTotal.Should().Be(600m);
        entry.AmountToReceive.Should().Be(4200m);
        entry.ProjectTotals.Sum(t => t.Amount).Should().Be(entry.TotalAmount);
        entry.ProjectTotals.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FinanceProjectTotalApiResponse(lastlinkProjectId, 4800m));
    }

    [Fact]
    public async Task GetFinanceSummary_ProjectFilter_ShouldReturnAllProjectTotalsForMatchingEntry()
    {
        var lastlinkProjectId = await GetProjectIdAsync(SeedKeys.Projects.LastlinkSample);
        var limaKarttosProjectId = await GetProjectIdAsync(SeedKeys.Projects.LimaKarttos);
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 5,
            year: 2029,
            totalAmount: 3000m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbEntry = await dbContext.PayrollCollaboratorEntries
                .SingleAsync(e => e.PayrollId == payroll.Id);
            dbEntry.Payload.DisplayProjectTotals =
            [
                new Core.Domain.PayrollCalculation.ProjectTotalAllocation(lastlinkProjectId, 2000m),
                new Core.Domain.PayrollCalculation.ProjectTotalAllocation(limaKarttosProjectId, 1000m)
            ];
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync(
            $"/api/v1/finance/summary?month=5&year=2029&projectId={lastlinkProjectId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var entry = (await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id)
            .Entries.Single();

        entry.ProjectTotals.Should().HaveCount(2);
        entry.ProjectTotals.Should().Contain(t => t.ProjectId == lastlinkProjectId && t.Amount == 2000m);
        entry.ProjectTotals.Should().Contain(t => t.ProjectId == limaKarttosProjectId && t.Amount == 1000m);
    }

    [Fact]
    public async Task GetFinanceSummary_ApprovedSnapshot_ShouldUsePersistedProjectTotals()
    {
        var lastlinkProjectId = await GetProjectIdAsync(SeedKeys.Projects.LastlinkSample);
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 6,
            year: 2029,
            totalAmount: 3500m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbEntry = await dbContext.PayrollCollaboratorEntries
                .SingleAsync(e => e.PayrollId == payroll.Id);
            dbEntry.Payload.CalculatedResult = new Core.Domain.PayrollCalculation.PayrollEntryResult
            {
                TotalAmount = 3500m,
                PlatformTotal = 400m
            };
            dbEntry.Payload.DisplayProjectTotals =
            [
                new Core.Domain.PayrollCalculation.ProjectTotalAllocation(lastlinkProjectId, 3500m)
            ];
            var entity = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
            entity.Status = Core.Domain.PayrollStatus.Approved;
            await dbContext.SaveChangesAsync();

            var level = await dbContext.CareerLevels
                .SingleAsync(c => c.Id == dbEntry.CareerLevelId);
            level.SalesPctBase = 99m;
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/finance/summary?month=6&year=2029");
        var entry = (await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id)
            .Entries.Single();

        entry.ProjectTotals.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new FinanceProjectTotalApiResponse(lastlinkProjectId, 3500m));
    }

    [Fact]
    public async Task GetFinanceSummary_AfterPayAndNfMutations_ShouldReflectUpdatedFlags()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 2,
            year: 2029,
            totalAmount: 2500m);

        Guid entryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await dbContext.Payrolls
                .Include(p => p.Entries)
                .SingleAsync(p => p.Id == payroll.Id);
            entity.Status = Core.Domain.PayrollStatus.Approved;
            var dbEntry = entity.Entries.Single();
            dbEntry.IsPaid = false;
            dbEntry.NfSent = false;
            dbEntry.Payload.CalculatedResult = new Core.Domain.PayrollCalculation.PayrollEntryResult
            {
                TotalAmount = 2500m,
                PlatformTotal = 300m
            };
            entryId = dbEntry.Id;
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateFinancialClientAsync();
        var payResponse = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/pay",
            new { isPaid = true });
        payResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var nfResponse = await client.PutAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/nf",
            new { nfSent = true });
        nfResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var summaryResponse = await client.GetAsync("/api/v1/finance/summary?month=2&year=2029");
        var group = (await summaryResponse.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>())!
            .Payrolls.Single(p => p.Id == payroll.Id);
        var entry = group.Entries.Single(e => e.Id == entryId);

        entry.IsPaid.Should().BeTrue();
        entry.NfSent.Should().BeTrue();
        group.PaidCount.Should().Be(1);
        group.PaidAmount.Should().Be(2200m);
    }

    private async Task<HttpClient> CreateFinancialClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateFinancialUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<HttpClient> CreateClientForRoleAsync(string roleKind)
    {
        var client = _factory.CreateClient();
        var (_, _, token) = roleKind switch
        {
            "Financial" => await AuthTestHelper.CreateFinancialUserAsync(_factory),
            "Admin" => await AuthTestHelper.CreateAdminUserAsync(_factory),
            "Director" => await AuthTestHelper.CreateDirectorUserAsync(_factory),
            _ => throw new ArgumentOutOfRangeException(nameof(roleKind))
        };

        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<Guid> GetCollaboratorIdAsync(string seedKey)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.SeedEntities
            .Where(s => s.Key == seedKey)
            .Select(s => s.EntityId)
            .SingleAsync();
    }

    private async Task<Guid> GetProjectIdAsync(string seedKey)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.SeedEntities
            .Where(s => s.Key == seedKey)
            .Select(s => s.EntityId)
            .SingleAsync();
    }

    private sealed record FinanceSummaryApiResponse(
        IReadOnlyList<FinancePayrollGroupApiResponse> Payrolls,
        FinanceFilterOptionsApiResponse FilterOptions);

    private sealed record FinanceFilterOptionsApiResponse(
        IReadOnlyList<FinanceDepartmentOptionApiResponse> Departments,
        IReadOnlyList<FinanceProjectOptionApiResponse> Projects);

    private sealed record FinanceDepartmentOptionApiResponse(Guid Id, string Name);

    private sealed record FinanceProjectOptionApiResponse(Guid Id, string Name);

    private sealed record FinancePayrollGroupApiResponse(
        Guid Id,
        Guid DepartmentId,
        string DepartmentName,
        int Month,
        int Year,
        string Status,
        decimal GrossTotal,
        decimal PlatformTotal,
        decimal AmountToReceive,
        decimal PaidAmount,
        int PaidCount,
        int EntryCount,
        FinanceAllowedActionsApiResponse AllowedActions,
        IReadOnlyList<FinanceEntryApiResponse> Entries);

    private sealed record FinanceAllowedActionsApiResponse(
        bool ApprovePayroll,
        bool RejectPayroll,
        bool ApproveEntry,
        bool Edit,
        bool PayPayroll,
        bool PayEntry,
        bool ToggleNf,
        bool PostApprovalAdjustments,
        bool Delete,
        bool Recalculate,
        bool AddCollaborator);

    private sealed record FinanceProjectTotalApiResponse(Guid ProjectId, decimal Amount);

    private sealed record FinanceEntryApiResponse(
        Guid Id,
        Guid CollaboratorId,
        string CollaboratorName,
        string? CareerLevelName,
        string? PixKey,
        decimal TotalAmount,
        decimal PlatformTotal,
        decimal AmountToReceive,
        bool IsApproved,
        bool IsPaid,
        bool NfSent,
        IReadOnlyList<FinanceProjectTotalApiResponse> ProjectTotals);
}

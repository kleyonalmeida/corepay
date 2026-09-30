using System.Net;
using System.Net.Http.Json;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;
using WebAPI.Tests.Seed;

namespace WebAPI.Tests.Payroll;

[Collection("WebApiIntegration")]
public class PayrollsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public PayrollsEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPayrolls_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/payrolls");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPayrolls_WithUserToken_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateUserWithoutPermissionsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/payrolls");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPayrolls_AsAdmin_ShouldReturnFilteredAndOrderedPayrolls()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);

        await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 10,
            year: 2027,
            totalAmount: 10_000m);
        await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.Collaborators.PaidTrafficInactive,
            month: 11,
            year: 2027,
            totalAmount: 8_000m);

        var client = await CreateAdminClientAsync();

        var allResponse = await client.GetAsync("/api/v1/payrolls");
        allResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allPayrolls = await ReadPayrollsAsync(allResponse);
        allPayrolls.Should().HaveCountGreaterThanOrEqualTo(2);
        allPayrolls[0].Year.Should().BeGreaterThanOrEqualTo(allPayrolls[^1].Year);

        var statusResponse = await client.GetAsync("/api/v1/payrolls?status=paid");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadPayrollsAsync(statusResponse))!
            .Should().OnlyContain(p => p.Status == "paid");

        var monthResponse = await client.GetAsync("/api/v1/payrolls?month=10&year=2027");
        monthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadPayrollsAsync(monthResponse))!
            .Should().OnlyContain(p => p.Month == 10 && p.Year == 2027);

        var searchResponse = await client.GetAsync("/api/v1/payrolls?search=Ana");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadPayrollsAsync(searchResponse))!
            .Should().Contain(p => p.DepartmentId == commercialDepartmentId);

        var departmentResponse = await client.GetAsync($"/api/v1/payrolls?departmentId={trafficDepartmentId}");
        departmentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadPayrollsAsync(departmentResponse))!
            .Should().OnlyContain(p => p.DepartmentId == trafficDepartmentId);
    }

    [Fact]
    public async Task GetPayrollById_WhenMissing_ShouldReturnNotFound()
    {
        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/v1/payrolls/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateEntry_WithEntryFromAnotherPayroll_ShouldReturnNotFoundWithoutMutation()
    {
        var firstPayroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 11,
            year: 2098,
            SeedKeys.Collaborators.CommercialAnalystActive);
        var secondPayroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 12,
            year: 2098,
            SeedKeys.Collaborators.CommercialAnalystActive);
        var foreignEntryId = secondPayroll.Entries.Single().Id;

        var client = await CreateAdminClientAsync();
        var response = await client.PutAsJsonAsync(
            $"/api/v1/payrolls/{firstPayroll.Id}/entries/{foreignEntryId}",
            new
            {
                entryId = foreignEntryId,
                finalSalary = 999_999m
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var foreignEntry = await dbContext.PayrollCollaboratorEntries
            .AsNoTracking()
            .SingleAsync(entry => entry.Id == foreignEntryId);
        foreignEntry.PayrollId.Should().Be(secondPayroll.Id);
        foreignEntry.FinalSalary.Should().NotBe(999_999m);
    }

    [Fact]
    public async Task UpdatePayroll_WithServerOwnedStatusTotalAndActorFields_ShouldRejectJsonWithoutMutation()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 10,
            year: 2098,
            SeedKeys.Collaborators.CommercialAnalystActive);
        var collaboratorId = payroll.Entries.Single().CollaboratorId;
        var client = await CreateAdminClientAsync();
        using var content = new StringContent(
            $$"""
            {
              "collaboratorIds": ["{{collaboratorId}}"],
              "status": "paid",
              "totalAmount": 999999,
              "submittedBy": "attacker",
              "approvedBy": "attacker",
              "approvedAt": "2098-01-01T00:00:00Z"
            }
            """,
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PutAsync($"/api/v1/payrolls/{payroll.Id}", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await dbContext.Payrolls.AsNoTracking().SingleAsync(item => item.Id == payroll.Id);
        persisted.Status.Should().Be(PayrollStatus.Draft);
        persisted.SubmittedBy.Should().BeNull();
        persisted.ApprovedBy.Should().BeNull();
        persisted.ApprovedAt.Should().BeNull();
        persisted.TotalAmount.Should().Be(payroll.TotalAmount);
    }

    [Fact]
    public async Task Manager_WithSingleDepartment_ShouldOnlySeeOwnPayrolls()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);

        var commercialPayroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 3,
            year: 2026);
        await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.Collaborators.PaidTrafficInactive,
            month: 3,
            year: 2026);

        var client = await CreateManagerClientAsync(commercialDepartmentId);

        var listResponse = await client.GetAsync("/api/v1/payrolls?month=3&year=2026");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payrolls = await ReadPayrollsAsync(listResponse);
        payrolls.Should().ContainSingle();
        payrolls[0].DepartmentId.Should().Be(commercialDepartmentId);

        var forbiddenFilterResponse = await client.GetAsync(
            $"/api/v1/payrolls?departmentId={trafficDepartmentId}");
        forbiddenFilterResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var trafficPayrollId = await GetPayrollIdByDepartmentAndCompetenceAsync(
            trafficDepartmentId,
            3,
            2026);
        var getByIdResponse = await client.GetAsync($"/api/v1/payrolls/{trafficPayrollId}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var ownResponse = await client.GetAsync($"/api/v1/payrolls/{commercialPayroll.Id}");
        ownResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DuplicatePaidPayroll_ShouldCreateDraftWithZeroedFlagsAndSameValues()
    {
        var paidPayroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 5,
            year: 2026,
            totalAmount: 15_750m);

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsync($"/api/v1/payrolls/{paidPayroll.Id}/duplicate", null);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<PayrollSummaryApiResponse>();
        created.Should().NotBeNull();
        created!.Status.Should().Be("draft");
        created.Month.Should().Be(6);
        created.Year.Should().Be(2026);
        created.TotalAmount.Should().Be(15_750m);
        created.EntryCount.Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var source = await dbContext.Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == paidPayroll.Id);
        source.Status.Should().Be(Core.Domain.PayrollStatus.Paid);
        source.Entries.Single().IsApproved.Should().BeTrue();
        source.Entries.Single().IsPaid.Should().BeTrue();
        source.Entries.Single().NfSent.Should().BeTrue();

        var duplicate = await dbContext.Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == created.Id);
        duplicate.Status.Should().Be(Core.Domain.PayrollStatus.Draft);
        duplicate.SubmittedBy.Should().BeNull();
        duplicate.ApprovedBy.Should().BeNull();
        duplicate.ApprovedAt.Should().BeNull();
        duplicate.RejectionComment.Should().BeNull();

        var duplicateEntry = duplicate.Entries.Single();
        duplicateEntry.IsApproved.Should().BeFalse();
        duplicateEntry.IsPaid.Should().BeFalse();
        duplicateEntry.NfSent.Should().BeFalse();
        duplicateEntry.CollaboratorName.Should().Be(DevelopmentFixtureData.ActiveCollaboratorName);
        duplicateEntry.FullBaseSalary.Should().Be(3500m);
        duplicateEntry.FinalSalary.Should().Be(4200m);
        duplicateEntry.Payload.BonusEntries.Should().HaveCount(1);
        duplicateEntry.Payload.DeductionEntries.Should().HaveCount(1);
        duplicateEntry.Id.Should().NotBe(source.Entries.Single().Id);
    }

    [Fact]
    public async Task DuplicatePaidPayroll_FromDecember_ShouldRollYearForward()
    {
        var paidPayroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 12,
            year: 2028);

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsync($"/api/v1/payrolls/{paidPayroll.Id}/duplicate", null);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<PayrollSummaryApiResponse>();
        created!.Month.Should().Be(1);
        created.Year.Should().Be(2029);
    }

    [Fact]
    public async Task DuplicatePayroll_WhenTargetCompetenceExists_ShouldReturnConflict()
    {
        var paidPayroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 7,
            year: 2026);

        await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 8,
            year: 2026);

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsync($"/api/v1/payrolls/{paidPayroll.Id}/duplicate", null);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetFormOptions_AsAdmin_ShouldReturnAllDepartments()
    {
        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync("/api/v1/payrolls/form-options");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var options = await response.Content.ReadFromJsonAsync<PayrollFormOptionsApiResponse>();
        options.Should().NotBeNull();
        options!.Departments.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetFormOptions_AsManager_ShouldReturnOnlyAssignedDepartments()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var client = await CreateManagerClientAsync(commercialDepartmentId);
        var response = await client.GetAsync("/api/v1/payrolls/form-options");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var options = await response.Content.ReadFromJsonAsync<PayrollFormOptionsApiResponse>();
        options!.Departments.Should().ContainSingle();
        options.Departments[0].Id.Should().Be(commercialDepartmentId);
    }

    [Fact]
    public async Task CreatePayroll_AsAdmin_ShouldCreateDraftWithSnapshot()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var collaboratorId = await AuthTestHelper.GetCollaboratorIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/payrolls", new
        {
            departmentId = commercialDepartmentId,
            month = 1,
            year = 2031,
            collaboratorIds = new[] { collaboratorId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        created.Should().NotBeNull();
        created!.Status.Should().Be("draft");
        created.TotalAmount.Should().Be(0m);
        created.Entries.Should().ContainSingle();
        created.Entries[0].CollaboratorName.Should().Be(DevelopmentFixtureData.ActiveCollaboratorName);
        created.Entries[0].PixKey.Should().Be(DevelopmentFixtureData.ActiveCollaboratorPixKey);
        created.Entries[0].FullBaseSalary.Should().Be(3500m);
        created.Entries[0].CalculationProfile.Should().Be("commercialAnalyst");
        created.Entries[0].IsApproved.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePayroll_WithDuplicateCompetence_ShouldReturnConflict()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var collaboratorId = await AuthTestHelper.GetCollaboratorIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Collaborators.CommercialAnalystActive);

        await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 2,
            year: 2031,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/payrolls", new
        {
            departmentId = commercialDepartmentId,
            month = 2,
            year = 2031,
            collaboratorIds = new[] { collaboratorId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePayroll_WithInactiveCollaborator_ShouldReturnBadRequest()
    {
        var trafficDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);
        var inactiveCollaboratorId = await AuthTestHelper.GetCollaboratorIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Collaborators.PaidTrafficInactive);

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/payrolls", new
        {
            departmentId = trafficDepartmentId,
            month = 3,
            year = 2031,
            collaboratorIds = new[] { inactiveCollaboratorId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayroll_WithCollaboratorFromOtherDepartment_ShouldReturnBadRequest()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficCollaboratorId = await AuthTestHelper.GetCollaboratorIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Collaborators.PaidTrafficInactive);

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/payrolls", new
        {
            departmentId = commercialDepartmentId,
            month = 4,
            year = 2031,
            collaboratorIds = new[] { trafficCollaboratorId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayroll_WithoutCollaborators_ShouldReturnBadRequest()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/payrolls", new
        {
            departmentId = commercialDepartmentId,
            month = 5,
            year = 2035,
            collaboratorIds = Array.Empty<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("payrolls.entries_required");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = await dbContext.Payrolls.AnyAsync(p =>
            p.DepartmentId == commercialDepartmentId && p.Month == 5 && p.Year == 2035);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePayroll_RemovingAllCollaborators_ShouldReturnBadRequest()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 6,
            year: 2035,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateAdminClientAsync();
        var response = await client.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", new
        {
            collaboratorIds = Array.Empty<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("payrolls.entries_required");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reloaded = await dbContext.Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == payroll.Id);
        reloaded.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task SubmitPayroll_WithoutEntries_ShouldReturnBadRequest()
    {
        var payrollId = await CreateEmptyDraftPayrollAsync(month: 7, year: 2035);

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsync($"/api/v1/payrolls/{payrollId}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("payrolls.entries_required");
    }

    [Fact]
    public async Task DuplicatePayroll_FromEmptySource_ShouldReturnBadRequest()
    {
        var payrollId = await CreateEmptyDraftPayrollAsync(month: 8, year: 2035);

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsync($"/api/v1/payrolls/{payrollId}/duplicate", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("payrolls.entries_required");
    }

    [Fact]
    public async Task Manager_CreatePayrollOutsideDepartment_ShouldReturnForbidden()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);
        var trafficCollaboratorId = await AuthTestHelper.GetCollaboratorIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Collaborators.PaidTrafficInactive);

        var client = await CreateManagerClientAsync(commercialDepartmentId);
        var response = await client.PostAsJsonAsync("/api/v1/payrolls", new
        {
            departmentId = trafficDepartmentId,
            month = 5,
            year = 2031,
            collaboratorIds = new[] { trafficCollaboratorId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPayrollById_ShouldReturnEntries()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 6,
            year: 2031,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.Entries.Should().ContainSingle();
        detail.EntryCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdatePayroll_WhenPendingApproval_ShouldReturnConflict()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 7,
            year: 2031,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
            entity.Status = Core.Domain.PayrollStatus.PendingApproval;
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateAdminClientAsync();
        var response = await client.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", new
        {
            collaboratorIds = Array.Empty<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdatePayroll_AsDraft_ShouldSyncCollaborators()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 8,
            year: 2031,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Carlos Comercial",
            "11988887777");

        var client = await CreateAdminClientAsync();
        var response = await client.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", new
        {
            collaboratorIds = new[] { secondCollaboratorId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        updated!.Entries.Should().ContainSingle();
        updated.Entries[0].CollaboratorName.Should().Be("Carlos Comercial");
    }

    [Fact]
    public async Task RejectedPayroll_Manager_ShouldHideApprovedEntries()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Carlos Comercial",
            "11988887777");

        var payroll = await PayrollTestHelper.CreateRejectedPayrollWithApprovedEntryAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            secondCollaboratorId,
            month: 9,
            year: 2031);

        var managerClient = await CreateManagerClientAsync(commercialDepartmentId);
        var managerResponse = await managerClient.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        managerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var managerDetail = await managerResponse.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        managerDetail!.Entries.Should().ContainSingle();
        managerDetail.Entries[0].CollaboratorName.Should().Be("Carlos Comercial");
        managerDetail.EntryCount.Should().Be(2);

        var adminClient = await CreateAdminClientAsync();
        var adminResponse = await adminClient.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        var adminDetail = await adminResponse.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        adminDetail!.Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task RejectedPayroll_Manager_Update_ShouldPreserveHiddenApprovedEntry()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Carlos Comercial",
            "11988887777");

        var payroll = await PayrollTestHelper.CreateRejectedPayrollWithApprovedEntryAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            secondCollaboratorId,
            month: 10,
            year: 2031);

        var managerClient = await CreateManagerClientAsync(commercialDepartmentId);
        var response = await managerClient.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", new
        {
            collaboratorIds = new[] { secondCollaboratorId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reloaded = await dbContext.Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == payroll.Id);
        reloaded.Entries.Should().HaveCount(2);
        reloaded.Entries.Should().Contain(e => e.IsApproved);
    }

    [Fact]
    public async Task PreviewEntry_CommercialTwoProjects_ShouldRecalculateTotalOnChange()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 11,
            year: 2031,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var lastlinkId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;
        var hublaId = seedEntities.Single(s => s.Key == SeedKeys.Projects.HublaSample).EntityId;

        var entryId = payroll.Entries.Single().Id;
        var client = await CreateAdminClientAsync();

        var initialRequest = new
        {
            commercialProjectEntries = new[]
            {
                new
                {
                    projectId = lastlinkId,
                    platform = "lastlink",
                    ftdTotal = 100,
                    ftdSuperbet = 0,
                    isFtdGoalReached = false,
                    isProjectFtdGoalReached = false,
                    cpaCount = 0,
                    salesAmount = 10_000m,
                    isSalesGoalReached = false,
                    isProjectSalesGoalReached = false,
                    rev = 0m
                },
                new
                {
                    projectId = hublaId,
                    platform = "hubla",
                    ftdTotal = 0,
                    ftdSuperbet = 0,
                    isFtdGoalReached = false,
                    isProjectFtdGoalReached = false,
                    cpaCount = 0,
                    salesAmount = 10_000m,
                    isSalesGoalReached = false,
                    isProjectSalesGoalReached = false,
                    rev = 0m
                }
            }
        };

        var initialResponse = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/preview",
            initialRequest);
        initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialPreview = await initialResponse.Content.ReadFromJsonAsync<PayrollEntryPreviewApiResponse>();
        initialPreview.Should().NotBeNull();
        initialPreview!.Result.PlatformTotal.Should().Be(800m);

        var updatedRequest = new
        {
            commercialProjectEntries = new[]
            {
                new
                {
                    projectId = lastlinkId,
                    platform = "lastlink",
                    ftdTotal = 100,
                    ftdSuperbet = 0,
                    isFtdGoalReached = false,
                    isProjectFtdGoalReached = false,
                    cpaCount = 0,
                    salesAmount = 20_000m,
                    isSalesGoalReached = false,
                    isProjectSalesGoalReached = false,
                    rev = 0m
                },
                new
                {
                    projectId = hublaId,
                    platform = "hubla",
                    ftdTotal = 0,
                    ftdSuperbet = 0,
                    isFtdGoalReached = false,
                    isProjectFtdGoalReached = false,
                    cpaCount = 0,
                    salesAmount = 10_000m,
                    isSalesGoalReached = false,
                    isProjectSalesGoalReached = false,
                    rev = 0m
                }
            }
        };

        var updatedResponse = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/preview",
            updatedRequest);
        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPreview = await updatedResponse.Content.ReadFromJsonAsync<PayrollEntryPreviewApiResponse>();
        updatedPreview!.Result.TotalAmount.Should().BeGreaterThan(initialPreview.Result.TotalAmount);
        updatedPreview.Result.PlatformTotal.Should().BeGreaterThan(initialPreview.Result.PlatformTotal);
    }

    [Fact]
    public async Task PreviewEntry_HublaProject_ShouldCapPlatformAt4Pct()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 12,
            year: 2031,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var hublaId = seedEntities.Single(s => s.Key == SeedKeys.Projects.HublaSample).EntityId;
        var entryId = payroll.Entries.Single().Id;
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/preview",
            new
            {
                commercialProjectEntries = new[]
                {
                    new
                    {
                        projectId = hublaId,
                        platform = "hubla",
                        ftdTotal = 0,
                        ftdSuperbet = 0,
                        isFtdGoalReached = false,
                        isProjectFtdGoalReached = false,
                        cpaCount = 0,
                        salesAmount = 10_000m,
                        isSalesGoalReached = false,
                        isProjectSalesGoalReached = false,
                        rev = 0m
                    }
                }
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<PayrollEntryPreviewApiResponse>();
        preview!.Result.PlatformTotal.Should().Be(400m);
    }

    [Fact]
    public async Task PreviewEntry_InvalidComplementSum_ShouldReturnUnprocessableEntity()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 1,
            year: 2032,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var projectA = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;
        var projectB = seedEntities.Single(s => s.Key == SeedKeys.Projects.HublaSample).EntityId;
        var entryId = payroll.Entries.Single().Id;
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/preview",
            new
            {
                complementPayingProjects = new[]
                {
                    new { projectId = projectA, percentage = 80m },
                    new { projectId = projectB, percentage = 30m }
                }
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PreviewEntry_TrafficManualRateio_ShouldReturnBadRequest()
    {
        var trafficCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.CareerLevels.PaidTrafficSenior,
            "João Tráfego",
            "11977776666");

        var payroll = await PayrollTestHelper.CreateDraftPayrollWithCollaboratorIdsAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            month: 2,
            year: 2032,
            trafficCollaboratorId);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var projectId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;
        var entryId = payroll.Entries.Single().Id;
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/preview",
            new
            {
                rateioProjectEntries = new[]
                {
                    new { projectId, rateioValue = 500m }
                }
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PreviewEntry_WhenPendingApproval_ShouldReturnConflict()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 3,
            year: 2032,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
            var entity = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
            entity.Status = Core.Domain.PayrollStatus.PendingApproval;
            await dbContext.SaveChangesAsync();
        }

        var entryId = payroll.Entries.Single().Id;
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/preview",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PreviewEntry_EntryFromOtherPayroll_ShouldReturnNotFound()
    {
        var payrollA = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 4,
            year: 2032,
            SeedKeys.Collaborators.CommercialAnalystActive);
        var payrollB = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 5,
            year: 2032,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var foreignEntryId = payrollB.Entries.Single().Id;
        var client = await CreateAdminClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payrollA.Id}/entries/{foreignEntryId}/preview",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PreviewEntry_ManagerOutsideScope_ShouldReturnForbidden()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var trafficCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.CareerLevels.PaidTrafficSenior,
            "Maria Tráfego",
            "11966665555");

        var trafficPayroll = await PayrollTestHelper.CreateDraftPayrollWithCollaboratorIdsAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            month: 6,
            year: 2032,
            trafficCollaboratorId);

        var entryId = trafficPayroll.Entries.Single().Id;
        var client = await CreateManagerClientAsync(commercialDepartmentId);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{trafficPayroll.Id}/entries/{entryId}/preview",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPayrollById_Draft_ShouldIncludeCalculatedEntryTotals()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 7,
            year: 2035,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var lastlinkId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;
        var entryId = payroll.Entries.Single().Id;
        var collaboratorId = payroll.Entries.Single().CollaboratorId;

        var client = await CreateAdminClientAsync();
        var saveResponse = await client.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", new
        {
            collaboratorIds = new[] { collaboratorId },
            entries = new[]
            {
                new
                {
                    entryId,
                    commercialProjectEntries = new[]
                    {
                        new
                        {
                            projectId = lastlinkId,
                            platform = "lastlink",
                            ftdTotal = 100,
                            ftdSuperbet = 0,
                            isFtdGoalReached = false,
                            isProjectFtdGoalReached = false,
                            cpaCount = 0,
                            salesAmount = 10_000m,
                            isSalesGoalReached = false,
                            isProjectSalesGoalReached = false,
                            rev = 0m
                        }
                    }
                }
            }
        });
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.Entries.Should().ContainSingle();
        detail.Entries[0].Result.Should().NotBeNull();
        detail.Entries[0].Result!.TotalAmount.Should().BeGreaterThan(0m);
        detail.Entries[0].ProjectTotals.Should().NotBeNull();
        detail.Entries[0].ProjectTotals!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPayrollById_Paid_ShouldIncludeIsPaidAndNfSent()
    {
        var paidPayroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 4,
            year: 2035,
            totalAmount: 12_500m);

        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/v1/payrolls/{paidPayroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.Entries.Should().ContainSingle();
        detail.Entries[0].IsPaid.Should().BeTrue();
        detail.Entries[0].NfSent.Should().BeTrue();
        detail.Entries[0].IsApproved.Should().BeTrue();
        detail.Entries[0].Result.Should().NotBeNull();
        detail.Entries[0].Result!.TotalAmount.Should().Be(12_500m);
    }

    [Fact]
    public async Task GetPayrollById_WhenPaid_ShouldReturnStoredTotals_AfterCareerLevelRateChange()
    {
        var paidPayroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 5,
            year: 2035,
            totalAmount: 12_500m);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var levelId = seedEntities.Single(s => s.Key == SeedKeys.CareerLevels.CommercialAnalystJunior).EntityId;

        var level = await dbContext.CareerLevels.SingleAsync(c => c.Id == levelId);
        level.FtdRateBase = 99m;
        level.SalesPctBase = 99m;
        await dbContext.SaveChangesAsync();

        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/v1/payrolls/{paidPayroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.TotalAmount.Should().Be(12_500m);
        detail.Entries.Single().Result!.TotalAmount.Should().Be(12_500m);
        detail.Entries.Single().ProjectTotals!.Should().ContainSingle()
            .Which.Amount.Should().Be(12_500m);
    }

    [Fact]
    public async Task GetPayrollById_ShouldReturnEditorOptionsAndPayload()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 7,
            year: 2032,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.EditorOptions.Projects.Should().NotBeEmpty();
        detail.EditorOptions.Departments.Should().NotBeEmpty();
        detail.EditorOptions.CareerLevels.Should().NotBeEmpty();
        detail.Entries.Should().ContainSingle();
        detail.Entries[0].Payload.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePayroll_WithCommercialEntries_ShouldPersistAndRecalculateTotal()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 8,
            year: 2033,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var lastlinkId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;
        var entryId = payroll.Entries.Single().Id;
        var collaboratorId = payroll.Entries.Single().CollaboratorId;

        var client = await CreateAdminClientAsync();
        var response = await client.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", new
        {
            collaboratorIds = new[] { collaboratorId },
            entries = new[]
            {
                new
                {
                    entryId,
                    commercialProjectEntries = new[]
                    {
                        new
                        {
                            projectId = lastlinkId,
                            platform = "lastlink",
                            ftdTotal = 100,
                            ftdSuperbet = 0,
                            isFtdGoalReached = false,
                            isProjectFtdGoalReached = false,
                            cpaCount = 0,
                            salesAmount = 10_000m,
                            isSalesGoalReached = false,
                            isProjectSalesGoalReached = false,
                            rev = 0m
                        }
                    },
                    bonusEntries = new[]
                    {
                        new { projectId = lastlinkId, value = 500m, justification = "Campanha" }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        updated!.TotalAmount.Should().BeGreaterThan(0m);

        var reloaded = await dbContext.Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == payroll.Id);
        reloaded.TotalAmount.Should().Be(updated.TotalAmount);
        reloaded.Entries.Single().Payload.CommercialProjectEntries.Should().ContainSingle();
        reloaded.Entries.Single().Payload.BonusEntries.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdatePayroll_WhenReopened_ShouldShowSameTotals()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 9,
            year: 2033,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var lastlinkId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;
        var entryId = payroll.Entries.Single().Id;
        var collaboratorId = payroll.Entries.Single().CollaboratorId;

        var savePayload = new
        {
            collaboratorIds = new[] { collaboratorId },
            entries = new[]
            {
                new
                {
                    entryId,
                    commercialProjectEntries = new[]
                    {
                        new
                        {
                            projectId = lastlinkId,
                            platform = "lastlink",
                            ftdTotal = 50,
                            ftdSuperbet = 0,
                            isFtdGoalReached = false,
                            isProjectFtdGoalReached = false,
                            cpaCount = 0,
                            salesAmount = 15_000m,
                            isSalesGoalReached = false,
                            isProjectSalesGoalReached = false,
                            rev = 0m
                        }
                    }
                }
            }
        };

        var client = await CreateAdminClientAsync();
        var saveResponse = await client.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", savePayload);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await saveResponse.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();

        var reopenResponse = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        reopenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reopened = await reopenResponse.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        reopened!.TotalAmount.Should().Be(saved!.TotalAmount);

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/preview",
            new
            {
                commercialProjectEntries = new[]
                {
                    new
                    {
                        projectId = lastlinkId,
                        platform = "lastlink",
                        ftdTotal = 50,
                        ftdSuperbet = 0,
                        isFtdGoalReached = false,
                        isProjectFtdGoalReached = false,
                        cpaCount = 0,
                        salesAmount = 15_000m,
                        isSalesGoalReached = false,
                        isProjectSalesGoalReached = false,
                        rev = 0m
                    }
                }
            });
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await previewResponse.Content.ReadFromJsonAsync<PayrollEntryPreviewApiResponse>();
        preview!.Result.TotalAmount.Should().Be(saved.TotalAmount);
    }

    [Fact]
    public async Task UpdatePayroll_WithInvalidTrafficRateio_ShouldNotPersistChanges()
    {
        var trafficCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.CareerLevels.PaidTrafficSenior,
            "Pedro Tráfego Save",
            "11933332222");

        var payroll = await PayrollTestHelper.CreateDraftPayrollWithCollaboratorIdsAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            month: 10,
            year: 2033,
            trafficCollaboratorId);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var projectId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;
        var entryId = payroll.Entries.Single().Id;
        var collaboratorId = payroll.Entries.Single().CollaboratorId;

        var client = await CreateAdminClientAsync();
        var response = await client.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", new
        {
            collaboratorIds = new[] { collaboratorId },
            entries = new[]
            {
                new
                {
                    entryId,
                    rateioProjectEntries = new[]
                    {
                        new { projectId, rateioValue = 500m }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var reloaded = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
        reloaded.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public async Task SubmitPayroll_FromDraft_ShouldSetPendingApprovalAndSubmittedBy()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 11,
            year: 2033,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var lastlinkId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;
        var entryId = payroll.Entries.Single().Id;
        var collaboratorId = payroll.Entries.Single().CollaboratorId;

        var managerClient = await CreateManagerClientAsync(
            await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
                _factory.Services,
                SeedKeys.Departments.CommercialAnalysts));

        var saveResponse = await managerClient.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", new
        {
            collaboratorIds = new[] { collaboratorId },
            entries = new[]
            {
                new
                {
                    entryId,
                    commercialProjectEntries = new[]
                    {
                        new
                        {
                            projectId = lastlinkId,
                            platform = "lastlink",
                            ftdTotal = 10,
                            ftdSuperbet = 0,
                            isFtdGoalReached = false,
                            isProjectFtdGoalReached = false,
                            cpaCount = 0,
                            salesAmount = 5_000m,
                            isSalesGoalReached = false,
                            isProjectSalesGoalReached = false,
                            rev = 0m
                        }
                    }
                }
            }
        });
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await saveResponse.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();

        var submitResponse = await managerClient.PostAsync($"/api/v1/payrolls/{payroll.Id}/submit", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitted = await submitResponse.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        submitted!.Status.Should().Be("pendingApproval");
        submitted.SubmittedBy.Should().Be("Test Manager");
        submitted.TotalAmount.Should().Be(saved!.TotalAmount);
    }

    [Fact]
    public async Task SubmitPayroll_WhenPendingApproval_ShouldReturnConflict()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 12,
            year: 2033,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
            var entity = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
            entity.Status = Core.Domain.PayrollStatus.PendingApproval;
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateAdminClientAsync();
        var response = await client.PostAsync($"/api/v1/payrolls/{payroll.Id}/submit", null);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SubmitPayroll_ManagerOutsideScope_ShouldReturnForbidden()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var trafficCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.CareerLevels.PaidTrafficSenior,
            "João Tráfego Submit",
            "11955554444");

        var trafficPayroll = await PayrollTestHelper.CreateDraftPayrollWithCollaboratorIdsAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            month: 1,
            year: 2034,
            trafficCollaboratorId);

        var client = await CreateManagerClientAsync(commercialDepartmentId);
        var response = await client.PostAsync($"/api/v1/payrolls/{trafficPayroll.Id}/submit", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RejectedPayroll_Manager_SaveVisibleEntry_ShouldIncludeHiddenApprovedInTotal()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Carlos Comercial Save",
            "11944443333");

        var payroll = await PayrollTestHelper.CreateRejectedPayrollWithApprovedEntryAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            secondCollaboratorId,
            month: 2,
            year: 2034);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var lastlinkId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LastlinkSample).EntityId;

        var reloadedBeforeSave = await dbContext.Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == payroll.Id);
        var visibleEntry = reloadedBeforeSave.Entries.Single(e => e.CollaboratorId == secondCollaboratorId);
        var approvedEntry = reloadedBeforeSave.Entries.Single(e => e.IsApproved);
        approvedEntry.Payload.CommercialProjectEntries =
        [
            new CommercialAnalystProjectEntryInput(
                lastlinkId,
                ProjectPlatform.Lastlink,
                20, 0, false, false, 0,
                8_000m, false, false, 0m)
        ];

        await dbContext.SaveChangesAsync();

        var managerClient = await CreateManagerClientAsync(commercialDepartmentId);
        var response = await managerClient.PutAsJsonAsync($"/api/v1/payrolls/{payroll.Id}", new
        {
            collaboratorIds = new[] { secondCollaboratorId },
            entries = new[]
            {
                new
                {
                    entryId = visibleEntry.Id,
                    commercialProjectEntries = new[]
                    {
                        new
                        {
                            projectId = lastlinkId,
                            platform = "lastlink",
                            ftdTotal = 5,
                            ftdSuperbet = 0,
                            isFtdGoalReached = false,
                            isProjectFtdGoalReached = false,
                            cpaCount = 0,
                            salesAmount = 3_000m,
                            isSalesGoalReached = false,
                            isProjectSalesGoalReached = false,
                            rev = 0m
                        }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        updated!.TotalAmount.Should().BeGreaterThan(0m);

        var reloaded = await dbContext.Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == payroll.Id);
        reloaded.Entries.Should().HaveCount(2);
        reloaded.Entries.Single(e => e.IsApproved).Payload.CommercialProjectEntries.Should().ContainSingle();
    }

    [Fact]
    public async Task DuplicatePayroll_OutsideManagerScope_ShouldReturnForbidden()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var trafficPayroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.Collaborators.PaidTrafficInactive,
            month: 9,
            year: 2026);

        var client = await CreateManagerClientAsync(commercialDepartmentId);
        var response = await client.PostAsync($"/api/v1/payrolls/{trafficPayroll.Id}/duplicate", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPayrollById_FinancialPendingApproval_AllowedActionsExcludeApproveAndPay()
    {
        var payroll = await PayrollTestHelper.CreatePendingApprovalPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 4,
            year: 2028);

        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateFinancialUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.AllowedActions.ApprovePayroll.Should().BeFalse();
        detail.AllowedActions.RejectPayroll.Should().BeFalse();
        detail.AllowedActions.PayPayroll.Should().BeFalse();
        detail.AllowedActions.PayEntry.Should().BeFalse();
        detail.AllowedActions.Edit.Should().BeFalse();
    }

    [Fact]
    public async Task GetPayrollById_DirectorPendingApproval_CanApproveButNotPay()
    {
        var payroll = await PayrollTestHelper.CreatePendingApprovalPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 5,
            year: 2028);

        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.AllowedActions.ApprovePayroll.Should().BeTrue();
        detail.AllowedActions.RejectPayroll.Should().BeTrue();
        detail.AllowedActions.PayPayroll.Should().BeFalse();
        detail.AllowedActions.PayEntry.Should().BeFalse();
        detail.AllowedActions.ToggleNf.Should().BeFalse();
        detail.AllowedActions.Edit.Should().BeFalse();
    }

    [Fact]
    public async Task GetPayrollById_AdminApproved_AllowedActionsIncludePayAndDelete()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 6,
            year: 2028);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
            entity.Status = PayrollStatus.Approved;
            entity.Entries.ToList().ForEach(e => e.IsPaid = false);
            await dbContext.SaveChangesAsync();
        }

        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.AllowedActions.PayPayroll.Should().BeTrue();
        detail.AllowedActions.Delete.Should().BeTrue();
        detail.AllowedActions.ApprovePayroll.Should().BeFalse();
        detail.AllowedActions.PostApprovalAdjustments.Should().BeTrue();
    }

    [Fact]
    public async Task GetPayrollById_ManagerDraft_CanEditButNotApproveOrPay()
    {
        var departmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 7,
            year: 2028,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateManagerClientAsync(departmentId);
        var response = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.AllowedActions.Edit.Should().BeTrue();
        detail.AllowedActions.Recalculate.Should().BeTrue();
        detail.AllowedActions.ApprovePayroll.Should().BeFalse();
        detail.AllowedActions.PayPayroll.Should().BeFalse();
        detail.AllowedActions.Delete.Should().BeFalse();
    }

    [Fact]
    public async Task Manager_WithZeroDepartments_ShouldReturnEmptyPayrollList()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/payrolls");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payrolls = await ReadPayrollsAsync(response);
        payrolls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPayrollById_AsManager_ShouldScopeEditorOptionsToAssignedDepartments()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);

        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 10,
            year: 2029,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateManagerClientAsync(commercialDepartmentId);
        var response = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.EditorOptions.Departments.Should().ContainSingle();
        detail.EditorOptions.Departments[0].Id.Should().Be(commercialDepartmentId);
        detail.EditorOptions.Departments.Should().NotContain(d => d.Id == trafficDepartmentId);
        detail.EditorOptions.CareerLevels.Should().OnlyContain(level =>
            level.DepartmentId == null || level.DepartmentId == commercialDepartmentId);
        detail.EditorOptions.Projects.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdatePayroll_OutsideManagerScope_ShouldReturnForbidden()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var trafficPayroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.Collaborators.PaidTrafficInactive,
            month: 11,
            year: 2029);
        var collaboratorId = trafficPayroll.Entries.Single().CollaboratorId;

        var client = await CreateManagerClientAsync(commercialDepartmentId);
        var response = await client.PutAsJsonAsync($"/api/v1/payrolls/{trafficPayroll.Id}", new
        {
            collaboratorIds = new[] { collaboratorId }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPayrollById_FinancialApproved_CanPayButNotApprove()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 8,
            year: 2028);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
            entity.Status = PayrollStatus.Approved;
            entity.Entries.ToList().ForEach(e => e.IsPaid = false);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateFinancialUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.AllowedActions.ApprovePayroll.Should().BeFalse();
        detail.AllowedActions.RejectPayroll.Should().BeFalse();
        detail.AllowedActions.ApproveEntry.Should().BeFalse();
        detail.AllowedActions.PayPayroll.Should().BeTrue();
        detail.AllowedActions.PayEntry.Should().BeTrue();
        detail.AllowedActions.ToggleNf.Should().BeTrue();
    }

    [Fact]
    public async Task GetPayrollById_DirectorApproved_CanApproveEntryButNotPay()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 9,
            year: 2028);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await dbContext.Payrolls.SingleAsync(p => p.Id == payroll.Id);
            entity.Status = PayrollStatus.Approved;
            entity.Entries.ToList().ForEach(e => e.IsPaid = false);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.AllowedActions.ApproveEntry.Should().BeTrue();
        detail.AllowedActions.PayPayroll.Should().BeFalse();
        detail.AllowedActions.PayEntry.Should().BeFalse();
        detail.AllowedActions.ToggleNf.Should().BeFalse();
    }

    private async Task<Guid> CreateEmptyDraftPayrollAsync(int month, int year)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var departmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var payroll = new Core.Domain.Payroll
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            Month = month,
            Year = year,
            Status = Core.Domain.PayrollStatus.Draft,
            TotalAmount = 0m
        };

        dbContext.Payrolls.Add(payroll);
        await dbContext.SaveChangesAsync();
        return payroll.Id;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<HttpClient> CreateManagerClientAsync(Guid departmentId)
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory, departmentId);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<Guid> GetPayrollIdByDepartmentAndCompetenceAsync(
        Guid departmentId,
        int month,
        int year)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Payrolls
            .Where(p => p.DepartmentId == departmentId && p.Month == month && p.Year == year)
            .Select(p => p.Id)
            .FirstAsync();
    }

    private static async Task<IReadOnlyList<PayrollListApiResponse>> ReadPayrollsAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<PayrollsListApiResponse>();
        payload.Should().NotBeNull();
        return payload!.Items;
    }

    private sealed record PayrollsListApiResponse(
        IReadOnlyList<PayrollListApiResponse> Items,
        int TotalCount,
        int Page,
        int PageSize);

    private sealed record PayrollListApiResponse(
        Guid Id,
        Guid DepartmentId,
        string DepartmentName,
        int Month,
        int Year,
        string Status,
        decimal TotalAmount,
        int EntryCount,
        string? SubmittedBy);

    private sealed record PayrollSummaryApiResponse(
        Guid Id,
        Guid DepartmentId,
        string DepartmentName,
        int Month,
        int Year,
        string Status,
        decimal TotalAmount,
        int EntryCount,
        string? SubmittedBy);

    private sealed record PayrollFormOptionsApiResponse(
        IReadOnlyList<PayrollFormDepartmentOptionApiResponse> Departments);

    private sealed record PayrollFormDepartmentOptionApiResponse(Guid Id, string Name);

    private sealed record PayrollDetailApiResponse(
        Guid Id,
        Guid DepartmentId,
        string DepartmentName,
        int Month,
        int Year,
        string Status,
        decimal TotalAmount,
        int EntryCount,
        string? SubmittedBy,
        string? RejectionComment,
        IReadOnlyList<PayrollEntryEditorApiResponse> Entries,
        PayrollEditorOptionsApiResponse EditorOptions,
        PayrollAllowedActionsApiResponse AllowedActions);

    private sealed record PayrollAllowedActionsApiResponse(
        bool ApprovePayroll,
        bool RejectPayroll,
        bool ApproveEntry,
        bool Edit,
        bool PayPayroll,
        bool PayEntry,
        bool ToggleNf,
        bool PostApprovalAdjustments,
        bool Delete,
        bool Recalculate);

    private sealed record PayrollEditorOptionsApiResponse(
        IReadOnlyList<PayrollEditorProjectOptionApiResponse> Projects,
        IReadOnlyList<PayrollEditorDepartmentOptionApiResponse> Departments,
        IReadOnlyList<PayrollEditorCareerLevelOptionApiResponse> CareerLevels);

    private sealed record PayrollEditorProjectOptionApiResponse(
        Guid Id,
        string Name,
        string Platform,
        bool ExcludesGoalBonus,
        bool IsDefaultAllocationTarget,
        bool ExcludesSupervisorFixedAllocation);

    private sealed record PayrollEditorDepartmentOptionApiResponse(Guid Id, string Name);

    private sealed record PayrollEditorCareerLevelOptionApiResponse(
        Guid Id,
        string Name,
        Guid? DepartmentId,
        string Profile);

    private sealed record PayrollEntryEditorApiResponse(
        Guid Id,
        Guid CollaboratorId,
        string CollaboratorName,
        string? CareerLevelName,
        string? PixKey,
        DateOnly? AdmissionDate,
        decimal? FullBaseSalary,
        string CalculationProfile,
        bool IsApproved,
        bool IsPaid,
        bool NfSent,
        string GoalTier,
        decimal? FinalSalary,
        int BetanoInternaCount,
        int BetanoMundoBetCount,
        decimal SupervisorAnalystRevenue,
        Guid? CommissionPayingProjectId,
        PayrollEntryPayloadApiResponse Payload,
        PayrollEntryResultApiResponse? Result = null,
        IReadOnlyList<PayrollProjectTotalApiResponse>? ProjectTotals = null);

    private sealed record PayrollProjectTotalApiResponse(Guid ProjectId, decimal Amount);

    private sealed record PayrollEntryPayloadApiResponse(
        IReadOnlyList<object> ProjectEntries,
        IReadOnlyList<object> RateioProjectEntries,
        IReadOnlyList<object> CommercialProjectEntries,
        IReadOnlyList<object> SupervisorProjectEntries,
        IReadOnlyList<object> TrafficProjectEntries,
        IReadOnlyList<object> ManagementRevenueEntries,
        IReadOnlyList<object> BonusEntries,
        IReadOnlyList<object> DeductionEntries,
        IReadOnlyList<object> ComplementPayingProjects,
        IReadOnlyList<object> RoleChanges);

    private sealed record PayrollEntryPreviewApiResponse(
        PayrollEntryResultApiResponse Result,
        IReadOnlyList<PayrollProjectTotalApiResponse> ProjectTotals);

    private sealed record PayrollEntryResultApiResponse(
        decimal TotalAmount,
        decimal BaseSalary,
        decimal CommissionAmount,
        decimal GoalBonusAmount,
        decimal GroupCommissionAmount,
        decimal PlatformTotal);

    private sealed record ApiErrorResponse(string? Error, string? Message);
}

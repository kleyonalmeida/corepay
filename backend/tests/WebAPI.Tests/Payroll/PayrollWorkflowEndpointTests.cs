using System.Net;
using System.Net.Http.Json;
using Core.Domain;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Payroll;

[Collection("WebApiIntegration")]
public class PayrollWorkflowEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public PayrollWorkflowEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Workflow_DraftToPaidAndBackToApproved_ShouldFollowStateDiagram()
    {
        var payroll = await CreateSubmittedPendingPayrollAsync(month: 1, year: 2040);
        var entryId = payroll.Entries.Single().Id;

        var directorClient = await CreateDirectorClientAsync();
        var approveResponse = await directorClient.PostAsync($"/api/v1/payrolls/{payroll.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approveResponse.Content.ReadFromJsonAsync<PayrollWorkflowDetailResponse>();
        approved!.Status.Should().Be("approved");
        approved.ApprovedBy.Should().NotBeNullOrWhiteSpace();
        approved.ApprovedAt.Should().NotBeNull();
        approved.Entries.Single().IsApproved.Should().BeTrue();

        var adminClient = await CreateAdminClientAsync();
        var payPayrollResponse = await adminClient.PostAsync($"/api/v1/payrolls/{payroll.Id}/pay", null);
        payPayrollResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await payPayrollResponse.Content.ReadFromJsonAsync<PayrollWorkflowDetailResponse>())!
            .Status.Should().Be("paid");

        var unpayResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/pay",
            new { isPaid = false });
        unpayResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await unpayResponse.Content.ReadFromJsonAsync<PayrollWorkflowDetailResponse>())!
            .Status.Should().Be("approved");
    }

    [Fact]
    public async Task ApprovePayroll_ShouldPersistSnapshot_IgnoringLaterCareerLevelChanges()
    {
        var payroll = await CreateSubmittedPendingPayrollAsync(month: 2, year: 2040);

        var directorClient = await CreateDirectorClientAsync();
        var approveResponse = await directorClient.PostAsync($"/api/v1/payrolls/{payroll.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approved = await approveResponse.Content.ReadFromJsonAsync<PayrollWorkflowDetailResponse>();
        var lockedTotal = approved!.TotalAmount;
        lockedTotal.Should().BeGreaterThan(0m);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var levelId = seedEntities.Single(s => s.Key == SeedKeys.CareerLevels.CommercialAnalystJunior).EntityId;
        var level = await dbContext.CareerLevels.SingleAsync(c => c.Id == levelId);
        level.FtdRateBase = 999m;
        level.SalesPctBase = 999m;
        await dbContext.SaveChangesAsync();

        var adminClient = await CreateAdminClientAsync();
        var getResponse = await adminClient.GetAsync($"/api/v1/payrolls/{payroll.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await getResponse.Content.ReadFromJsonAsync<PayrollWorkflowDetailResponse>();
        detail!.TotalAmount.Should().Be(lockedTotal);
        detail.Entries.Single().Result!.TotalAmount.Should().Be(lockedTotal);
    }

    [Fact]
    public async Task RejectPayroll_WithoutComment_ShouldReturnBadRequest()
    {
        var payroll = await PayrollTestHelper.CreatePendingApprovalPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 3,
            year: 2040);

        var directorClient = await CreateDirectorClientAsync();
        var response = await directorClient.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/reject",
            new { rejectionComment = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RejectPayroll_WithComment_ShouldPersistRejection()
    {
        var payroll = await PayrollTestHelper.CreatePendingApprovalPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 4,
            year: 2040);

        var directorClient = await CreateDirectorClientAsync();
        var response = await directorClient.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/reject",
            new { rejectionComment = "Ajustar FTD" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rejected = await response.Content.ReadFromJsonAsync<PayrollWorkflowDetailResponse>();
        rejected!.Status.Should().Be("rejected");
        rejected.RejectionComment.Should().Be("Ajustar FTD");
        rejected.ApprovedBy.Should().BeNull();
    }

    [Fact]
    public async Task FinancialUser_CannotApprovePendingPayroll()
    {
        var payroll = await PayrollTestHelper.CreatePendingApprovalPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 5,
            year: 2040);

        var client = await CreateFinancialClientAsync();
        var response = await client.PostAsync($"/api/v1/payrolls/{payroll.Id}/approve", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FinancialUser_CannotRejectPendingPayroll()
    {
        var payroll = await PayrollTestHelper.CreatePendingApprovalPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 9,
            year: 2040);

        var client = await CreateFinancialClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/reject",
            new { rejectionComment = "Não deveria reprovar" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FinancialUser_CannotApproveEntry()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 10,
            year: 2040,
            SeedKeys.Collaborators.CommercialAnalystActive);
        var entryId = payroll.Entries.Single().Id;

        var client = await CreateFinancialClientAsync();
        var response = await client.PostAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/approve",
            null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DirectorUser_CannotPayPendingApprovalPayroll()
    {
        var payroll = await PayrollTestHelper.CreatePendingApprovalPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 6,
            year: 2040);

        var client = await CreateDirectorClientAsync();
        var response = await client.PostAsync($"/api/v1/payrolls/{payroll.Id}/pay", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DirectorUser_CannotPayApprovedPayroll()
    {
        var payroll = await CreateApprovedPayrollAsync(month: 11, year: 2040);

        var client = await CreateDirectorClientAsync();
        var response = await client.PostAsync($"/api/v1/payrolls/{payroll.Id}/pay", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DirectorUser_CannotPayEntryOnApprovedPayroll()
    {
        var payroll = await CreateApprovedPayrollAsync(month: 12, year: 2040);
        var entryId = payroll.Entries.Single().Id;

        var client = await CreateDirectorClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/pay",
            new { isPaid = true });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DirectorUser_CannotToggleNfOnApprovedPayroll()
    {
        var payroll = await CreateApprovedPayrollAsync(month: 1, year: 2041);
        var entryId = payroll.Entries.Single().Id;

        var client = await CreateDirectorClientAsync();
        var response = await client.PutAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries/{entryId}/nf",
            new { nfSent = true });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeletePayroll_AsAdmin_ShouldRemovePayroll()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 7,
            year: 2040,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateAdminClientAsync();
        var response = await client.DeleteAsync($"/api/v1/payrolls/{payroll.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.Payrolls.AnyAsync(p => p.Id == payroll.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task RecalculatePayroll_WhenApproved_ShouldReturnConflict()
    {
        var payroll = await CreateSubmittedPendingPayrollAsync(month: 8, year: 2040);
        var directorClient = await CreateDirectorClientAsync();
        await directorClient.PostAsync($"/api/v1/payrolls/{payroll.Id}/approve", null);

        var adminClient = await CreateAdminClientAsync();
        var response = await adminClient.PostAsync($"/api/v1/payrolls/{payroll.Id}/recalculate", null);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<Core.Domain.Payroll> CreateSubmittedPendingPayrollAsync(int month, int year)
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month,
            year,
            SeedKeys.Collaborators.CommercialAnalystActive);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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

        var submitResponse = await managerClient.PostAsync($"/api/v1/payrolls/{payroll.Id}/submit", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return await dbContext.Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == payroll.Id);
    }

    private async Task<Core.Domain.Payroll> CreateApprovedPayrollAsync(int month, int year)
    {
        var payroll = await CreateSubmittedPendingPayrollAsync(month, year);
        var directorClient = await CreateDirectorClientAsync();
        var approveResponse = await directorClient.PostAsync($"/api/v1/payrolls/{payroll.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Payrolls
            .Include(p => p.Entries)
            .SingleAsync(p => p.Id == payroll.Id);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<HttpClient> CreateDirectorClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<HttpClient> CreateFinancialClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateFinancialUserAsync(_factory);
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

    private sealed record PayrollWorkflowDetailResponse(
        Guid Id,
        string Status,
        decimal TotalAmount,
        string? RejectionComment,
        string? ApprovedBy,
        DateTimeOffset? ApprovedAt,
        IReadOnlyList<PayrollWorkflowEntryResponse> Entries);

    private sealed record PayrollWorkflowEntryResponse(
        Guid Id,
        bool IsApproved,
        bool IsPaid,
        PayrollWorkflowEntryResultResponse? Result);

    private sealed record PayrollWorkflowEntryResultResponse(decimal TotalAmount);
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Payroll;

[Collection("WebApiIntegration")]
public class AddCollaboratorEntryEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public AddCollaboratorEntryEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddCollaboratorEntry_DraftPayroll_ShouldSetIsApprovedAndAppearInFinanceSummary()
    {
        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Bruno Avulso",
            "11988887777");

        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 7,
            year: 2030,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateFinancialClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries",
            new { collaboratorId = secondCollaboratorId });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail.Should().NotBeNull();
        detail!.Entries.Should().ContainSingle(e => e.CollaboratorId == secondCollaboratorId && e.IsApproved);

        var summaryResponse = await client.GetAsync("/api/v1/finance/summary?month=7&year=2030");
        var summary = await summaryResponse.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>();
        summary!.Payrolls.Single(p => p.Id == payroll.Id).Entries.Should().ContainSingle();
        summary.Payrolls.Single(p => p.Id == payroll.Id).Entries[0].CollaboratorName.Should().Be("Bruno Avulso");
        summary.Payrolls.Single(p => p.Id == payroll.Id).Entries[0].IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task AddCollaboratorEntry_PendingApproval_ShouldReturnConflict()
    {
        var payroll = await PayrollTestHelper.CreatePendingApprovalPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 8,
            year: 2030);

        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Carla Avulso",
            "11977776666");

        var client = await CreateFinancialClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries",
            new { collaboratorId = secondCollaboratorId });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddCollaboratorEntry_AsDirector_ShouldReturnForbidden()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 9,
            year: 2030,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Diego Avulso",
            "11966665555");

        var client = await CreateDirectorClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries",
            new { collaboratorId = secondCollaboratorId });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddCollaboratorEntry_DuplicateCollaborator_ShouldReturnConflict()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 10,
            year: 2030,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var collaboratorId = await GetCollaboratorIdAsync(SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateFinancialClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries",
            new { collaboratorId });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddCollaboratorEntry_PaidPayroll_ShouldRevertStatusToApproved()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 11,
            year: 2030,
            totalAmount: 5000m);

        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Edu Avulso",
            "11955554444");

        var client = await CreateFinancialClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries",
            new { collaboratorId = secondCollaboratorId });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<PayrollDetailApiResponse>();
        detail!.Status.Should().Be("approved");
        detail.Entries.Should().HaveCount(2);
        detail.Entries.Should().Contain(e => e.CollaboratorId == secondCollaboratorId && e.IsApproved);
    }

    [Fact]
    public async Task AddCollaboratorEntry_PaidPayroll_ShouldPreserveExistingEntrySnapshot()
    {
        var payroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.Collaborators.CommercialAnalystActive,
            month: 12,
            year: 2030,
            totalAmount: 4500m);

        Guid existingEntryId;
        decimal existingTotal;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existingEntry = await dbContext.PayrollCollaboratorEntries
                .SingleAsync(e => e.PayrollId == payroll.Id);
            existingEntryId = existingEntry.Id;
            existingTotal = existingEntry.Payload.CalculatedResult!.TotalAmount;
        }

        var secondCollaboratorId = await PayrollTestHelper.CreateActiveCollaboratorAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            SeedKeys.CareerLevels.CommercialAnalystJunior,
            "Fernanda Avulso",
            "11944443333");

        var client = await CreateFinancialClientAsync();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/payrolls/{payroll.Id}/entries",
            new { collaboratorId = secondCollaboratorId });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existingEntry = await dbContext.PayrollCollaboratorEntries
                .SingleAsync(e => e.Id == existingEntryId);
            existingEntry.Payload.CalculatedResult!.TotalAmount.Should().Be(existingTotal);
        }
    }

    [Fact]
    public async Task AddCollaboratorEntry_FinanceSummary_ShouldExposeAddCollaboratorAction()
    {
        var payroll = await PayrollTestHelper.CreateDraftPayrollAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts,
            month: 1,
            year: 2031,
            SeedKeys.Collaborators.CommercialAnalystActive);

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
        var response = await client.GetAsync("/api/v1/finance/summary?month=1&year=2031");
        var summary = await response.Content.ReadFromJsonAsync<FinanceSummaryApiResponse>();
        summary!.Payrolls.Single(p => p.Id == payroll.Id).AllowedActions.AddCollaborator.Should().BeTrue();
    }

    private async Task<HttpClient> CreateFinancialClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateFinancialUserAsync(_factory);
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

    private async Task<Guid> GetCollaboratorIdAsync(string seedKey)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.SeedEntities
            .Where(s => s.Key == seedKey)
            .Select(s => s.EntityId)
            .SingleAsync();
    }

    private sealed record PayrollDetailApiResponse(
        Guid Id,
        string Status,
        IReadOnlyList<PayrollEntryApiResponse> Entries);

    private sealed record PayrollEntryApiResponse(
        Guid Id,
        Guid CollaboratorId,
        bool IsApproved);

    private sealed record FinanceSummaryApiResponse(
        IReadOnlyList<FinancePayrollGroupApiResponse> Payrolls);

    private sealed record FinancePayrollGroupApiResponse(
        Guid Id,
        FinanceAllowedActionsApiResponse AllowedActions,
        IReadOnlyList<FinanceEntryApiResponse> Entries);

    private sealed record FinanceAllowedActionsApiResponse(
        bool AddCollaborator);

    private sealed record FinanceEntryApiResponse(
        string CollaboratorName,
        bool IsApproved);
}

using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Time;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Dashboard;

[Collection("WebApiIntegration")]
public sealed class DashboardEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public DashboardEndpointTests(CorePayWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetDashboard_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_UserWithoutPermissions_ShouldReturnNoProtectedBlocks()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateUserWithoutPermissionsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/dashboard?month=1&year=2071");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardApiResponse>();
        dashboard!.PayrollStats.Should().BeNull();
        dashboard.ActiveCollaborators.Should().BeNull();
        dashboard.RecentPayrolls.Should().BeNull();
    }

    [Fact]
    public async Task GetDashboard_ShouldIgnoreDraftAndAggregateLockedNetAmounts()
    {
        var departments = await GetDepartmentIdsAsync(4);
        await AddPayrollAsync(departments[0], 2, 2072, PayrollStatus.Draft);
        await AddPayrollAsync(departments[1], 2, 2072, PayrollStatus.PendingApproval);
        await AddPayrollAsync(departments[2], 2, 2072, PayrollStatus.Rejected);
        await AddPayrollAsync(
            departments[3],
            2,
            2072,
            PayrollStatus.Approved,
            (Total: 5_000m, Platform: 800m, IsPaid: false),
            (Total: 3_000m, Platform: 200m, IsPaid: true));

        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync("/api/v1/dashboard?month=2&year=2072");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardApiResponse>();
        dashboard!.PayrollStats.Should().BeEquivalentTo(new DashboardStatsApiResponse(
            TotalPayrolls: 3,
            AwaitingApproval: 1,
            Approved: 1,
            Rejected: 1,
            TotalToPay: 4_200m,
            TotalPaid: 2_800m));
    }

    [Fact]
    public async Task GetDashboard_Manager_ShouldOnlySeeAssignedDepartments()
    {
        var departments = await GetDepartmentIdsAsync(2);
        await AddPayrollAsync(
            departments[0],
            3,
            2073,
            PayrollStatus.Approved,
            (Total: 1_500m, Platform: 100m, IsPaid: false));
        var foreignPayrollId = await AddPayrollAsync(
            departments[1],
            3,
            2073,
            PayrollStatus.Paid,
            (Total: 9_000m, Platform: 0m, IsPaid: true));

        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory, departments[0]);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/dashboard?month=3&year=2073");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardApiResponse>();
        dashboard!.PayrollStats!.TotalPayrolls.Should().Be(1);
        dashboard.PayrollStats.TotalToPay.Should().Be(1_400m);
        dashboard.PayrollStats.TotalPaid.Should().Be(0m);
        dashboard.RecentPayrolls.Should().NotContain(p => p.Id == foreignPayrollId);

        using var scope = _factory.Services.CreateScope();
        var expectedActive = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Collaborators
            .CountAsync(c => c.IsActive && c.DepartmentId == departments[0]);
        dashboard.ActiveCollaborators.Should().Be(expectedActive);
    }

    [Fact]
    public async Task GetDashboard_ManagerWithZeroDepartments_ShouldReturnZeroStats()
    {
        var departments = await GetDepartmentIdsAsync(1);
        await AddPayrollAsync(
            departments[0],
            4,
            2077,
            PayrollStatus.Approved,
            (Total: 2_000m, Platform: 0m, IsPaid: false));

        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/dashboard?month=4&year=2077");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dashboard = await response.Content.ReadFromJsonAsync<DashboardApiResponse>();
        dashboard!.PayrollStats!.TotalPayrolls.Should().Be(0);
        dashboard.PayrollStats.TotalToPay.Should().Be(0m);
        dashboard.PayrollStats.TotalPaid.Should().Be(0m);
        dashboard.ActiveCollaborators.Should().Be(0);
        dashboard.RecentPayrolls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboard_WithoutFilters_ShouldUsePreviousBahiaCompetenceAcrossYearBoundary()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(
                    new FixedTimeProvider(new DateTimeOffset(2027, 1, 1, 2, 30, 0, TimeSpan.Zero)));
            }));
        var client = factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardApiResponse>();
        dashboard!.Month.Should().Be(11);
        dashboard.Year.Should().Be(2026);
    }

    [Fact]
    public async Task GetDashboard_RecentPayrolls_ShouldBeGlobalOrderedAndLimitedToEight()
    {
        var departmentId = (await GetDepartmentIdsAsync(1))[0];
        for (var month = 1; month <= 9; month++)
        {
            await AddPayrollAsync(departmentId, month, 2074, PayrollStatus.Draft);
        }

        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync("/api/v1/dashboard?month=1&year=2071");
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardApiResponse>();

        dashboard!.RecentPayrolls.Should().HaveCount(8);
        dashboard.RecentPayrolls!.Select(p => p.Month).Should().ContainInOrder(9, 8, 7, 6, 5, 4, 3, 2);
    }

    [Theory]
    [InlineData(0, 2026)]
    [InlineData(13, 2026)]
    [InlineData(1, 1999)]
    public async Task GetDashboard_InvalidCompetence_ShouldReturnBadRequest(int month, int year)
    {
        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/v1/dashboard?month={month}&year={year}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<IReadOnlyList<Guid>> GetDepartmentIdsAsync(int count)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Departments
            .AsNoTracking()
            .OrderBy(d => d.Id)
            .Select(d => d.Id)
            .Take(count)
            .ToListAsync();
    }

    private async Task<Guid> AddPayrollAsync(
        Guid departmentId,
        int month,
        int year,
        PayrollStatus status,
        params (decimal Total, decimal Platform, bool IsPaid)[] entries)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payrollId = Guid.NewGuid();
        var payroll = new Core.Domain.Payroll
        {
            Id = payrollId,
            DepartmentId = departmentId,
            Month = month,
            Year = year,
            Status = status,
            TotalAmount = entries.Sum(e => e.Total)
        };

        foreach (var entry in entries)
        {
            payroll.Entries.Add(new PayrollCollaboratorEntry
            {
                Id = Guid.NewGuid(),
                PayrollId = payrollId,
                CollaboratorId = Guid.NewGuid(),
                CollaboratorName = $"Dashboard {Guid.NewGuid():N}",
                DepartmentId = departmentId,
                IsApproved = true,
                IsPaid = entry.IsPaid,
                Payload = new PayrollCollaboratorEntryPayload
                {
                    CalculatedResult = new PayrollEntryResult
                    {
                        TotalAmount = entry.Total,
                        PlatformTotal = entry.Platform
                    }
                }
            });
        }

        dbContext.Payrolls.Add(payroll);
        await dbContext.SaveChangesAsync();
        return payrollId;
    }

    private sealed record DashboardApiResponse(
        int Month,
        int Year,
        DashboardStatsApiResponse? PayrollStats,
        int? ActiveCollaborators,
        IReadOnlyList<RecentPayrollApiResponse>? RecentPayrolls);

    private sealed record DashboardStatsApiResponse(
        int TotalPayrolls,
        int AwaitingApproval,
        int Approved,
        int Rejected,
        decimal TotalToPay,
        decimal TotalPaid);

    private sealed record RecentPayrollApiResponse(
        Guid Id,
        Guid DepartmentId,
        string DepartmentName,
        int Month,
        int Year,
        string Status,
        decimal TotalAmount,
        int EntryCount);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

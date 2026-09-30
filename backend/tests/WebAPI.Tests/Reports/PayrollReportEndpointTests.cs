using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Time;
using Core.Auth;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Reports;

[Collection("WebApiIntegration")]
public sealed class PayrollReportEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public PayrollReportEndpointTests(CorePayWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetPayrollReport_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/reports/payroll?year=2080");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPayrollReport_ManagerWithoutReportsPermission_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/reports/payroll?year=2080");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPayrollReport_FinancialUser_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateFinancialUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/reports/payroll?year=2080");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPayrollReport_WithoutYear_ShouldUseCurrentBahiaYear()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(
                    new FixedTimeProvider(new DateTimeOffset(2028, 3, 15, 12, 0, 0, TimeSpan.Zero)));
            }));

        var client = factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateFinancialUserAsync(factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/reports/payroll");
        var report = await response.Content.ReadFromJsonAsync<PayrollReportApiResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        report!.Year.Should().Be(2028);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task GetPayrollReport_InvalidYear_ShouldReturnBadRequest(int year)
    {
        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync($"/api/v1/reports/payroll?year={year}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPayrollReport_ShouldAggregateLockedSnapshotsAndMonthlyAverage()
    {
        var departments = await GetDepartmentIdsAsync(2);
        var projectId = await GetProjectIdAsync();
        await AddPayrollAsync(
            departments[0],
            1,
            2081,
            PayrollStatus.Approved,
            projectId,
            (Total: 12_000m, [(projectId, 12_000m)]));
        await AddPayrollAsync(
            departments[1],
            2,
            2081,
            PayrollStatus.Paid,
            projectId,
            (Total: 6_000m, [(projectId, 6_000m)]));

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/reports/payroll?year=2081");
        var report = await response.Content.ReadFromJsonAsync<PayrollReportApiResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        report!.Summary.TotalYear.Should().Be(18_000m);
        report.Summary.MonthlyAverage.Should().Be(1_500m);
        report.Summary.CollaboratorCount.Should().Be(2);
        report.ByDepartment.Should().HaveCount(2);
        report.ByProject.Should().ContainSingle(row => row.ProjectId == projectId && row.Amount == 18_000m);
        report.MonthlySeries.Single(point => point.Month == 1).Amount.Should().Be(12_000m);
        report.MonthlySeries.Single(point => point.Month == 2).Amount.Should().Be(6_000m);
    }

    [Fact]
    public async Task GetPayrollReport_ShouldExcludeDraftWithoutApprovedEntries()
    {
        var departmentId = (await GetDepartmentIdsAsync(1))[0];
        var projectId = await GetProjectIdAsync();
        await AddPayrollAsync(
            departmentId,
            3,
            2082,
            PayrollStatus.Draft,
            projectId,
            (Total: 9_000m, [(projectId, 9_000m)]),
            isApproved: false);
        await AddPayrollAsync(
            departmentId,
            4,
            2082,
            PayrollStatus.Paid,
            projectId,
            (Total: 4_000m, [(projectId, 4_000m)]));

        var client = await CreateFinancialClientAsync();
        var report = await (await client.GetAsync("/api/v1/reports/payroll?year=2082"))
            .Content.ReadFromJsonAsync<PayrollReportApiResponse>();

        report!.Summary.TotalYear.Should().Be(4_000m);
        report.ByDepartment.Should().ContainSingle();
    }

    [Fact]
    public async Task GetPayrollReport_PendingApproval_ShouldIncludeOnlyApprovedEntries()
    {
        var departmentId = (await GetDepartmentIdsAsync(1))[0];
        var projectId = await GetProjectIdAsync();
        await AddPayrollAsync(
            departmentId,
            5,
            2083,
            PayrollStatus.PendingApproval,
            projectId,
            (Total: 2_000m, [(projectId, 2_000m)], IsApproved: true),
            (Total: 7_000m, [(projectId, 7_000m)], IsApproved: false));

        var client = await CreateFinancialClientAsync();
        var report = await (await client.GetAsync("/api/v1/reports/payroll?year=2083"))
            .Content.ReadFromJsonAsync<PayrollReportApiResponse>();

        report!.Summary.TotalYear.Should().Be(2_000m);
        report.Summary.CollaboratorCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPayrollReport_ShouldKeepLockedSnapshotAfterCareerLevelChanges()
    {
        var departmentId = (await GetDepartmentIdsAsync(1))[0];
        var projectId = await GetProjectIdAsync();
        Guid levelId;
        await AddPayrollAsync(
            departmentId,
            6,
            2084,
            PayrollStatus.Paid,
            projectId,
            (Total: 5_500m, [(projectId, 5_500m)]));

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            levelId = await dbContext.CareerLevels
                .AsNoTracking()
                .Select(level => level.Id)
                .FirstAsync();
        }

        var client = await CreateFinancialClientAsync();
        var first = await (await client.GetAsync("/api/v1/reports/payroll?year=2084"))
            .Content.ReadFromJsonAsync<PayrollReportApiResponse>();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var level = await dbContext.CareerLevels.SingleAsync(l => l.Id == levelId);
            level.BaseSalary = 99_999m;
            await dbContext.SaveChangesAsync();
        }

        var second = await (await client.GetAsync("/api/v1/reports/payroll?year=2084"))
            .Content.ReadFromJsonAsync<PayrollReportApiResponse>();

        first!.Summary.TotalYear.Should().Be(5_500m);
        second!.Summary.TotalYear.Should().Be(5_500m);
    }

    [Fact]
    public async Task GetPayrollReport_ProjectFilter_ShouldScopeAllMetrics()
    {
        var departmentId = (await GetDepartmentIdsAsync(1))[0];
        var projects = await GetProjectIdsAsync(2);
        await AddPayrollAsync(
            departmentId,
            7,
            2085,
            PayrollStatus.Approved,
            projects[0],
            (Total: 10_000m, [(projects[0], 4_000m), (projects[1], 6_000m)]));

        var client = await CreateFinancialClientAsync();
        var report = await (await client.GetAsync($"/api/v1/reports/payroll?year=2085&projectId={projects[0]}"))
            .Content.ReadFromJsonAsync<PayrollReportApiResponse>();

        report!.Summary.TotalYear.Should().Be(4_000m);
        report.Summary.TopProjectAmount.Should().Be(4_000m);
        report.ByProject.Should().ContainSingle(row => row.ProjectId == projects[0] && row.Amount == 4_000m);
    }

    [Fact]
    public async Task GetPayrollReport_ManagerWithReportsPermission_ShouldRespectDepartmentScope()
    {
        var departments = await GetDepartmentIdsAsync(2);
        var projectId = await GetProjectIdAsync();
        await AddPayrollAsync(
            departments[0],
            8,
            2086,
            PayrollStatus.Paid,
            projectId,
            (Total: 3_000m, [(projectId, 3_000m)]));
        await AddPayrollAsync(
            departments[1],
            8,
            2086,
            PayrollStatus.Paid,
            projectId,
            (Total: 8_000m, [(projectId, 8_000m)]));

        var client = _factory.CreateClient();
        var (_, _, token) = await CreateManagerWithReportsReadAsync(departments[0]);
        AuthTestHelper.SetBearerToken(client, token);

        var allowed = await client.GetAsync("/api/v1/reports/payroll?year=2086");
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await allowed.Content.ReadFromJsonAsync<PayrollReportApiResponse>();
        report!.Summary.TotalYear.Should().Be(3_000m);

        var forbidden = await client.GetAsync(
            $"/api/v1/reports/payroll?year=2086&departmentId={departments[1]}");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPayrollReport_ManagerWithReportsPermission_ShouldScopeFilterOptions()
    {
        var departments = await GetDepartmentIdsAsync(2);
        var projectId = await GetProjectIdAsync();
        await AddPayrollAsync(
            departments[0],
            9,
            2087,
            PayrollStatus.Paid,
            projectId,
            (Total: 3_000m, [(projectId, 3_000m)]));
        await AddPayrollAsync(
            departments[1],
            9,
            2087,
            PayrollStatus.Paid,
            projectId,
            (Total: 8_000m, [(projectId, 8_000m)]));

        var client = _factory.CreateClient();
        var (_, _, token) = await CreateManagerWithReportsReadAsync(departments[0]);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/reports/payroll?year=2087");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<PayrollReportApiResponse>();
        report!.FilterOptions.Departments.Should().ContainSingle();
        report.FilterOptions.Departments[0].Id.Should().Be(departments[0]);
        report.FilterOptions.Departments.Should().NotContain(option => option.Id == departments[1]);
        report.FilterOptions.Projects.Should().NotBeEmpty();
        report.Summary.TotalYear.Should().Be(3_000m);
    }

    private async Task<HttpClient> CreateFinancialClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateFinancialUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<(string Email, string Password, string AccessToken)> CreateManagerWithReportsReadAsync(
        Guid departmentId)
    {
        var email = $"reports-manager-{Guid.NewGuid():N}@corepay.test";
        const string password = "TestPassword123!";

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            DisplayName = "Reports Manager",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        createResult.Succeeded.Should().BeTrue();

        const string customRoleName = "ReportsScopedManager";
        if (!await roleManager.RoleExistsAsync(customRoleName))
        {
            await roleManager.CreateAsync(new IdentityRole(customRoleName));
        }

        var permissionId = await dbContext.Permissions
            .Where(permission => permission.Key == AppPermissions.ReportsRead)
            .Select(permission => permission.Id)
            .SingleAsync();

        var customRoleId = await dbContext.Roles
            .Where(role => role.Name == customRoleName)
            .Select(role => role.Id)
            .SingleAsync();

        if (!await dbContext.RolePermissions.AnyAsync(rp =>
                rp.RoleId == customRoleId && rp.PermissionId == permissionId))
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = customRoleId,
                PermissionId = permissionId
            });
            await dbContext.SaveChangesAsync();
        }

        await userManager.AddToRoleAsync(user, AppRoles.Manager);
        await userManager.AddToRoleAsync(user, customRoleName);
        dbContext.UserDepartments.Add(new UserDepartment
        {
            UserId = user.Id,
            DepartmentId = departmentId
        });
        await dbContext.SaveChangesAsync();

        var client = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAsync(client, email, password);
        return (email, password, token);
    }

    private async Task<IReadOnlyList<Guid>> GetDepartmentIdsAsync(int count)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Departments
            .AsNoTracking()
            .OrderBy(department => department.Id)
            .Select(department => department.Id)
            .Take(count)
            .ToListAsync();
    }

    private async Task<Guid> GetProjectIdAsync()
    {
        var projects = await GetProjectIdsAsync(1);
        return projects[0];
    }

    private async Task<IReadOnlyList<Guid>> GetProjectIdsAsync(int count)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Projects
            .AsNoTracking()
            .OrderBy(project => project.Id)
            .Select(project => project.Id)
            .Take(count)
            .ToListAsync();
    }

    private async Task<Guid> AddPayrollAsync(
        Guid departmentId,
        int month,
        int year,
        PayrollStatus status,
        Guid projectId,
        params (decimal Total, (Guid ProjectId, decimal Amount)[] ProjectTotals, bool IsApproved)[] entries)
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
            TotalAmount = entries.Sum(entry => entry.Total)
        };

        foreach (var entry in entries)
        {
            payroll.Entries.Add(new PayrollCollaboratorEntry
            {
                Id = Guid.NewGuid(),
                PayrollId = payrollId,
                CollaboratorId = Guid.NewGuid(),
                CollaboratorName = $"Report {Guid.NewGuid():N}",
                DepartmentId = departmentId,
                IsApproved = entry.IsApproved,
                Payload = new PayrollCollaboratorEntryPayload
                {
                    CalculatedResult = new PayrollEntryResult
                    {
                        TotalAmount = entry.Total
                    },
                    DisplayProjectTotals = entry.ProjectTotals
                        .Select(total => new ProjectTotalAllocation(total.ProjectId, total.Amount))
                        .ToList()
                }
            });
        }

        dbContext.Payrolls.Add(payroll);
        await dbContext.SaveChangesAsync();
        return payrollId;
    }

    private async Task<Guid> AddPayrollAsync(
        Guid departmentId,
        int month,
        int year,
        PayrollStatus status,
        Guid projectId,
        (decimal Total, (Guid ProjectId, decimal Amount)[] ProjectTotals) entry,
        bool isApproved = true) =>
        await AddPayrollAsync(
            departmentId,
            month,
            year,
            status,
            projectId,
            (entry.Total, entry.ProjectTotals, isApproved));

    private sealed record PayrollReportApiResponse(
        int Year,
        PayrollReportSummaryApiResponse Summary,
        IReadOnlyList<PayrollReportMonthlyPointApiResponse> MonthlySeries,
        IReadOnlyList<PayrollReportDepartmentRowApiResponse> ByDepartment,
        IReadOnlyList<PayrollReportProjectRowApiResponse> ByProject,
        IReadOnlyList<PayrollReportCollaboratorRowApiResponse> ByCollaborator,
        PayrollReportFilterOptionsApiResponse FilterOptions);

    private sealed record PayrollReportFilterOptionsApiResponse(
        IReadOnlyList<PayrollReportFilterOptionApiResponse> Departments,
        IReadOnlyList<PayrollReportFilterOptionApiResponse> Projects);

    private sealed record PayrollReportFilterOptionApiResponse(Guid Id, string Name);

    private sealed record PayrollReportSummaryApiResponse(
        decimal TotalYear,
        decimal MonthlyAverage,
        Guid? TopDepartmentId,
        string? TopDepartmentName,
        decimal TopDepartmentAmount,
        Guid? TopProjectId,
        string? TopProjectName,
        decimal TopProjectAmount,
        int CollaboratorCount);

    private sealed record PayrollReportMonthlyPointApiResponse(int Month, decimal Amount);

    private sealed record PayrollReportDepartmentRowApiResponse(
        Guid DepartmentId,
        string DepartmentName,
        decimal Amount,
        int EntryCount,
        int PayrollCount);

    private sealed record PayrollReportProjectRowApiResponse(
        Guid ProjectId,
        string ProjectName,
        decimal Amount,
        int EntryCount);

    private sealed record PayrollReportCollaboratorRowApiResponse(
        Guid CollaboratorId,
        string CollaboratorName,
        Guid DepartmentId,
        string DepartmentName,
        decimal Amount,
        int CompetenceCount);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

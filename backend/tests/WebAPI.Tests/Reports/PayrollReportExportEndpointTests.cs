using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Core.Auth;
using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Reports;

[Collection("WebApiIntegration")]
public sealed class PayrollReportExportEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private const string ExportContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly CorePayWebApplicationFactory _factory;

    public PayrollReportExportEndpointTests(CorePayWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ExportPayrollReport_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/reports/payroll/export?year=2090");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportPayrollReport_ManagerWithoutReportsPermission_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/reports/payroll/export?year=2090");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportPayrollReport_FinancialUser_ShouldReturnXlsxFile()
    {
        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/reports/payroll/export?year=2091");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ExportContentType);
        response.Content.Headers.ContentDisposition!.FileName.Should().Be("relatorio-folha-2091.xlsx");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task ExportPayrollReport_InvalidYear_ShouldReturnBadRequest(int year)
    {
        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync($"/api/v1/reports/payroll/export?year={year}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExportPayrollReport_EmptyReport_ShouldReturnWorkbookWithHeadersOnly()
    {
        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/reports/payroll/export?year=2092");
        var workbook = await ReadWorkbookAsync(response);

        workbook.Worksheets.Should().HaveCount(5);
        workbook.Worksheet("Resumo").Cell(1, 1).GetString().Should().Be("Ano");
        workbook.Worksheet("Por setor").Cell(1, 1).GetString().Should().Be("Setor");
        workbook.Worksheet("Por projeto").Cell(1, 1).GetString().Should().Be("Projeto");
        workbook.Worksheet("Por colaborador").Cell(1, 1).GetString().Should().Be("Colaborador");
    }

    [Fact]
    public async Task ExportPayrollReport_ShouldMatchJsonReportMatrices()
    {
        var departments = await GetDepartmentIdsAsync(2);
        var projectId = await GetProjectIdAsync();
        await AddPayrollAsync(
            departments[0],
            1,
            2093,
            PayrollStatus.Approved,
            projectId,
            (Total: 12_000m, [(projectId, 12_000m)]));
        await AddPayrollAsync(
            departments[1],
            2,
            2093,
            PayrollStatus.Paid,
            projectId,
            (Total: 6_000m, [(projectId, 6_000m)]));

        var client = await CreateFinancialClientAsync();
        var jsonReport = await (await client.GetAsync("/api/v1/reports/payroll?year=2093"))
            .Content.ReadFromJsonAsync<PayrollReportApiResponse>();
        var workbook = await ReadWorkbookAsync(
            await client.GetAsync("/api/v1/reports/payroll/export?year=2093"));

        jsonReport!.Summary.TotalYear.Should().Be(18_000m);
        workbook.Worksheet("Resumo").Cell(2, 2).GetValue<decimal>().Should().Be(18_000m);
        workbook.Worksheet("Resumo").Cell(3, 2).GetValue<decimal>().Should().Be(1_500m);
        workbook.Worksheet("Resumo").Cell(6, 2).GetValue<int>().Should().Be(2);

        workbook.Worksheet("Mensal").Cell(2, 2).GetValue<decimal>().Should().Be(12_000m);
        workbook.Worksheet("Mensal").Cell(3, 2).GetValue<decimal>().Should().Be(6_000m);

        workbook.Worksheet("Por setor").RowsUsed().Count().Should().Be(jsonReport.ByDepartment.Count + 1);
        workbook.Worksheet("Por projeto").Cell(2, 2).GetValue<decimal>()
            .Should().Be(jsonReport.ByProject.Single().Amount);
        workbook.Worksheet("Por colaborador").RowsUsed().Count()
            .Should().Be(jsonReport.ByCollaborator.Count + 1);
    }

    [Fact]
    public async Task ExportPayrollReport_ShouldExcludeDraftWithoutApprovedEntries()
    {
        var departmentId = (await GetDepartmentIdsAsync(1))[0];
        var projectId = await GetProjectIdAsync();
        await AddPayrollAsync(
            departmentId,
            3,
            2094,
            PayrollStatus.Draft,
            projectId,
            (Total: 9_000m, [(projectId, 9_000m)]),
            isApproved: false);
        await AddPayrollAsync(
            departmentId,
            4,
            2094,
            PayrollStatus.Paid,
            projectId,
            (Total: 4_000m, [(projectId, 4_000m)]));

        var client = await CreateFinancialClientAsync();
        var workbook = await ReadWorkbookAsync(
            await client.GetAsync("/api/v1/reports/payroll/export?year=2094"));

        workbook.Worksheet("Resumo").Cell(2, 2).GetValue<decimal>().Should().Be(4_000m);
        workbook.Worksheet("Por setor").RowsUsed().Count().Should().Be(2);
    }

    [Fact]
    public async Task ExportPayrollReport_ProjectFilter_ShouldScopeTotals()
    {
        var departmentId = (await GetDepartmentIdsAsync(1))[0];
        var projects = await GetProjectIdsAsync(2);
        await AddPayrollAsync(
            departmentId,
            5,
            2095,
            PayrollStatus.Approved,
            projects[0],
            (Total: 10_000m, [(projects[0], 4_000m), (projects[1], 6_000m)]));

        var client = await CreateFinancialClientAsync();
        var workbook = await ReadWorkbookAsync(
            await client.GetAsync($"/api/v1/reports/payroll/export?year=2095&projectId={projects[0]}"));

        workbook.Worksheet("Resumo").Cell(2, 2).GetValue<decimal>().Should().Be(4_000m);
        workbook.Worksheet("Por projeto").RowsUsed().Count().Should().Be(2);
        workbook.Worksheet("Por projeto").Cell(2, 2).GetValue<decimal>().Should().Be(4_000m);
    }

    [Fact]
    public async Task ExportPayrollReport_ManagerWithReportsPermission_ShouldRespectDepartmentScope()
    {
        var departments = await GetDepartmentIdsAsync(2);
        var projectId = await GetProjectIdAsync();
        await AddPayrollAsync(
            departments[0],
            6,
            2096,
            PayrollStatus.Paid,
            projectId,
            (Total: 3_000m, [(projectId, 3_000m)]));
        await AddPayrollAsync(
            departments[1],
            6,
            2096,
            PayrollStatus.Paid,
            projectId,
            (Total: 8_000m, [(projectId, 8_000m)]));

        var client = _factory.CreateClient();
        var (_, _, token) = await CreateManagerWithReportsReadAsync(departments[0]);
        AuthTestHelper.SetBearerToken(client, token);

        var allowedWorkbook = await ReadWorkbookAsync(
            await client.GetAsync("/api/v1/reports/payroll/export?year=2096"));
        allowedWorkbook.Worksheet("Resumo").Cell(2, 2).GetValue<decimal>().Should().Be(3_000m);

        var forbidden = await client.GetAsync(
            $"/api/v1/reports/payroll/export?year=2096&departmentId={departments[1]}");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportPayrollReport_MissingSnapshot_ShouldReturnBadRequest()
    {
        var departmentId = (await GetDepartmentIdsAsync(1))[0];
        await AddPayrollWithoutSnapshotAsync(departmentId, 7, 2097);

        var client = await CreateFinancialClientAsync();
        var response = await client.GetAsync("/api/v1/reports/payroll/export?year=2097");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("reports.snapshot_missing");
    }

    private static async Task<XLWorkbook> ReadWorkbookAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ExportContentType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        return new XLWorkbook(new MemoryStream(bytes));
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
        var email = $"reports-export-manager-{Guid.NewGuid():N}@corepay.test";
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
            DisplayName = "Reports Export Manager",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        createResult.Succeeded.Should().BeTrue();

        const string customRoleName = "ReportsExportScopedManager";
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

    private async Task AddPayrollWithoutSnapshotAsync(Guid departmentId, int month, int year)
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
            Status = PayrollStatus.Approved,
            TotalAmount = 1_000m
        };

        payroll.Entries.Add(new PayrollCollaboratorEntry
        {
            Id = Guid.NewGuid(),
            PayrollId = payrollId,
            CollaboratorId = Guid.NewGuid(),
            CollaboratorName = "Missing Snapshot",
            DepartmentId = departmentId,
            IsApproved = true,
            Payload = new PayrollCollaboratorEntryPayload()
        });

        dbContext.Payrolls.Add(payroll);
        await dbContext.SaveChangesAsync();
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
                CollaboratorName = $"Export {Guid.NewGuid():N}",
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
        IReadOnlyList<PayrollReportDepartmentRowApiResponse> ByDepartment,
        IReadOnlyList<PayrollReportProjectRowApiResponse> ByProject,
        IReadOnlyList<PayrollReportCollaboratorRowApiResponse> ByCollaborator);

    private sealed record PayrollReportSummaryApiResponse(decimal TotalYear, decimal MonthlyAverage);

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

    private sealed record ApiErrorResponse(string? Error, string? Message);
}

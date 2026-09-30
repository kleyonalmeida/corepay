using BuildingBlocks.Results;
using BuildingBlocks.Time;
using Core.Application.Collaborators;
using Core.Application.Payrolls;
using Core.Application.Reports;
using Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Reports;

public sealed class PayrollReportStore(AppDbContext dbContext, TimeProvider timeProvider) : IPayrollReportStore
{
    public async Task<Result<PayrollReportResponse>> GetPayrollReportAsync(
        PayrollReportFilters filters,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var year = filters.Year ?? BahiaTimeZone.GetCivilMonthYear(timeProvider.GetUtcNow()).Year;

        if (year is < 2000 or > 2100)
        {
            return Result<PayrollReportResponse>.Failure(
                Error.Validation("reports.invalid_year", "Year is out of allowed range."));
        }

        var scopeResult = ResolveDepartmentScope(filters.DepartmentId, access);
        if (scopeResult.IsFailure)
        {
            return Result<PayrollReportResponse>.Failure(scopeResult.Error!);
        }

        var query = dbContext.Payrolls
            .AsNoTracking()
            .Include(p => p.Department)
            .Include(p => p.Entries)
            .Where(p => p.Year == year);

        query = ApplyDepartmentScope(query, scopeResult.Value!);

        var payrolls = await query.ToListAsync(cancellationToken);
        var winningPayrolls = PayrollReportSelection.SelectWinningPayrolls(payrolls).ToList();

        var lineItemsResult = PayrollReportAggregator.BuildLineItems(winningPayrolls, filters.ProjectId);
        if (lineItemsResult.IsFailure)
        {
            return Result<PayrollReportResponse>.Failure(lineItemsResult.Error!);
        }

        var referencedProjectIds = lineItemsResult.Value!
            .SelectMany(lineItem => lineItem.ProjectAmounts.Keys)
            .Distinct()
            .ToList();

        var projectNames = referencedProjectIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Projects
                .AsNoTracking()
                .Where(project => referencedProjectIds.Contains(project.Id))
                .ToDictionaryAsync(project => project.Id, project => project.Name, cancellationToken);

        var filterOptions = await LoadFilterOptionsAsync(access, cancellationToken);

        return PayrollReportAggregator.Aggregate(
            year,
            lineItemsResult.Value!,
            projectNames,
            filterOptions);
    }

    private async Task<PayrollReportFilterOptionsResponse> LoadFilterOptionsAsync(
        PayrollAccessContext access,
        CancellationToken cancellationToken)
    {
        var departmentQuery = dbContext.Departments
            .AsNoTracking()
            .Where(department => department.IsActive);

        if (CollaboratorAccessResolver.IsDepartmentScoped(access.Roles))
        {
            var allowed = access.AllowedDepartmentIds ?? [];
            departmentQuery = departmentQuery.Where(department => allowed.Contains(department.Id));
        }

        var departments = await departmentQuery
            .OrderBy(department => department.Name)
            .Select(department => new PayrollReportFilterOption(department.Id, department.Name))
            .ToListAsync(cancellationToken);

        var projects = await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.IsActive)
            .OrderBy(project => project.Name)
            .Select(project => new PayrollReportFilterOption(project.Id, project.Name))
            .ToListAsync(cancellationToken);

        return new PayrollReportFilterOptionsResponse(departments, projects);
    }

    private static IQueryable<Payroll> ApplyDepartmentScope(IQueryable<Payroll> query, DepartmentScope scope)
    {
        if (scope.DepartmentId is not null)
        {
            return query.Where(p => p.DepartmentId == scope.DepartmentId.Value);
        }

        if (scope.AllowedDepartmentIds is not null)
        {
            return query.Where(p => scope.AllowedDepartmentIds.Contains(p.DepartmentId));
        }

        return query;
    }

    private static Result<DepartmentScope> ResolveDepartmentScope(
        Guid? departmentId,
        PayrollAccessContext access)
    {
        if (!CollaboratorAccessResolver.IsDepartmentScoped(access.Roles))
        {
            return Result<DepartmentScope>.Success(new DepartmentScope(departmentId, null));
        }

        var allowed = access.AllowedDepartmentIds ?? [];
        if (allowed.Count == 0)
        {
            return Result<DepartmentScope>.Success(new DepartmentScope(null, []));
        }

        if (departmentId is not null && !allowed.Contains(departmentId.Value))
        {
            return Result<DepartmentScope>.Failure(
                Error.Forbidden("reports.department_forbidden", "Department is outside your scope."));
        }

        return Result<DepartmentScope>.Success(
            departmentId is not null
                ? new DepartmentScope(departmentId, null)
                : new DepartmentScope(null, allowed));
    }

    private sealed record DepartmentScope(Guid? DepartmentId, IReadOnlyList<Guid>? AllowedDepartmentIds);
}

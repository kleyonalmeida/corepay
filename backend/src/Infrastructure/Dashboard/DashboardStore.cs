using BuildingBlocks.Results;
using BuildingBlocks.Time;
using Core.Application.Collaborators;
using Core.Application.Dashboard;
using Core.Application.Finance;
using Core.Application.Payrolls;
using Core.Auth;
using Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dashboard;

public sealed class DashboardStore(AppDbContext dbContext, TimeProvider timeProvider) : IDashboardStore
{
    public async Task<Result<DashboardResponse>> GetAsync(
        DashboardFilters filters,
        PayrollAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var defaultCompetence = GetPreviousCompetence(timeProvider.GetUtcNow());
        var month = filters.Month ?? defaultCompetence.Month;
        var year = filters.Year ?? defaultCompetence.Year;

        if (month is < 1 or > 12)
        {
            return Result<DashboardResponse>.Failure(
                Error.Validation("dashboard.invalid_month", "Month must be between 1 and 12."));
        }

        if (year is < 2000 or > 2100)
        {
            return Result<DashboardResponse>.Failure(
                Error.Validation("dashboard.invalid_year", "Year is out of allowed range."));
        }

        var canReadPayrolls = HasPermission(access, AppPermissions.PayrollsRead);
        var canReadCollaborators = HasPermission(access, AppPermissions.CollaboratorsRead);
        var allowedDepartmentIds = CollaboratorAccessResolver.IsDepartmentScoped(access.Roles)
            ? access.AllowedDepartmentIds ?? []
            : null;

        DashboardPayrollStatsResponse? payrollStats = null;
        IReadOnlyList<DashboardRecentPayrollResponse>? recentPayrolls = null;
        int? activeCollaborators = null;

        if (canReadPayrolls)
        {
            var scopedPayrolls = ApplyDepartmentScope(
                dbContext.Payrolls.AsNoTracking(),
                allowedDepartmentIds);

            var competenceQuery = scopedPayrolls.Where(payroll =>
                payroll.Month == month && payroll.Year == year);

            var statusCounts = await competenceQuery
                .Where(payroll => payroll.Status != PayrollStatus.Draft)
                .GroupBy(payroll => payroll.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

            var lockedEntries = await dbContext.PayrollCollaboratorEntries
                .AsNoTracking()
                .Where(entry => entry.Payroll.Month == month
                                && entry.Payroll.Year == year
                                && (entry.Payroll.Status == PayrollStatus.Approved
                                    || entry.Payroll.Status == PayrollStatus.Paid)
                                && (allowedDepartmentIds == null
                                    || allowedDepartmentIds.Contains(entry.Payroll.DepartmentId)))
                .Select(entry => new
                {
                    entry.IsPaid,
                    entry.Payload
                })
                .ToListAsync(cancellationToken);

            var lockedAmounts = lockedEntries
                .Select(entry =>
                {
                    var amounts = FinanceEntryAmounts.FromResult(entry.Payload.CalculatedResult);
                    return new FinanceEntryAmounts(
                        amounts.TotalAmount,
                        amounts.PlatformTotal,
                        amounts.AmountToReceive,
                        entry.IsPaid);
                })
                .ToList();

            payrollStats = new DashboardPayrollStatsResponse(
                statusCounts.Sum(item => item.Count),
                statusCounts.FirstOrDefault(item => item.Status == PayrollStatus.PendingApproval)?.Count ?? 0,
                statusCounts.Where(item => item.Status is PayrollStatus.Approved or PayrollStatus.Paid)
                    .Sum(item => item.Count),
                statusCounts.FirstOrDefault(item => item.Status == PayrollStatus.Rejected)?.Count ?? 0,
                lockedAmounts.Where(entry => !entry.IsPaid).Sum(entry => entry.AmountToReceive),
                lockedAmounts.Where(entry => entry.IsPaid).Sum(entry => entry.AmountToReceive));

            recentPayrolls = await ApplyDepartmentScope(
                    dbContext.Payrolls.AsNoTracking(),
                    allowedDepartmentIds)
                .OrderByDescending(payroll => payroll.Year)
                .ThenByDescending(payroll => payroll.Month)
                .ThenBy(payroll => payroll.Department.Name)
                .ThenBy(payroll => payroll.Id)
                .Take(8)
                .Select(payroll => new DashboardRecentPayrollResponse(
                    payroll.Id,
                    payroll.DepartmentId,
                    payroll.Department.Name,
                    payroll.Month,
                    payroll.Year,
                    payroll.Status,
                    payroll.TotalAmount,
                    dbContext.PayrollCollaboratorEntries.Count(entry => entry.PayrollId == payroll.Id)))
                .ToListAsync(cancellationToken);
        }

        if (canReadCollaborators)
        {
            var collaborators = dbContext.Collaborators
                .AsNoTracking()
                .Where(collaborator => collaborator.IsActive);
            if (allowedDepartmentIds is not null)
            {
                collaborators = collaborators.Where(collaborator =>
                    allowedDepartmentIds.Contains(collaborator.DepartmentId));
            }

            activeCollaborators = await collaborators.CountAsync(cancellationToken);
        }

        return Result<DashboardResponse>.Success(new DashboardResponse(
            month,
            year,
            payrollStats,
            activeCollaborators,
            recentPayrolls));
    }

    private static IQueryable<Payroll> ApplyDepartmentScope(
        IQueryable<Payroll> query,
        IReadOnlyList<Guid>? allowedDepartmentIds) =>
        allowedDepartmentIds is null
            ? query
            : query.Where(payroll => allowedDepartmentIds.Contains(payroll.DepartmentId));

    private static bool HasPermission(PayrollAccessContext access, string permission) =>
        access.Roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase)
        || access.Permissions?.Contains(permission, StringComparer.OrdinalIgnoreCase) == true;

    private static (int Month, int Year) GetPreviousCompetence(DateTimeOffset utcNow)
    {
        var (month, year) = BahiaTimeZone.GetCivilMonthYear(utcNow);
        var previous = new DateOnly(year, month, 1).AddMonths(-1);
        return (previous.Month, previous.Year);
    }
}

using BuildingBlocks.Results;
using Core.Application.Finance;
using Core.Domain;

namespace Core.Application.Reports;

public static class PayrollReportAggregator
{
    public sealed record ReportLineItem(
        Guid DepartmentId,
        string DepartmentName,
        int Month,
        int Year,
        Guid CollaboratorId,
        string CollaboratorName,
        decimal Amount,
        IReadOnlyDictionary<Guid, decimal> ProjectAmounts);

    public static Result<PayrollReportResponse> Aggregate(
        int year,
        IReadOnlyList<ReportLineItem> lines,
        IReadOnlyDictionary<Guid, string> projectNames,
        PayrollReportFilterOptionsResponse filterOptions)
    {
        var monthlyAmounts = Enumerable.Range(1, 12)
            .ToDictionary(month => month, _ => 0m);

        var departmentTotals = new Dictionary<Guid, (string Name, decimal Amount, int EntryCount, HashSet<(int Month, int Year)> Payrolls)>();
        var projectTotals = new Dictionary<Guid, (decimal Amount, int EntryCount)>();
        var collaboratorTotals = new Dictionary<Guid, (string Name, Guid DepartmentId, string DepartmentName, decimal Amount, HashSet<(int Month, int Year)> Competences)>();

        foreach (var line in lines)
        {
            monthlyAmounts[line.Month] += line.Amount;

            if (!departmentTotals.TryGetValue(line.DepartmentId, out var department))
            {
                department = (line.DepartmentName, 0m, 0, []);
                departmentTotals[line.DepartmentId] = department;
            }

            department.Amount += line.Amount;
            department.EntryCount++;
            department.Payrolls.Add((line.Month, line.Year));
            departmentTotals[line.DepartmentId] = department;

            foreach (var (projectId, amount) in line.ProjectAmounts)
            {
                if (amount == 0m)
                {
                    continue;
                }

                if (!projectTotals.TryGetValue(projectId, out var project))
                {
                    project = (0m, 0);
                }

                project.Amount += amount;
                project.EntryCount++;
                projectTotals[projectId] = project;
            }

            if (!collaboratorTotals.TryGetValue(line.CollaboratorId, out var collaborator))
            {
                collaborator = (line.CollaboratorName, line.DepartmentId, line.DepartmentName, 0m, []);
            }

            collaborator.Amount += line.Amount;
            collaborator.Competences.Add((line.Month, line.Year));
            collaboratorTotals[line.CollaboratorId] = collaborator;
        }

        var totalYear = monthlyAmounts.Values.Sum();
        var monthlyAverage = totalYear / 12m;

        var topDepartment = departmentTotals
            .OrderByDescending(pair => pair.Value.Amount)
            .ThenBy(pair => pair.Value.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var topProject = projectTotals
            .OrderByDescending(pair => pair.Value.Amount)
            .ThenBy(pair => projectNames.GetValueOrDefault(pair.Key, string.Empty), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var summary = new PayrollReportSummaryResponse(
            totalYear,
            monthlyAverage,
            topDepartment.Key == Guid.Empty ? null : topDepartment.Key,
            topDepartment.Key == Guid.Empty ? null : topDepartment.Value.Name,
            topDepartment.Key == Guid.Empty ? 0m : topDepartment.Value.Amount,
            topProject.Key == Guid.Empty ? null : topProject.Key,
            topProject.Key == Guid.Empty ? null : projectNames.GetValueOrDefault(topProject.Key, "Projeto"),
            topProject.Key == Guid.Empty ? 0m : topProject.Value.Amount,
            collaboratorTotals.Count);

        var monthlySeries = monthlyAmounts
            .OrderBy(pair => pair.Key)
            .Select(pair => new PayrollReportMonthlyPointResponse(pair.Key, pair.Value))
            .ToList();

        var byDepartment = departmentTotals
            .Select(pair => new PayrollReportDepartmentRowResponse(
                pair.Key,
                pair.Value.Name,
                pair.Value.Amount,
                pair.Value.EntryCount,
                pair.Value.Payrolls.Count))
            .OrderByDescending(row => row.Amount)
            .ThenBy(row => row.DepartmentName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var byProject = projectTotals
            .Select(pair => new PayrollReportProjectRowResponse(
                pair.Key,
                projectNames.GetValueOrDefault(pair.Key, "Projeto"),
                pair.Value.Amount,
                pair.Value.EntryCount))
            .OrderByDescending(row => row.Amount)
            .ThenBy(row => row.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var byCollaborator = collaboratorTotals
            .Select(pair => new PayrollReportCollaboratorRowResponse(
                pair.Key,
                pair.Value.Name,
                pair.Value.DepartmentId,
                pair.Value.DepartmentName,
                pair.Value.Amount,
                pair.Value.Competences.Count))
            .OrderByDescending(row => row.Amount)
            .ThenBy(row => row.CollaboratorName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<PayrollReportResponse>.Success(new PayrollReportResponse(
            year,
            summary,
            monthlySeries,
            byDepartment,
            byProject,
            byCollaborator,
            filterOptions));
    }

    public static Result<IReadOnlyList<ReportLineItem>> BuildLineItems(
        IEnumerable<Payroll> payrolls,
        Guid? projectId)
    {
        var lines = new List<ReportLineItem>();

        foreach (var payroll in payrolls)
        {
            foreach (var entry in payroll.Entries)
            {
                if (!FinanceEntryVisibility.IsEntryVisible(payroll.Status, entry.IsApproved))
                {
                    continue;
                }

                var totalResult = PayrollReportSnapshotReader.GetEntryTotalAmount(entry);
                if (totalResult.IsFailure)
                {
                    return Result<IReadOnlyList<ReportLineItem>>.Failure(totalResult.Error!);
                }

                var projectAmounts = entry.Payload.DisplayProjectTotals
                    .GroupBy(total => total.ProjectId)
                    .ToDictionary(group => group.Key, group => group.Sum(total => total.Amount));

                if (projectId is not null)
                {
                    var scopedAmount = projectAmounts.GetValueOrDefault(projectId.Value);
                    if (scopedAmount == 0m)
                    {
                        continue;
                    }

                    projectAmounts = new Dictionary<Guid, decimal> { [projectId.Value] = scopedAmount };
                }

                var amount = projectId is null
                    ? totalResult.Value!
                    : projectAmounts[projectId.Value];

                lines.Add(new ReportLineItem(
                    payroll.DepartmentId,
                    payroll.Department?.Name ?? string.Empty,
                    payroll.Month,
                    payroll.Year,
                    entry.CollaboratorId,
                    entry.CollaboratorName,
                    amount,
                    projectAmounts));
            }
        }

        return Result<IReadOnlyList<ReportLineItem>>.Success(lines);
    }
}

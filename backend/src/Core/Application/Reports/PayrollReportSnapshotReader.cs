using BuildingBlocks.Results;
using Core.Domain;

namespace Core.Application.Reports;

public static class PayrollReportSnapshotReader
{
    public static Result<decimal> GetEntryTotalAmount(PayrollCollaboratorEntry entry)
    {
        if (entry.Payload.CalculatedResult is null)
        {
            return Result<decimal>.Failure(
                Error.Validation(
                    "reports.snapshot_missing",
                    "Payroll entry is missing a locked calculation snapshot."));
        }

        return Result<decimal>.Success(entry.Payload.CalculatedResult.TotalAmount);
    }

    public static decimal GetProjectAmount(PayrollCollaboratorEntry entry, Guid projectId) =>
        entry.Payload.DisplayProjectTotals
            .Where(total => total.ProjectId == projectId)
            .Sum(total => total.Amount);
}

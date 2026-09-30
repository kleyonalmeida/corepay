using BuildingBlocks.Results;
using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Normaliza alocações de custo por projeto para exibição (Fase 6.2 — REGRAS §6.4, §6.8, §6.9).
/// </summary>
public static class ProjectDisplayHelper
{
    public static Result<IReadOnlyList<ProjectTotalAllocation>> Transform(
        PayrollEntryInput input,
        IReadOnlyList<ProjectTotalAllocation> calculatedTotals,
        PayrollEntryResult entryResult,
        Guid affiliatesProjectId)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(calculatedTotals);
        ArgumentNullException.ThrowIfNull(entryResult);

        if (input.Department is null)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(calculatedTotals);
        }

        if (DepartmentCalculationFlags.IsAffiliatesDepartment(input.Department))
        {
            return BuildAffiliatesAllocation(input, entryResult, affiliatesProjectId);
        }

        if (input.Department.CalculationType == CalculationProfile.Management)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(
                AggregateManagementBreakdown(input, calculatedTotals));
        }

        if (DepartmentCalculationFlags.RoutesFixedToLimaKarttos(input.Department))
        {
            return NormalizeLimaKarttosDisplay(input, calculatedTotals);
        }

        return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(calculatedTotals);
    }

    public static IReadOnlyList<ProjectTotalAllocation> AggregateManagementBreakdown(
        PayrollEntryInput input,
        IReadOnlyList<ProjectTotalAllocation> calculatedTotals)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.ManagementRevenueEntries.Count == 0)
        {
            return calculatedTotals;
        }

        var accumulator = new ProjectTotalsAccumulator();
        foreach (var revenueEntry in input.ManagementRevenueEntries)
        {
            foreach (var breakdown in revenueEntry.ProjectBreakdown)
            {
                accumulator.AddOther(breakdown.ProjectId, breakdown.Amount);
            }
        }

        foreach (var allocation in calculatedTotals)
        {
            if (!input.ManagementRevenueEntries
                .SelectMany(entry => entry.ProjectBreakdown)
                .Any(breakdown => breakdown.ProjectId == allocation.ProjectId))
            {
                accumulator.AddRange([allocation]);
            }
        }

        return accumulator.ToList();
    }

    private static Result<IReadOnlyList<ProjectTotalAllocation>> BuildAffiliatesAllocation(
        PayrollEntryInput input,
        PayrollEntryResult entryResult,
        Guid affiliatesProjectId)
    {
        var deductions = input.DeductionEntries.Sum(d => d.Value);
        var unallocatedBonuses = input.BonusEntries
            .Where(b => !b.ProjectId.HasValue)
            .Sum(b => b.Value);

        var amount = Money.FromDecimal(entryResult.TotalAmount + deductions - unallocatedBonuses)
            .RoundToCurrencyScale()
            .Amount;

        return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(
            [new ProjectTotalAllocation(affiliatesProjectId, amount, Other: amount)]);
    }

    private static Result<IReadOnlyList<ProjectTotalAllocation>> NormalizeLimaKarttosDisplay(
        PayrollEntryInput input,
        IReadOnlyList<ProjectTotalAllocation> calculatedTotals)
    {
        if (calculatedTotals.Count == 0)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(calculatedTotals);
        }

        var limaSnapshot = input.ProjectSnapshots.FirstOrDefault(snapshot => snapshot.IsDefaultAllocationTarget);
        if (limaSnapshot is null)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(calculatedTotals);
        }

        var accumulator = new ProjectTotalsAccumulator();
        foreach (var allocation in calculatedTotals)
        {
            var b = new ProjectTotalAllocation(limaSnapshot.ProjectId, allocation.Amount, allocation.BaseSalary, allocation.Commission, allocation.GoalBonus, allocation.ManualBonus, allocation.Other);
            accumulator.AddRange([b]);
        }

        return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(accumulator.ToList());
    }
}

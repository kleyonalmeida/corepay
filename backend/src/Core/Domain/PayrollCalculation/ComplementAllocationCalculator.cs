using BuildingBlocks.Results;
using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Distribui complemento, Betano, extra FTD e plataforma entre projetos pagadores (REGRAS §6.1).
/// </summary>
public static class ComplementAllocationCalculator
{
    public static Result ValidateComplementPayingProjects(
        IReadOnlyList<ComplementPayingProjectInput> complementPayingProjects)
    {
        if (complementPayingProjects.Count == 0)
        {
            return Result.Success();
        }

        var totalPercentage = complementPayingProjects.Sum(entry => entry.Percentage);
        if (totalPercentage != 100m)
        {
            return Result.Failure(
                Error.Validation(
                    "payroll.complement_paying_projects_invalid_sum",
                    "A soma dos percentuais de complement_paying_projects deve ser exatamente 100."));
        }

        return Result.Success();
    }

    public static Result<IReadOnlyList<ProjectTotalAllocation>> Allocate(
        decimal bucketAmount,
        PayrollEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (bucketAmount <= 0m)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Success([]);
        }

        var validation = ValidateComplementPayingProjects(input.ComplementPayingProjects);
        if (validation.IsFailure)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(validation.Error!);
        }

        if (input.ComplementPayingProjects.Count > 0)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(
                AllocateProportional(bucketAmount, input.ComplementPayingProjects));
        }

        if (input.CommissionPayingProjectId.HasValue)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(
            [
                new ProjectTotalAllocation(input.CommissionPayingProjectId.Value, bucketAmount, BaseSalary: bucketAmount)
            ]);
        }

        var defaultTarget = input.ProjectSnapshots
            .FirstOrDefault(snapshot => snapshot.IsDefaultAllocationTarget);

        if (defaultTarget is null)
        {
            return Result<IReadOnlyList<ProjectTotalAllocation>>.Failure(
                Error.Validation(
                    "payroll.complement_allocation_target_missing",
                    "Informe complement_paying_projects, commission_paying_project_id ou projeto Lima Karttos (IsDefaultAllocationTarget)."));
        }

        return Result<IReadOnlyList<ProjectTotalAllocation>>.Success(
        [
            new ProjectTotalAllocation(defaultTarget.ProjectId, bucketAmount, BaseSalary: bucketAmount)
        ]);
    }

    private static IReadOnlyList<ProjectTotalAllocation> AllocateProportional(
        decimal bucketAmount,
        IReadOnlyList<ComplementPayingProjectInput> complementPayingProjects)
    {
        var consolidated = complementPayingProjects
            .GroupBy(entry => entry.ProjectId)
            .Select(group => new ComplementPayingProjectInput(
                group.Key,
                group.Sum(entry => entry.Percentage)))
            .ToList();

        var results = new List<ProjectTotalAllocation>(consolidated.Count);
        var distributed = 0m;

        for (var index = 0; index < consolidated.Count; index++)
        {
            var entry = consolidated[index];
            var amount = index == consolidated.Count - 1
                ? Money.FromDecimal(bucketAmount - distributed).RoundToCurrencyScale().Amount
                : Money.FromDecimal(bucketAmount * entry.Percentage / 100m)
                    .RoundToCurrencyScale()
                    .Amount;

            distributed += amount;
            results.Add(new ProjectTotalAllocation(entry.ProjectId, amount, BaseSalary: amount));
        }

        return results;
    }
}

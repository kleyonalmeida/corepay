using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Acumula alocações monetárias por <see cref="Guid"/> de projeto.
/// </summary>
public sealed class ProjectTotalsAccumulator
{
    private readonly Dictionary<Guid, decimal> _amounts = new();

    public void Add(Guid projectId, decimal amount)
    {
        if (amount == 0m)
        {
            return;
        }

        _amounts[projectId] = _amounts.GetValueOrDefault(projectId) + amount;
    }

    public void AddRange(IEnumerable<ProjectTotalAllocation> allocations)
    {
        foreach (var allocation in allocations)
        {
            Add(allocation.ProjectId, allocation.Amount);
        }
    }

    public void AddRange(IEnumerable<FixedAllocationCalculator.Allocation> allocations)
    {
        foreach (var allocation in allocations)
        {
            Add(allocation.ProjectId, allocation.Amount);
        }
    }

    public IReadOnlyList<ProjectTotalAllocation> ToList() =>
        _amounts
            .Where(pair => pair.Value != 0m)
            .OrderBy(pair => pair.Key)
            .Select(pair => new ProjectTotalAllocation(
                pair.Key,
                Money.FromDecimal(pair.Value).RoundToCurrencyScale().Amount))
            .ToList();
}

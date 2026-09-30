using BuildingBlocks.ValueObjects;

namespace Core.Domain.PayrollCalculation;

/// <summary>
/// Acumula alocações monetárias por <see cref="Guid"/> de projeto.
/// </summary>
public sealed class ProjectTotalsAccumulator
{
    private sealed class Breakdown
    {
        public decimal Total { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal Commission { get; set; }
        public decimal GoalBonus { get; set; }
        public decimal ManualBonus { get; set; }
        public decimal Other { get; set; }
    }

    private readonly Dictionary<Guid, Breakdown> _amounts = new();

    private Breakdown GetOrCreate(Guid projectId)
    {
        if (!_amounts.TryGetValue(projectId, out var b))
        {
            b = new Breakdown();
            _amounts[projectId] = b;
        }
        return b;
    }

    public void Add(Guid projectId, decimal amount)
    {
        if (amount == 0m) return;
        var b = GetOrCreate(projectId);
        b.Total += amount;
        b.Other += amount;
    }
    
    public void AddBaseSalary(Guid projectId, decimal amount)
    {
        if (amount == 0m) return;
        var b = GetOrCreate(projectId);
        b.Total += amount;
        b.BaseSalary += amount;
    }

    public void AddCommission(Guid projectId, decimal amount)
    {
        if (amount == 0m) return;
        var b = GetOrCreate(projectId);
        b.Total += amount;
        b.Commission += amount;
    }

    public void AddGoalBonus(Guid projectId, decimal amount)
    {
        if (amount == 0m) return;
        var b = GetOrCreate(projectId);
        b.Total += amount;
        b.GoalBonus += amount;
    }

    public void AddManualBonus(Guid projectId, decimal amount)
    {
        if (amount == 0m) return;
        var b = GetOrCreate(projectId);
        b.Total += amount;
        b.ManualBonus += amount;
    }

    public void AddOther(Guid projectId, decimal amount)
    {
        if (amount == 0m) return;
        var b = GetOrCreate(projectId);
        b.Total += amount;
        b.Other += amount;
    }

    public void AddRange(IEnumerable<ProjectTotalAllocation> allocations)
    {
        foreach (var allocation in allocations)
        {
            if (allocation.Amount == 0m) continue;
            var b = GetOrCreate(allocation.ProjectId);
            b.Total += allocation.Amount;
            b.BaseSalary += allocation.BaseSalary;
            b.Commission += allocation.Commission;
            b.GoalBonus += allocation.GoalBonus;
            b.ManualBonus += allocation.ManualBonus;
            b.Other += allocation.Other;
        }
    }

    public void AddRangeFixed(IEnumerable<FixedAllocationCalculator.Allocation> allocations)
    {
        foreach (var allocation in allocations)
        {
            AddBaseSalary(allocation.ProjectId, allocation.Amount);
        }
    }

    public IReadOnlyList<ProjectTotalAllocation> ToList() =>
        _amounts
            .Where(pair => pair.Value.Total != 0m)
            .OrderBy(pair => pair.Key)
            .Select(pair => new ProjectTotalAllocation(
                pair.Key,
                Money.FromDecimal(pair.Value.Total).RoundToCurrencyScale().Amount,
                Money.FromDecimal(pair.Value.BaseSalary).RoundToCurrencyScale().Amount,
                Money.FromDecimal(pair.Value.Commission).RoundToCurrencyScale().Amount,
                Money.FromDecimal(pair.Value.GoalBonus).RoundToCurrencyScale().Amount,
                Money.FromDecimal(pair.Value.ManualBonus).RoundToCurrencyScale().Amount,
                Money.FromDecimal(pair.Value.Other).RoundToCurrencyScale().Amount))
            .ToList();
}

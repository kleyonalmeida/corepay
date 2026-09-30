namespace Core.Domain;

public sealed class Department
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public CalculationProfile CalculationType { get; set; }

    public decimal GoalBonusPercentage { get; set; }

    public decimal LowRevenueThreshold { get; set; } = 200_000m;

    public decimal LowRevenueBonusPct { get; set; } = 0.4m;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsAllocatedFixed { get; set; }

    public bool RoutesFixedToLimaKarttos { get; set; }

    public ICollection<CareerLevel> CareerLevels { get; set; } = [];
}

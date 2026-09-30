namespace Core.Domain;

public sealed class ProjectRevenue
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal ValueIgaming { get; set; }

    public decimal ValueVendas { get; set; }

    public decimal Value { get; set; }

    public decimal GroupPercentage { get; set; }

    public string? Notes { get; set; }
}

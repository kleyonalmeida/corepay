namespace Core.Domain;

public sealed class TrafficInvestment
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal MonthlyTarget { get; set; }

    public ICollection<TrafficInvestmentWeek> Weeks { get; set; } = [];
}

namespace Core.Domain.TrafficInvestmentCalculation;

public sealed record TrafficWeekResult
{
    public int WeekNumber { get; init; }

    public decimal RequestedAmount { get; init; }

    public decimal DepositedAmount { get; init; }

    public decimal SpentAmount { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal Balance { get; init; }

    public decimal SuggestedNext { get; init; }
}

public sealed record TrafficInvestmentResult
{
    public decimal MonthlyTarget { get; init; }

    public IReadOnlyList<TrafficWeekResult> Weeks { get; init; } = [];
}

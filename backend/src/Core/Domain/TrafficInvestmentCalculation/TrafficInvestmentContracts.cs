namespace Core.Domain.TrafficInvestmentCalculation;

public sealed record TrafficDepositInput(
    decimal RequestedAmount,
    decimal DepositedAmount,
    TrafficDepositStatus Status);

public sealed record TrafficChannelSpendInput(
    TrafficMediaChannel Channel,
    decimal Amount);

public sealed record TrafficWeekInput
{
    public int WeekNumber { get; init; }

    public IReadOnlyList<TrafficDepositInput> Deposits { get; init; } = [];

    public IReadOnlyList<TrafficChannelSpendInput> ChannelSpends { get; init; } = [];
}

public sealed record TrafficInvestmentInput
{
    public decimal MonthlyTarget { get; init; }

    public IReadOnlyList<TrafficWeekInput> Weeks { get; init; } = [];
}

/// <summary>
/// Entrada legada com <c>requested_amount</c> no nível da semana (REGRAS §8.1).
/// </summary>
public sealed record LegacyTrafficWeekInput
{
    public int WeekNumber { get; init; }

    public decimal? LegacyRequestedAmount { get; init; }

    public IReadOnlyList<TrafficDepositInput> Deposits { get; init; } = [];

    public IReadOnlyList<TrafficChannelSpendInput> ChannelSpends { get; init; } = [];
}

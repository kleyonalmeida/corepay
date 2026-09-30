namespace Core.Domain.TrafficInvestmentCalculation;

/// <summary>
/// Normaliza semanas legadas com <c>requested_amount</c> no nível da semana (REGRAS §8.1).
/// </summary>
public static class TrafficLegacyWeekAdapter
{
    public static TrafficWeekInput Adapt(LegacyTrafficWeekInput legacy)
    {
        ArgumentNullException.ThrowIfNull(legacy);

        var deposits = legacy.Deposits.Count > 0
            ? legacy.Deposits
            : legacy.LegacyRequestedAmount is > 0
                ? (IReadOnlyList<TrafficDepositInput>)
                [
                    new TrafficDepositInput(
                        legacy.LegacyRequestedAmount.Value,
                        0m,
                        TrafficDepositStatus.Requested)
                ]
                : [];

        return new TrafficWeekInput
        {
            WeekNumber = legacy.WeekNumber,
            Deposits = deposits,
            ChannelSpends = legacy.ChannelSpends
        };
    }
}

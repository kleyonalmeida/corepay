using Core.Domain.TrafficInvestmentCalculation;

namespace Core.Domain;

public sealed class TrafficWeekDeposit
{
    public Guid Id { get; set; }

    public Guid TrafficInvestmentWeekId { get; set; }

    public TrafficInvestmentWeek Week { get; set; } = null!;

    public decimal RequestedAmount { get; set; }

    public decimal DepositedAmount { get; set; }

    public TrafficDepositStatus Status { get; set; }
}

using Core.Domain.TrafficInvestmentCalculation;

namespace Core.Domain;

public sealed class TrafficWeekChannelSpend
{
    public Guid Id { get; set; }

    public Guid TrafficInvestmentWeekId { get; set; }

    public TrafficInvestmentWeek Week { get; set; } = null!;

    public TrafficMediaChannel Channel { get; set; }

    public decimal Amount { get; set; }
}

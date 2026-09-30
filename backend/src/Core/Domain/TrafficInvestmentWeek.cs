namespace Core.Domain;

public sealed class TrafficInvestmentWeek
{
    public Guid Id { get; set; }

    public Guid TrafficInvestmentId { get; set; }

    public TrafficInvestment TrafficInvestment { get; set; } = null!;

    public int WeekNumber { get; set; }

    public ICollection<TrafficWeekDeposit> Deposits { get; set; } = [];

    public ICollection<TrafficWeekChannelSpend> ChannelSpends { get; set; } = [];
}

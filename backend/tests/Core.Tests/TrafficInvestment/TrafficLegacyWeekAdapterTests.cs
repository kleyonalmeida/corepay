using Core.Domain.TrafficInvestmentCalculation;
using FluentAssertions;

namespace Core.Tests.TrafficInvestment;

public class TrafficLegacyWeekAdapterTests
{
    [Fact]
    public void Adapt_ShouldConvertLegacyRequestedAmount_ToSingleRequestedDeposit()
    {
        var legacy = new LegacyTrafficWeekInput
        {
            WeekNumber = 1,
            LegacyRequestedAmount = 1500m
        };

        var week = TrafficLegacyWeekAdapter.Adapt(legacy);

        week.WeekNumber.Should().Be(1);
        week.Deposits.Should().ContainSingle();
        week.Deposits[0].RequestedAmount.Should().Be(1500m);
        week.Deposits[0].DepositedAmount.Should().Be(0m);
        week.Deposits[0].Status.Should().Be(TrafficDepositStatus.Requested);
    }

    [Fact]
    public void Adapt_ShouldPreserveExistingDeposits_WhenLegacyRequestedAmountIsNull()
    {
        var legacy = new LegacyTrafficWeekInput
        {
            WeekNumber = 2,
            Deposits =
            [
                new TrafficDepositInput(100m, 200m, TrafficDepositStatus.Deposited)
            ]
        };

        var week = TrafficLegacyWeekAdapter.Adapt(legacy);

        week.Deposits.Should().ContainSingle();
        week.Deposits[0].DepositedAmount.Should().Be(200m);
    }

    [Fact]
    public void Adapt_ShouldNotAddLegacyDeposit_WhenDepositsAlreadyExist()
    {
        var legacy = new LegacyTrafficWeekInput
        {
            WeekNumber = 3,
            LegacyRequestedAmount = 999m,
            Deposits =
            [
                new TrafficDepositInput(500m, 500m, TrafficDepositStatus.Deposited)
            ]
        };

        var week = TrafficLegacyWeekAdapter.Adapt(legacy);

        week.Deposits.Should().ContainSingle();
        week.Deposits[0].RequestedAmount.Should().Be(500m);
    }

    [Fact]
    public void Adapt_ShouldPreserveChannelSpends()
    {
        var legacy = new LegacyTrafficWeekInput
        {
            WeekNumber = 1,
            LegacyRequestedAmount = 1000m,
            ChannelSpends =
            [
                new TrafficChannelSpendInput(TrafficMediaChannel.Telegram, 250m)
            ]
        };

        var week = TrafficLegacyWeekAdapter.Adapt(legacy);

        week.ChannelSpends.Should().ContainSingle();
        week.ChannelSpends[0].Amount.Should().Be(250m);
    }

    [Fact]
    public void AdaptedWeek_ShouldCalculateCorrectly_WithLegacyRequestedAmount()
    {
        var legacy = new LegacyTrafficWeekInput
        {
            WeekNumber = 1,
            LegacyRequestedAmount = 1000m
        };

        var week = TrafficLegacyWeekAdapter.Adapt(legacy);
        var result = TrafficInvestmentCalculator.CalculateWeek(week, monthlyTarget: 4000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestedAmount.Should().Be(1000m);
        result.Value.DepositedAmount.Should().Be(0m);
        // requested legado não entra no saldo; sem gasto, ainda falta a base semanal
        result.Value.SuggestedNext.Should().Be(1000m);
    }
}

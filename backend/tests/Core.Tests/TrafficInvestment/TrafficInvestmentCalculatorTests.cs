using Core.Domain.TrafficInvestmentCalculation;
using FluentAssertions;

namespace Core.Tests.TrafficInvestment;

public class TrafficInvestmentCalculatorTests
{
    [Fact]
    public void Constants_ShouldMatchRoadmap()
    {
        TrafficInvestmentConstants.TotalWeeks.Should().Be(4);
        TrafficInvestmentConstants.TaxRate.Should().Be(0.1215m);
    }

    [Fact]
    public void CalculateWeek_ShouldMatchRoadmapAcceptance_SuggestedNext1000()
    {
        // meta 4000 → base semanal 1000; sem depósito efetivo nem gasto → sugere 1000
        var week = new TrafficWeekInput { WeekNumber = 1 };

        var result = TrafficInvestmentCalculator.CalculateWeek(week, monthlyTarget: 4000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.SuggestedNext.Should().Be(1000m);
        result.Value.Balance.Should().Be(0m);
        result.Value.SpentAmount.Should().Be(0m);
        result.Value.TaxAmount.Should().Be(0m);
    }

    [Fact]
    public void CalculateWeek_ShouldReturnZeroSuggestedNext_WhenBalanceCoversWeeklyBase()
    {
        var week = new TrafficWeekInput
        {
            WeekNumber = 1,
            Deposits =
            [
                new TrafficDepositInput(RequestedAmount: 0m, DepositedAmount: 1500m, TrafficDepositStatus.Deposited)
            ]
        };

        var result = TrafficInvestmentCalculator.CalculateWeek(week, monthlyTarget: 4000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.SuggestedNext.Should().Be(0m);
    }

    [Fact]
    public void CalculateWeek_ShouldComputeTaxAndTotal_ForTelegram1000()
    {
        var week = new TrafficWeekInput
        {
            WeekNumber = 1,
            ChannelSpends =
            [
                new TrafficChannelSpendInput(TrafficMediaChannel.Telegram, 1000m)
            ]
        };

        var result = TrafficInvestmentCalculator.CalculateWeek(week, monthlyTarget: 4000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.SpentAmount.Should().Be(1000m);
        result.Value.TaxAmount.Should().Be(121.50m);
        result.Value.TotalAmount.Should().Be(1121.50m);
        result.Value.Balance.Should().Be(-1121.50m);
    }

    [Fact]
    public void CalculateWeek_ShouldSumAllChannels()
    {
        var week = new TrafficWeekInput
        {
            WeekNumber = 2,
            ChannelSpends =
            [
                new TrafficChannelSpendInput(TrafficMediaChannel.Telegram, 100m),
                new TrafficChannelSpendInput(TrafficMediaChannel.Instagram, 200m),
                new TrafficChannelSpendInput(TrafficMediaChannel.Story, 50m),
                new TrafficChannelSpendInput(TrafficMediaChannel.Direct, 75m),
                new TrafficChannelSpendInput(TrafficMediaChannel.Remarketing, 25m),
                new TrafficChannelSpendInput(TrafficMediaChannel.Other, 50m)
            ]
        };

        var result = TrafficInvestmentCalculator.CalculateWeek(week, monthlyTarget: 4000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.SpentAmount.Should().Be(500m);
        result.Value.TaxAmount.Should().Be(60.75m);
        result.Value.TotalAmount.Should().Be(560.75m);
    }

    [Fact]
    public void CalculateWeek_ShouldSumMultipleDeposits()
    {
        var week = new TrafficWeekInput
        {
            WeekNumber = 1,
            Deposits =
            [
                new TrafficDepositInput(RequestedAmount: 500m, DepositedAmount: 400m, TrafficDepositStatus.Deposited),
                new TrafficDepositInput(RequestedAmount: 300m, DepositedAmount: 600m, TrafficDepositStatus.Deposited)
            ]
        };

        var result = TrafficInvestmentCalculator.CalculateWeek(week, monthlyTarget: 4000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.DepositedAmount.Should().Be(1000m);
        result.Value.RequestedAmount.Should().Be(800m);
    }

    [Fact]
    public void CalculateWeek_ShouldUseDepositedAmountOnlyForBalance_RegardlessOfStatus()
    {
        var week = new TrafficWeekInput
        {
            WeekNumber = 1,
            Deposits =
            [
                new TrafficDepositInput(RequestedAmount: 1000m, DepositedAmount: 0m, TrafficDepositStatus.Requested),
                new TrafficDepositInput(RequestedAmount: 500m, DepositedAmount: 200m, TrafficDepositStatus.Pending)
            ]
        };

        var result = TrafficInvestmentCalculator.CalculateWeek(week, monthlyTarget: 4000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestedAmount.Should().Be(1500m);
        result.Value.DepositedAmount.Should().Be(200m);
        result.Value.Balance.Should().Be(200m);
    }

    [Fact]
    public void CalculateWeek_ShouldIncreaseSuggestedNext_WhenBalanceIsNegative()
    {
        var week = new TrafficWeekInput
        {
            WeekNumber = 1,
            Deposits =
            [
                new TrafficDepositInput(RequestedAmount: 0m, DepositedAmount: 500m, TrafficDepositStatus.Deposited)
            ],
            ChannelSpends =
            [
                new TrafficChannelSpendInput(TrafficMediaChannel.Telegram, 1000m)
            ]
        };

        var result = TrafficInvestmentCalculator.CalculateWeek(week, monthlyTarget: 4000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Balance.Should().Be(-621.50m);
        result.Value.SuggestedNext.Should().Be(1621.50m);
    }

    [Fact]
    public void CalculateWeek_ShouldReturnZeroSuggestedNext_WhenMonthlyTargetIsZero()
    {
        var week = new TrafficWeekInput
        {
            WeekNumber = 1,
            Deposits =
            [
                new TrafficDepositInput(RequestedAmount: 0m, DepositedAmount: 100m, TrafficDepositStatus.Deposited)
            ]
        };

        var result = TrafficInvestmentCalculator.CalculateWeek(week, monthlyTarget: 0m);

        result.IsSuccess.Should().BeTrue();
        result.Value.SuggestedNext.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldProcessAllWeeks()
    {
        var input = new TrafficInvestmentInput
        {
            MonthlyTarget = 4000m,
            Weeks =
            [
                new TrafficWeekInput { WeekNumber = 1 },
                new TrafficWeekInput
                {
                    WeekNumber = 2,
                    ChannelSpends =
                    [
                        new TrafficChannelSpendInput(TrafficMediaChannel.Telegram, 500m)
                    ]
                }
            ]
        };

        var result = TrafficInvestmentCalculator.Calculate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Weeks.Should().HaveCount(2);
        result.Value.Weeks[0].SuggestedNext.Should().Be(1000m);
        result.Value.Weeks[1].TaxAmount.Should().Be(60.75m);
    }

    [Fact]
    public void Calculate_ShouldFail_WhenMonthlyTargetIsNegative()
    {
        var input = new TrafficInvestmentInput
        {
            MonthlyTarget = -1m,
            Weeks = [new TrafficWeekInput { WeekNumber = 1 }]
        };

        var result = TrafficInvestmentCalculator.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.investment.negative_monthly_target");
    }

    [Fact]
    public void Calculate_ShouldFail_WhenChannelSpendIsNegative()
    {
        var input = new TrafficInvestmentInput
        {
            MonthlyTarget = 4000m,
            Weeks =
            [
                new TrafficWeekInput
                {
                    WeekNumber = 1,
                    ChannelSpends = [new TrafficChannelSpendInput(TrafficMediaChannel.Telegram, -1m)]
                }
            ]
        };

        var result = TrafficInvestmentCalculator.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.investment.negative_spend");
    }

    [Fact]
    public void Calculate_ShouldFail_WhenDepositAmountIsNegative()
    {
        var input = new TrafficInvestmentInput
        {
            MonthlyTarget = 4000m,
            Weeks =
            [
                new TrafficWeekInput
                {
                    WeekNumber = 1,
                    Deposits = [new TrafficDepositInput(-1m, 0m, TrafficDepositStatus.Pending)]
                }
            ]
        };

        var result = TrafficInvestmentCalculator.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.investment.negative_deposit");
    }

    [Fact]
    public void Calculate_ShouldFail_WhenWeekNumberIsOutOfRange()
    {
        var input = new TrafficInvestmentInput
        {
            MonthlyTarget = 4000m,
            Weeks = [new TrafficWeekInput { WeekNumber = 5 }]
        };

        var result = TrafficInvestmentCalculator.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.investment.invalid_week_number");
    }

    [Fact]
    public void Calculate_ShouldFail_WhenWeekNumbersAreDuplicated()
    {
        var input = new TrafficInvestmentInput
        {
            MonthlyTarget = 4000m,
            Weeks =
            [
                new TrafficWeekInput { WeekNumber = 1 },
                new TrafficWeekInput { WeekNumber = 1 }
            ]
        };

        var result = TrafficInvestmentCalculator.Calculate(input);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("traffic.investment.duplicate_week");
    }
}

using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class ManagementCommissionCalculatorTests
{
    [Fact]
    public void Calculate_ShouldMeetRoadmapAcceptance_WhenNetRevenue100kFactor50Pct2()
    {
        var level = CreateLevel(netRevenueFactor: 50m, netRevenuePctNoGoal: 2m);

        var commission = ManagementCommissionCalculator.Calculate(
            [new ManagementRevenueEntryInput(100_000m)],
            level,
            GoalTier.None);

        commission.Should().Be(1000m);
    }

    [Fact]
    public void Calculate_ShouldNotApplyDoubleDivision_WhenPctIsExplicitPercentPoints()
    {
        var level = CreateLevel(netRevenueFactor: 50m, netRevenuePctNoGoal: 2m);

        var commission = ManagementCommissionCalculator.Calculate(
            [new ManagementRevenueEntryInput(100_000m)],
            level,
            GoalTier.None);

        commission.Should().NotBe(10m);
        commission.Should().NotBe(0.01m);
    }

    [Fact]
    public void Calculate_ShouldUseDirectorFactor_WhenNetRevenueFactorIs100()
    {
        var level = CreateLevel(netRevenueFactor: 100m, netRevenuePctNoGoal: 2m);

        var commission = ManagementCommissionCalculator.Calculate(
            [new ManagementRevenueEntryInput(100_000m)],
            level,
            GoalTier.None);

        commission.Should().Be(2000m);
    }

    [Fact]
    public void Calculate_ShouldUseWithGoalPct_WhenGoalTierIsGoal()
    {
        var level = CreateLevel(
            netRevenueFactor: 50m,
            netRevenuePctNoGoal: 2m,
            netRevenuePctWithGoal: 1.5m);

        var commission = ManagementCommissionCalculator.Calculate(
            [new ManagementRevenueEntryInput(100_000m)],
            level,
            GoalTier.Goal);

        commission.Should().Be(750m);
    }

    [Fact]
    public void Calculate_ShouldSumMultipleRevenueEntries()
    {
        var level = CreateLevel(netRevenueFactor: 50m, netRevenuePctNoGoal: 2m);

        var commission = ManagementCommissionCalculator.Calculate(
            [
                new ManagementRevenueEntryInput(100_000m),
                new ManagementRevenueEntryInput(50_000m)
            ],
            level,
            GoalTier.None);

        commission.Should().Be(1500m);
    }

    [Fact]
    public void Calculate_ShouldRoundCommissionPerEntry_BeforeSumming()
    {
        var level = CreateLevel(netRevenueFactor: 50m, netRevenuePctNoGoal: 1.2m);

        var commission = ManagementCommissionCalculator.Calculate(
            [
                new ManagementRevenueEntryInput(33_333.33m),
                new ManagementRevenueEntryInput(33_333.33m)
            ],
            level,
            GoalTier.None);

        var perEntry = decimal.Round(33_333.33m * 0.5m * 0.012m, 2, MidpointRounding.AwayFromZero);
        commission.Should().Be(perEntry + perEntry);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoRevenueEntriesOrLevel()
    {
        var level = CreateLevel(netRevenueFactor: 50m, netRevenuePctNoGoal: 2m);

        ManagementCommissionCalculator.Calculate([], level, GoalTier.None)
            .Should().Be(0m);

        ManagementCommissionCalculator.Calculate(
                [new ManagementRevenueEntryInput(100_000m)],
                null,
                GoalTier.None)
            .Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNetRevenueFactorIsZero()
    {
        var level = CreateLevel(netRevenueFactor: 0m, netRevenuePctNoGoal: 2m);

        ManagementCommissionCalculator.Calculate(
                [new ManagementRevenueEntryInput(100_000m)],
                level,
                GoalTier.None)
            .Should().Be(0m);
    }

    private static CareerLevel CreateLevel(
        decimal netRevenueFactor,
        decimal netRevenuePctNoGoal,
        decimal netRevenuePctWithGoal = 0m) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Gerente",
            Profile = CalculationProfile.Management,
            NetRevenueFactor = netRevenueFactor,
            NetRevenuePctNoGoal = netRevenuePctNoGoal,
            NetRevenuePctWithGoal = netRevenuePctWithGoal
        };
}

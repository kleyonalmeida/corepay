using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class ManagementCommissionPercentSelectorTests
{
    [Fact]
    public void Select_ShouldReturnNoGoalPct_WhenGoalTierIsNone()
    {
        var level = CreateLevel(netRevenuePctNoGoal: 1.2m, netRevenuePctWithGoal: 1.5m);

        ManagementCommissionPercentSelector.Select(level, GoalTier.None)
            .Should().Be(1.2m);
    }

    [Theory]
    [InlineData(GoalTier.Goal)]
    [InlineData(GoalTier.SuperGoal)]
    public void Select_ShouldReturnWithGoalPct_WhenGoalTierIsGoalOrSuperGoal(GoalTier goalTier)
    {
        var level = CreateLevel(netRevenuePctNoGoal: 1.2m, netRevenuePctWithGoal: 1.5m);

        ManagementCommissionPercentSelector.Select(level, goalTier)
            .Should().Be(1.5m);
    }

    [Fact]
    public void Select_ShouldReturnZero_WhenCareerLevelIsNull()
    {
        ManagementCommissionPercentSelector.Select(null, GoalTier.None)
            .Should().Be(0m);
    }

    private static CareerLevel CreateLevel(
        decimal netRevenuePctNoGoal,
        decimal netRevenuePctWithGoal) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Gerente",
            Profile = CalculationProfile.Management,
            NetRevenueFactor = 50m,
            NetRevenuePctNoGoal = netRevenuePctNoGoal,
            NetRevenuePctWithGoal = netRevenuePctWithGoal
        };
}

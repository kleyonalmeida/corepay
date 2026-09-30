using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class ProjectLeaderCommissionPercentSelectorTests
{
    [Theory]
    [InlineData(GoalTier.None, 1.0)]
    [InlineData(GoalTier.Goal, 1.5)]
    [InlineData(GoalTier.SuperGoal, 2.0)]
    public void Select_ShouldReturnPctByGoalTier(GoalTier goalTier, decimal expected)
    {
        var level = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.ProjectLeader,
            CommissionWithoutGoalPct = 1.0m,
            CommissionWithGoalPct = 1.5m,
            CommissionWithSuperGoalPct = 2.0m
        };

        var pct = ProjectLeaderCommissionPercentSelector.Select(level, goalTier);

        pct.Should().Be(expected);
    }

    [Fact]
    public void Select_ShouldReturnZero_WhenCareerLevelIsNull()
    {
        var pct = ProjectLeaderCommissionPercentSelector.Select(null, GoalTier.Goal);

        pct.Should().Be(0m);
    }
}

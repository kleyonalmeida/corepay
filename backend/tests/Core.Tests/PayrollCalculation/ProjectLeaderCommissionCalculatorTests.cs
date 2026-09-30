using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class ProjectLeaderCommissionCalculatorTests
{
    [Fact]
    public void Calculate_ShouldMeetRoadmapAcceptance_WhenRevenueBelowThresholdWithoutGoal()
    {
        var level = CreateLevel(commissionWithoutGoalPct: 1m);
        var department = CreateDepartment();

        var commission = ProjectLeaderCommissionCalculator.Calculate(
            [new ProjectEntryInput(Guid.NewGuid(), 100_000m)],
            level,
            department,
            GoalTier.None);

        commission.Should().Be(1120m);
    }

    [Fact]
    public void Calculate_ShouldNotApplyLowRevenueBonus_WhenRevenueAtOrAboveThreshold()
    {
        var level = CreateLevel(commissionWithoutGoalPct: 1m);
        var department = CreateDepartment();

        var commission = ProjectLeaderCommissionCalculator.Calculate(
            [new ProjectEntryInput(Guid.NewGuid(), 300_000m)],
            level,
            department,
            GoalTier.None);

        commission.Should().Be(2400m);
    }

    [Fact]
    public void Calculate_ShouldNotApplyLowRevenueBonus_WhenRevenueEqualsThreshold()
    {
        var level = CreateLevel(commissionWithoutGoalPct: 1m);
        var department = CreateDepartment();

        var commission = ProjectLeaderCommissionCalculator.Calculate(
            [new ProjectEntryInput(Guid.NewGuid(), 200_000m)],
            level,
            department,
            GoalTier.None);

        commission.Should().Be(1600m);
    }

    [Fact]
    public void Calculate_ShouldCapFinalPercentAtTwoPointOne()
    {
        var level = CreateLevel(commissionWithoutGoalPct: 2m);
        var department = CreateDepartment();

        var commission = ProjectLeaderCommissionCalculator.Calculate(
            [new ProjectEntryInput(Guid.NewGuid(), 100_000m)],
            level,
            department,
            GoalTier.None);

        commission.Should().Be(1680m);
    }

    [Fact]
    public void Calculate_ShouldUseSuperGoalPct_WhenSuperGoalReached()
    {
        var level = CreateLevel(
            commissionWithoutGoalPct: 1m,
            commissionWithGoalPct: 1.5m,
            commissionWithSuperGoalPct: 2m);
        var department = CreateDepartment();

        var commission = ProjectLeaderCommissionCalculator.Calculate(
            [new ProjectEntryInput(Guid.NewGuid(), 300_000m)],
            level,
            department,
            GoalTier.SuperGoal);

        commission.Should().Be(4800m);
    }

    [Fact]
    public void Calculate_ShouldEvaluateLowRevenueBonusPerProject()
    {
        var level = CreateLevel(commissionWithoutGoalPct: 1m);
        var department = CreateDepartment();

        var commission = ProjectLeaderCommissionCalculator.Calculate(
            [
                new ProjectEntryInput(Guid.NewGuid(), 100_000m),
                new ProjectEntryInput(Guid.NewGuid(), 300_000m)
            ],
            level,
            department,
            GoalTier.None);

        commission.Should().Be(3520m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenNoProjectsOrLevel()
    {
        var department = CreateDepartment();

        ProjectLeaderCommissionCalculator.Calculate([], CreateLevel(), department, GoalTier.None)
            .Should().Be(0m);

        ProjectLeaderCommissionCalculator.Calculate(
                [new ProjectEntryInput(Guid.NewGuid(), 100_000m)],
                null,
                department,
                GoalTier.None)
            .Should().Be(0m);
    }

    private static Department CreateDepartment() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Líderes de Projetos",
            CalculationType = CalculationProfile.ProjectLeader,
            LowRevenueThreshold = 200_000m,
            LowRevenueBonusPct = 0.4m
        };

    private static CareerLevel CreateLevel(
        decimal commissionWithoutGoalPct = 1m,
        decimal commissionWithGoalPct = 1.5m,
        decimal commissionWithSuperGoalPct = 2m) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Líder",
            Profile = CalculationProfile.ProjectLeader,
            CommissionWithoutGoalPct = commissionWithoutGoalPct,
            CommissionWithGoalPct = commissionWithGoalPct,
            CommissionWithSuperGoalPct = commissionWithSuperGoalPct
        };
}

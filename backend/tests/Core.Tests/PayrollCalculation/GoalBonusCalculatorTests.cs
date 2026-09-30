using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class GoalBonusCalculatorTests
{
    private static readonly Guid FeiraId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OtherId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    [Fact]
    public void Calculate_ShouldReturnZero_WhenGoalNotReached()
    {
        var input = CreateInput(goalTier: GoalTier.None, goalBonusPercentage: 10m);

        var bonus = GoalBonusCalculator.Calculate(
            input,
            CalculationProfile.FixedBonus,
            3000m,
            []);

        bonus.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldUseProportionalFixed_ForFixedBonusProfile()
    {
        var input = CreateInput(goalTier: GoalTier.Goal, goalBonusPercentage: 10m);

        var bonus = GoalBonusCalculator.Calculate(
            input,
            CalculationProfile.FixedBonus,
            3000m,
            []);

        bonus.Should().Be(300m);
    }

    [Fact]
    public void Calculate_ShouldExcludeFeiraAllocation_ForAllocatedFixedProfile()
    {
        var input = CreateInput(goalTier: GoalTier.Goal, goalBonusPercentage: 10m);
        var allocations = new List<FixedAllocationCalculator.Allocation>
        {
            new(FeiraId, 1500m),
            new(OtherId, 1500m)
        };

        var bonus = GoalBonusCalculator.Calculate(
            input,
            CalculationProfile.AllocatedFixed,
            3000m,
            allocations);

        bonus.Should().Be(150m);
    }

    [Fact]
    public void Calculate_ShouldUseRevenue_ForTipsterWhenRevenuePositive()
    {
        var input = CreateInput(
            goalTier: GoalTier.Goal,
            goalBonusPercentage: 10m,
            projectEntries: [new ProjectEntryInput(Guid.NewGuid(), 5000m)]);

        var bonus = GoalBonusCalculator.Calculate(
            input,
            CalculationProfile.Tipster,
            2000m,
            []);

        bonus.Should().Be(500m);
    }

    [Fact]
    public void Calculate_ShouldFallbackToFixed_ForTipsterWhenRevenueZero()
    {
        var input = CreateInput(
            goalTier: GoalTier.Goal,
            goalBonusPercentage: 10m,
            projectEntries: [new ProjectEntryInput(Guid.NewGuid(), 0m, 15m)]);

        var bonus = GoalBonusCalculator.Calculate(
            input,
            CalculationProfile.Tipster,
            2000m,
            []);

        bonus.Should().Be(200m);
    }

    private static PayrollEntryInput CreateInput(
        GoalTier goalTier,
        decimal goalBonusPercentage,
        IReadOnlyList<ProjectEntryInput>? projectEntries = null) =>
        new()
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Setor Teste",
                CalculationType = CalculationProfile.AllocatedFixed,
                GoalBonusPercentage = goalBonusPercentage
            },
            Collaborator = new Collaborator
            {
                Id = Guid.NewGuid(),
                Name = "Colaborador",
                DepartmentId = Guid.NewGuid()
            },
            GoalTier = goalTier,
            ProjectEntries = projectEntries ?? [],
            ProjectSnapshots =
            [
                new ProjectCalculationSnapshot(FeiraId, ExcludesGoalBonus: true),
                new ProjectCalculationSnapshot(OtherId)
            ]
        };
}

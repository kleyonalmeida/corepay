using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class FixedBonusSectionCalculatorTests
{
    private static readonly Guid FeiraId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OtherId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    [Fact]
    public void Calculate_ShouldApplyGoalBonusOnFixed_ForFixedBonusProfile()
    {
        var input = CreateSectionInput(
            CalculationProfile.FixedBonus,
            baseSalary: 3000m,
            goalBonusPercentage: 10m,
            goalTier: GoalTier.Goal);

        var result = FixedBonusSectionCalculator.Calculate(input, CalculationProfile.FixedBonus);

        result.BaseSalary.Should().Be(3000m);
        result.GoalBonusAmount.Should().Be(300m);
        result.TotalAmount.Should().Be(3300m);
    }

    [Fact]
    public void Calculate_ShouldMeetAcceptanceCriteria_ForAdministrativeWithFeiraAndGoal()
    {
        var input = CreateSectionInput(
            CalculationProfile.AllocatedFixed,
            baseSalary: 3000m,
            goalBonusPercentage: 10m,
            goalTier: GoalTier.Goal,
            projectEntries:
            [
                new ProjectEntryInput(FeiraId, 0m),
                new ProjectEntryInput(OtherId, 0m)
            ],
            projectSnapshots:
            [
                new ProjectCalculationSnapshot(FeiraId, ExcludesGoalBonus: true),
                new ProjectCalculationSnapshot(OtherId)
            ]);

        var result = FixedBonusSectionCalculator.Calculate(input, CalculationProfile.AllocatedFixed);

        result.BaseSalary.Should().Be(3000m);
        result.GoalBonusAmount.Should().Be(150m);
        result.TotalAmount.Should().Be(3150m);
    }

    [Fact]
    public void Calculate_ShouldIncludeGroupCommissionAndGoalBonus_ForTipster()
    {
        var input = CreateSectionInput(
            CalculationProfile.Tipster,
            baseSalary: 2000m,
            goalBonusPercentage: 10m,
            goalTier: GoalTier.Goal,
            projectEntries:
            [
                new ProjectEntryInput(Guid.NewGuid(), 5000m, 40m)
            ],
            level: new CareerLevel
            {
                Id = Guid.NewGuid(),
                Name = "Tipster",
                Profile = CalculationProfile.Tipster,
                GroupCommissionPerPercent = 100m,
                GroupCommissionPer20Percent = 500m
            });

        var result = FixedBonusSectionCalculator.Calculate(input, CalculationProfile.Tipster);

        result.BaseSalary.Should().Be(2000m);
        result.GroupCommissionAmount.Should().Be(5000m);
        result.GoalBonusAmount.Should().Be(500m);
        result.TotalAmount.Should().Be(7500m);
    }

    [Fact]
    public void Calculate_ShouldApplyProportionalFactor_ToBaseOnly()
    {
        var collaborator = CreateCollaborator();
        collaborator.AdmissionDate = new DateOnly(2025, 3, 16);

        var input = CreateSectionInput(
            CalculationProfile.FixedBonus,
            baseSalary: 3100m,
            goalBonusPercentage: 0m,
            goalTier: GoalTier.None,
            collaborator: collaborator);

        var expectedBase = decimal.Round(3100m * (16m / 31m), 2, MidpointRounding.AwayFromZero);
        var result = FixedBonusSectionCalculator.Calculate(input, CalculationProfile.FixedBonus);

        result.BaseSalary.Should().Be(expectedBase);
        result.TotalAmount.Should().Be(expectedBase);
    }

    [Fact]
    public void Calculate_ShouldAddBonusesAndSubtractDeductions()
    {
        var input = CreateSectionInput(
            CalculationProfile.FixedBonus,
            baseSalary: 1000m,
            goalBonusPercentage: 0m,
            goalTier: GoalTier.None,
            bonuses: [100m],
            deductions: [50m]);

        var result = FixedBonusSectionCalculator.Calculate(input, CalculationProfile.FixedBonus);

        result.TotalAmount.Should().Be(1050m);
    }

    private static PayrollEntryInput CreateSectionInput(
        CalculationProfile profile,
        decimal baseSalary,
        decimal goalBonusPercentage,
        GoalTier goalTier,
        IReadOnlyList<ProjectEntryInput>? projectEntries = null,
        IReadOnlyList<ProjectCalculationSnapshot>? projectSnapshots = null,
        CareerLevel? level = null,
        Collaborator? collaborator = null,
        decimal[]? bonuses = null,
        decimal[]? deductions = null) =>
        new()
        {
            Month = 3,
            Year = 2025,
            Department = new Department
            {
                Id = Guid.NewGuid(),
                Name = "Setor Teste",
                CalculationType = profile,
                GoalBonusPercentage = goalBonusPercentage,
                IsAllocatedFixed = profile == CalculationProfile.AllocatedFixed
            },
            CareerLevel = level ?? new CareerLevel
            {
                Id = Guid.NewGuid(),
                Name = "Nível",
                Profile = profile,
                BaseSalary = baseSalary
            },
            Collaborator = collaborator ?? CreateCollaborator(),
            FullBaseSalary = baseSalary,
            GoalTier = goalTier,
            ProjectEntries = projectEntries ?? [],
            ProjectSnapshots = projectSnapshots ?? [],
            BonusEntries = (bonuses ?? []).Select(value => new BonusEntryInput(null, value)).ToList(),
            DeductionEntries = (deductions ?? []).Select(value => new DeductionEntryInput(value)).ToList()
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador",
            DepartmentId = Guid.NewGuid()
        };
}

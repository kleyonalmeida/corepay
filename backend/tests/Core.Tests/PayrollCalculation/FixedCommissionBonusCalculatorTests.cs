using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class FixedCommissionBonusCalculatorTests
{
    private const int March = 3;
    private const int Year = 2025;

    [Fact]
    public void Calculate_ShouldAddGoalBonusValue_WhenSuperGoal()
    {
        var level = CreateLevel();
        level.BaseSalary = 2000m;
        level.CommissionWithoutGoalPct = 2m;
        level.CommissionWithGoalPct = 2m;
        level.GoalBonusValue = 500m;

        var input = CreateInput(
            goalTier: GoalTier.SuperGoal,
            level: level,
            projects: [50_000m]);

        var result = FixedCommissionBonusCalculator.Calculate(input);

        result.TotalAmount.Should().Be(3500m);
    }

    [Fact]
    public void Calculate_ShouldAddGoalBonusValue_WhenGoalReached()
    {
        var level = CreateLevel();
        level.BaseSalary = 2000m;
        level.CommissionWithoutGoalPct = 2m;
        level.CommissionWithGoalPct = 2m;
        level.GoalBonusValue = 500m;

        var input = CreateInput(
            goalTier: GoalTier.Goal,
            level: level,
            projects: [50_000m]);

        var result = FixedCommissionBonusCalculator.Calculate(input);

        result.BaseSalary.Should().Be(2000m);
        result.CommissionAmount.Should().Be(1000m);
        result.TotalAmount.Should().Be(3500m);
    }

    [Fact]
    public void Calculate_ShouldIgnoreBaseAndCommission_WhenFinalSalaryIsPositive()
    {
        var level = CreateLevel();
        level.BaseSalary = 2000m;
        level.CommissionWithoutGoalPct = 2m;
        level.GoalBonusValue = 500m;

        var input = CreateInput(
            goalTier: GoalTier.Goal,
            level: level,
            finalSalary: 4500m,
            projects: [100_000m],
            bonuses: [200m],
            deductions: [100m]);

        var result = FixedCommissionBonusCalculator.Calculate(input);

        result.BaseSalary.Should().Be(4500m);
        result.CommissionAmount.Should().Be(0m);
        result.TotalAmount.Should().Be(4600m);
    }

    [Fact]
    public void Calculate_ShouldRecalculateNormally_WhenFinalSalaryIsZeroOrNull()
    {
        var level = CreateLevel();
        level.BaseSalary = 2000m;
        level.CommissionWithoutGoalPct = 2m;
        level.GoalBonusValue = 500m;

        var withNull = FixedCommissionBonusCalculator.Calculate(CreateInput(
            goalTier: GoalTier.None,
            level: level,
            finalSalary: null,
            projects: [10_000m]));

        var withZero = FixedCommissionBonusCalculator.Calculate(CreateInput(
            goalTier: GoalTier.None,
            level: level,
            finalSalary: 0m,
            projects: [10_000m]));

        withNull.TotalAmount.Should().Be(2200m);
        withZero.TotalAmount.Should().Be(2200m);
    }

    [Fact]
    public void Calculate_ShouldNotAddGoalBonus_WhenGoalNotReached()
    {
        var level = CreateLevel();
        level.BaseSalary = 2000m;
        level.CommissionWithoutGoalPct = 2m;
        level.GoalBonusValue = 500m;

        var input = CreateInput(
            goalTier: GoalTier.None,
            level: level,
            projects: [10_000m]);

        var result = FixedCommissionBonusCalculator.Calculate(input);

        result.TotalAmount.Should().Be(2200m);
    }

    [Fact]
    public void Calculate_ShouldApplyProportionalBase_WhenNoFinalSalary()
    {
        var level = CreateLevel();
        level.BaseSalary = 3100m;
        level.CommissionWithoutGoalPct = 2m;

        var collaborator = CreateCollaborator();
        collaborator.AdmissionDate = new DateOnly(Year, March, 16);

        var input = CreateInput(
            goalTier: GoalTier.None,
            level: level,
            projects: [10_000m],
            collaborator: collaborator);

        var expectedBase = decimal.Round(3100m * (16m / 31m), 2, MidpointRounding.AwayFromZero);

        var result = FixedCommissionBonusCalculator.Calculate(input);

        result.BaseSalary.Should().Be(expectedBase);
        result.TotalAmount.Should().Be(expectedBase + 200m);
    }

    [Fact]
    public void Calculate_ShouldAddBonusesAndSubtractDeductions_InNormalPath()
    {
        var level = CreateLevel();
        level.BaseSalary = 1000m;
        level.CommissionWithoutGoalPct = 2m;
        level.CommissionWithGoalPct = 2m;
        level.GoalBonusValue = 500m;

        var input = CreateInput(
            goalTier: GoalTier.Goal,
            level: level,
            projects: [10_000m],
            bonuses: [100m],
            deductions: [50m]);

        var result = FixedCommissionBonusCalculator.Calculate(input);

        result.TotalAmount.Should().Be(1750m);
    }

    private static PayrollEntryInput CreateInput(
        GoalTier goalTier,
        CareerLevel level,
        decimal? finalSalary = null,
        decimal[]? projects = null,
        decimal[]? bonuses = null,
        decimal[]? deductions = null,
        Collaborator? collaborator = null) =>
        new()
        {
            Month = March,
            Year = Year,
            Department = CreateDepartment(CalculationProfile.FixedCommissionBonus),
            CareerLevel = level,
            Collaborator = collaborator ?? CreateCollaborator(),
            GoalTier = goalTier,
            FinalSalary = finalSalary,
            ProjectEntries = (projects ?? []).Select(value =>
                new ProjectEntryInput(Guid.NewGuid(), value)).ToList(),
            BonusEntries = (bonuses ?? []).Select(value =>
                new BonusEntryInput(null, value)).ToList(),
            DeductionEntries = (deductions ?? []).Select(value =>
                new DeductionEntryInput(value)).ToList()
        };

    private static Department CreateDepartment(CalculationProfile profile) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Affiliates Renomeado",
            CalculationType = profile
        };

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Affiliate Renomeado",
            Profile = CalculationProfile.FixedCommissionBonus
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Affiliates",
            DepartmentId = Guid.NewGuid()
        };
}

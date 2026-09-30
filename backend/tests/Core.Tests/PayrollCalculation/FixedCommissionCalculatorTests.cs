using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class FixedCommissionCalculatorTests
{
    private const int March = 3;
    private const int Year = 2025;

    [Fact]
    public void Calculate_ShouldApplyProportionalBaseAndFullCommission()
    {
        var level = CreateLevel();
        level.BaseSalary = 3100m;
        level.CommissionWithGoalPct = 3m;

        var input = CreateInput(
            goalTier: GoalTier.Goal,
            level: level,
            baseSalary: 3100m,
            projects: [100_000m]);

        var result = FixedCommissionCalculator.Calculate(input);

        result.BaseSalary.Should().Be(3100m);
        result.CommissionAmount.Should().Be(3000m);
        result.TotalAmount.Should().Be(6100m);
    }

    [Fact]
    public void Calculate_ShouldApplyProportionalFactor_ToBaseOnly()
    {
        var level = CreateLevel();
        level.BaseSalary = 3100m;
        level.CommissionWithGoalPct = 3m;

        var collaborator = CreateCollaborator();
        collaborator.AdmissionDate = new DateOnly(Year, March, 16);

        var input = CreateInput(
            goalTier: GoalTier.Goal,
            level: level,
            baseSalary: 3100m,
            projects: [100_000m],
            collaborator: collaborator);

        var expectedBase = decimal.Round(3100m * (16m / 31m), 2, MidpointRounding.AwayFromZero);

        var result = FixedCommissionCalculator.Calculate(input);

        result.BaseSalary.Should().Be(expectedBase);
        result.CommissionAmount.Should().Be(3000m);
        result.TotalAmount.Should().Be(expectedBase + 3000m);
    }

    [Fact]
    public void Calculate_ShouldSelectPctByGoalReached()
    {
        var level = CreateLevel();
        level.BaseSalary = 1000m;
        level.CommissionWithoutGoalPct = 2m;
        level.CommissionWithGoalPct = 2.5m;

        var withoutGoal = FixedCommissionCalculator.Calculate(CreateInput(
            goalTier: GoalTier.None,
            level: level,
            projects: [10_000m]));

        var withGoal = FixedCommissionCalculator.Calculate(CreateInput(
            goalTier: GoalTier.Goal,
            level: level,
            projects: [10_000m]));

        withoutGoal.CommissionAmount.Should().Be(200m);
        withGoal.CommissionAmount.Should().Be(250m);
    }

    [Fact]
    public void Calculate_ShouldTreatSuperGoal_AsWithGoalPct()
    {
        var level = CreateLevel();
        level.BaseSalary = 1000m;
        level.CommissionWithoutGoalPct = 2m;
        level.CommissionWithGoalPct = 2.5m;
        level.CommissionWithSuperGoalPct = 99m;

        var withSuperGoal = FixedCommissionCalculator.Calculate(CreateInput(
            goalTier: GoalTier.SuperGoal,
            level: level,
            projects: [10_000m]));

        withSuperGoal.CommissionAmount.Should().Be(250m);
    }

    [Fact]
    public void Calculate_ShouldPreferFullBaseSalarySnapshot_OverLevel()
    {
        var level = CreateLevel();
        level.BaseSalary = 3000m;

        var input = CreateInput(
            goalTier: GoalTier.None,
            level: level,
            baseSalary: 3500m,
            projects: []);

        var result = FixedCommissionCalculator.Calculate(input);

        result.BaseSalary.Should().Be(3500m);
    }

    [Fact]
    public void Calculate_ShouldAddBonusesAndSubtractDeductions()
    {
        var level = CreateLevel();
        level.BaseSalary = 1000m;
        level.CommissionWithoutGoalPct = 2m;

        var input = CreateInput(
            goalTier: GoalTier.None,
            level: level,
            projects: [10_000m],
            bonuses: [100m],
            deductions: [50m]);

        var result = FixedCommissionCalculator.Calculate(input);

        result.TotalAmount.Should().Be(1250m);
    }

    private static PayrollEntryInput CreateInput(
        GoalTier goalTier,
        CareerLevel level,
        decimal? baseSalary = null,
        decimal[]? projects = null,
        decimal[]? bonuses = null,
        decimal[]? deductions = null,
        Collaborator? collaborator = null) =>
        new()
        {
            Month = March,
            Year = Year,
            Department = CreateDepartment(CalculationProfile.FixedCommission),
            CareerLevel = level,
            Collaborator = collaborator ?? CreateCollaborator(),
            FullBaseSalary = baseSalary,
            GoalTier = goalTier,
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
            Name = "Setor Teste",
            CalculationType = profile
        };

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Nível Teste",
            Profile = CalculationProfile.FixedCommission
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Teste",
            DepartmentId = Guid.NewGuid()
        };
}

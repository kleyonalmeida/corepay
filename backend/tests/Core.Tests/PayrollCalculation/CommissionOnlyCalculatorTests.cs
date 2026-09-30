using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class CommissionOnlyCalculatorTests
{
    private const int March = 3;
    private const int Year = 2025;

    [Fact]
    public void Calculate_ShouldApplyCommissionWithoutGoal_OnRevenueSum()
    {
        var input = CreateInput(
            goalTier: GoalTier.None,
            commissionWithoutGoalPct: 2m,
            commissionWithGoalPct: 3m,
            projects: [30_000m, 70_000m]);

        var result = CommissionOnlyCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(2000m);
        result.BaseSalary.Should().Be(0m);
        result.TotalAmount.Should().Be(2000m);
    }

    [Fact]
    public void Calculate_ShouldUseCommissionWithGoal_WhenGoalReached()
    {
        var input = CreateInput(
            goalTier: GoalTier.Goal,
            commissionWithoutGoalPct: 2m,
            commissionWithGoalPct: 3m,
            projects: [100_000m]);

        var result = CommissionOnlyCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(3000m);
        result.TotalAmount.Should().Be(3000m);
    }

    [Fact]
    public void Calculate_ShouldIgnoreProportionalFactor_EvenWithMidMonthAdmission()
    {
        var collaborator = CreateCollaborator();
        collaborator.AdmissionDate = new DateOnly(Year, March, 16);

        var input = CreateInput(
            goalTier: GoalTier.None,
            commissionWithoutGoalPct: 2m,
            commissionWithGoalPct: 3m,
            projects: [50_000m],
            collaborator: collaborator);

        var result = CommissionOnlyCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(1000m);
        result.TotalAmount.Should().Be(1000m);
    }

    [Fact]
    public void Calculate_ShouldAddBonusesAndSubtractDeductions()
    {
        var input = CreateInput(
            goalTier: GoalTier.None,
            commissionWithoutGoalPct: 2m,
            commissionWithGoalPct: 3m,
            projects: [10_000m],
            bonuses: [100m],
            deductions: [50m]);

        var result = CommissionOnlyCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(200m);
        result.TotalAmount.Should().Be(250m);
    }

    [Fact]
    public void Calculate_ShouldNotUseSuperGoalPct()
    {
        var level = CreateLevel(CalculationProfile.CommissionOnly);
        level.CommissionWithoutGoalPct = 2m;
        level.CommissionWithGoalPct = 3m;
        level.CommissionWithSuperGoalPct = 10m;

        var input = CreateInput(
            goalTier: GoalTier.Goal,
            level: level,
            projects: [100_000m]);

        var result = CommissionOnlyCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(3000m);
    }

    private static PayrollEntryInput CreateInput(
        GoalTier goalTier,
        decimal commissionWithoutGoalPct = 2m,
        decimal commissionWithGoalPct = 3m,
        decimal[]? projects = null,
        decimal[]? bonuses = null,
        decimal[]? deductions = null,
        CareerLevel? level = null,
        Collaborator? collaborator = null) =>
        new()
        {
            Month = March,
            Year = Year,
            Department = CreateDepartment(CalculationProfile.CommissionOnly),
            CareerLevel = level ?? CreateLevel(
                CalculationProfile.CommissionOnly,
                commissionWithoutGoalPct,
                commissionWithGoalPct),
            Collaborator = collaborator ?? CreateCollaborator(),
            GoalTier = goalTier,
            ProjectEntries = (projects ?? []).Select((value, index) =>
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

    private static CareerLevel CreateLevel(
        CalculationProfile profile,
        decimal commissionWithoutGoalPct = 2m,
        decimal commissionWithGoalPct = 3m) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Nível Teste",
            Profile = profile,
            CommissionWithoutGoalPct = commissionWithoutGoalPct,
            CommissionWithGoalPct = commissionWithGoalPct
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Teste",
            DepartmentId = Guid.NewGuid()
        };
}

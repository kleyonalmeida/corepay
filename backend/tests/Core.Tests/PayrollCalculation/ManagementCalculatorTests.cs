using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class ManagementCalculatorTests
{
    private const int March = 3;
    private const int Year = 2025;

    [Fact]
    public void Calculate_ShouldApplyProportionalBaseAndCommission()
    {
        var input = CreateInput(
            goalTier: GoalTier.None,
            baseSalary: 5000m,
            revenueEntries: [100_000m]);

        var result = ManagementCalculator.Calculate(input);

        result.BaseSalary.Should().Be(5000m);
        result.CommissionAmount.Should().Be(1000m);
        result.TotalAmount.Should().Be(6000m);
    }

    [Fact]
    public void Calculate_ShouldApplyProportionalFactor_ToBaseOnly()
    {
        var collaborator = CreateCollaborator();
        collaborator.AdmissionDate = new DateOnly(Year, March, 16);

        var input = CreateInput(
            goalTier: GoalTier.None,
            baseSalary: 3100m,
            revenueEntries: [100_000m],
            collaborator: collaborator);

        var expectedBase = decimal.Round(3100m * (16m / 31m), 2, MidpointRounding.AwayFromZero);

        var result = ManagementCalculator.Calculate(input);

        result.BaseSalary.Should().Be(expectedBase);
        result.CommissionAmount.Should().Be(1000m);
        result.TotalAmount.Should().Be(expectedBase + 1000m);
    }

    [Fact]
    public void Calculate_ShouldAddBonusesAndSubtractDeductions()
    {
        var input = CreateInput(
            goalTier: GoalTier.None,
            baseSalary: 1000m,
            revenueEntries: [100_000m],
            bonuses: [100m],
            deductions: [50m]);

        var result = ManagementCalculator.Calculate(input);

        result.TotalAmount.Should().Be(2050m);
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
            revenueEntries: []);

        var result = ManagementCalculator.Calculate(input);

        result.BaseSalary.Should().Be(3500m);
        result.CommissionAmount.Should().Be(0m);
        result.TotalAmount.Should().Be(3500m);
    }

    private static PayrollEntryInput CreateInput(
        GoalTier goalTier,
        CareerLevel? level = null,
        decimal? baseSalary = null,
        decimal[]? revenueEntries = null,
        decimal[]? bonuses = null,
        decimal[]? deductions = null,
        Collaborator? collaborator = null) =>
        new()
        {
            Month = March,
            Year = Year,
            Department = CreateDepartment(),
            CareerLevel = level ?? CreateLevel(),
            Collaborator = collaborator ?? CreateCollaborator(),
            FullBaseSalary = baseSalary,
            GoalTier = goalTier,
            ManagementRevenueEntries = (revenueEntries ?? []).Select(value =>
                new ManagementRevenueEntryInput(value)).ToList(),
            BonusEntries = (bonuses ?? []).Select(value =>
                new BonusEntryInput(null, value)).ToList(),
            DeductionEntries = (deductions ?? []).Select(value =>
                new DeductionEntryInput(value)).ToList()
        };

    private static Department CreateDepartment() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Gerência",
            CalculationType = CalculationProfile.Management
        };

    private static CareerLevel CreateLevel()
    {
        return new CareerLevel
        {
            Id = Guid.NewGuid(),
            Name = "Gerente",
            Profile = CalculationProfile.Management,
            NetRevenueFactor = 50m,
            NetRevenuePctNoGoal = 2m,
            NetRevenuePctWithGoal = 1.5m
        };
    }

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Colaborador Gerência",
            DepartmentId = Guid.NewGuid()
        };
}

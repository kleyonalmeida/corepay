using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class CommercialSupervisorCalculatorTests
{
    private const int March = 3;
    private const int Year = 2025;

    [Fact]
    public void Calculate_ShouldApplyProportionalBaseAndCommissions()
    {
        var input = CreateInput(
            baseSalary: 3000m,
            supervisorAnalystRevenue: 200m,
            projectEntries:
            [
                new SupervisorProjectEntryInput(
                    Guid.NewGuid(), 10, 3, 100_000m, 1000m, false, true)
            ]);

        var result = CommercialSupervisorCalculator.Calculate(input);

        result.BaseSalary.Should().Be(3000m);
        result.CommissionAmount.Should().Be(914.1m + 200m);
        result.TotalAmount.Should().Be(3000m + 914.1m + 200m);
    }

    [Fact]
    public void Calculate_ShouldApplyProportionalFactor_ToBaseOnly()
    {
        var collaborator = CreateCollaborator();
        collaborator.AdmissionDate = new DateOnly(Year, March, 16);

        var input = CreateInput(
            baseSalary: 3100m,
            collaborator: collaborator,
            projectEntries: []);

        var expectedBase = decimal.Round(3100m * (16m / 31m), 2, MidpointRounding.AwayFromZero);

        var result = CommercialSupervisorCalculator.Calculate(input);

        result.BaseSalary.Should().Be(expectedBase);
        result.TotalAmount.Should().Be(expectedBase);
    }

    [Fact]
    public void Calculate_ShouldAddBonusesAndSubtractDeductions_WithoutAffectingCommissionAmount()
    {
        var input = CreateInput(
            baseSalary: 2000m,
            supervisorAnalystRevenue: 100m,
            projectEntries:
            [
                new SupervisorProjectEntryInput(
                    Guid.NewGuid(), 0, 0, 0m, 0m, false, false)
            ],
            bonuses: [100m],
            deductions: [50m]);

        var result = CommercialSupervisorCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(100m);
        result.TotalAmount.Should().Be(2000m + 100m + 100m - 50m);
    }

    [Fact]
    public void Calculate_ShouldReturnFixedPlusRevOnly_WhenNoProjectCommissions()
    {
        var input = CreateInput(
            baseSalary: 2500m,
            supervisorAnalystRevenue: 300m,
            projectEntries: []);

        var result = CommercialSupervisorCalculator.Calculate(input);

        result.BaseSalary.Should().Be(2500m);
        result.CommissionAmount.Should().Be(300m);
        result.TotalAmount.Should().Be(2800m);
    }

    [Fact]
    public void Calculate_ShouldPreferFullBaseSalarySnapshot_OverLevel()
    {
        var level = CreateLevel();
        level.BaseSalary = 2000m;

        var input = CreateInput(
            level: level,
            baseSalary: 3500m,
            projectEntries: []);

        var result = CommercialSupervisorCalculator.Calculate(input);

        result.BaseSalary.Should().Be(3500m);
    }

    private static PayrollEntryInput CreateInput(
        CareerLevel? level = null,
        decimal? baseSalary = null,
        decimal supervisorAnalystRevenue = 0m,
        IReadOnlyList<SupervisorProjectEntryInput>? projectEntries = null,
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
            SupervisorAnalystRevenue = supervisorAnalystRevenue,
            SupervisorProjectEntries = projectEntries ?? [],
            BonusEntries = (bonuses ?? []).Select(value =>
                new BonusEntryInput(null, value)).ToList(),
            DeductionEntries = (deductions ?? []).Select(value =>
                new DeductionEntryInput(value)).ToList()
        };

    private static Department CreateDepartment() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Comercial",
            CalculationType = CalculationProfile.CommercialAnalyst
        };

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Supervisor",
            Profile = CalculationProfile.CommercialSupervisor,
            BaseSalary = 3000m,
            SupFtdSuperbetNoGoal = 4m,
            SupFtdSuperbetWithGoal = 5m,
            SupFtdOtherNoGoal = 0.3m,
            SupFtdOtherWithGoal = 0.5m,
            SupSalesPctNoGoal = 0.5m,
            SupSalesPctWithGoal = 0.8m,
            SupRevPct = 10m
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Supervisor Comercial",
            DepartmentId = Guid.NewGuid()
        };
}

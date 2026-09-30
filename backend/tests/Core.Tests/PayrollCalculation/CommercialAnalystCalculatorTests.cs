using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class CommercialAnalystCalculatorTests
{
    private const int March = 3;
    private const int Year = 2025;

    [Fact]
    public void Calculate_ShouldApplyDefaultScenario_OneProjectZeroGoals()
    {
        var input = CreateInput(
            baseSalary: 1500m,
            projectEntries:
            [
                new CommercialAnalystProjectEntryInput(
                    Guid.NewGuid(),
                    ProjectPlatform.Lastlink,
                    FtdTotal: 100,
                    FtdSuperbet: 10,
                    false, false, 0,
                    SalesAmount: 10_000m,
                    false, false,
                    Rev: 0m)
            ]);

        var result = CommercialAnalystCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(630m);
        result.BaseSalary.Should().Be(870m);
        result.PlatformTotal.Should().Be(400m);
        result.TotalAmount.Should().Be(630m + 870m + 400m);
    }

    [Fact]
    public void Calculate_ShouldSetComplement500_WhenCommission1000AndMinimum1500()
    {
        var level = CreateLevelWithoutSalesBonus();
        var input = CreateInput(
            level: level,
            baseSalary: 1500m,
            projectEntries:
            [
                CreateProjectWithCommissionProxy(sales: 25_000m)
            ]);

        var result = CommercialAnalystCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(1000m);
        result.BaseSalary.Should().Be(500m);
        result.PlatformTotal.Should().Be(1000m);
        result.TotalAmount.Should().Be(1000m + 500m + 1000m);
    }

    [Fact]
    public void Calculate_ShouldNotAddPlatformToTotal_WhenComplementIsZero()
    {
        var level = CreateLevelWithoutSalesBonus();
        var input = CreateInput(
            level: level,
            baseSalary: 1500m,
            projectEntries:
            [
                CreateProjectWithCommissionProxy(sales: 50_000m)
            ]);

        var result = CommercialAnalystCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(2000m);
        result.BaseSalary.Should().Be(0m);
        result.PlatformTotal.Should().Be(2000m);
        result.TotalAmount.Should().Be(2000m);
    }

    [Fact]
    public void Calculate_ShouldExcludeManualBonusFromMinimumComparison()
    {
        var level = CreateLevelWithoutSalesBonus();
        var input = CreateInput(
            level: level,
            baseSalary: 1500m,
            projectEntries:
            [
                CreateProjectWithCommissionProxy(sales: 25_000m)
            ],
            bonuses: [500m]);

        var result = CommercialAnalystCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(1000m);
        result.BaseSalary.Should().Be(500m);
        result.TotalAmount.Should().Be(1000m + 500m + 500m + 1000m);
    }

    [Fact]
    public void Calculate_ShouldIncludeBetanoInternaInMinimum_NotMundoBet()
    {
        var level = CreateLevelWithoutSalesBonus();
        level.BetanoInternaValue = 200m;
        level.BetanoMundoBetValue = 70m;

        var input = CreateInput(
            level: level,
            baseSalary: 1500m,
            betanoInternaCount: 1,
            betanoMundoBetCount: 2,
            projectEntries:
            [
                CreateProjectWithCommissionProxy(sales: 25_000m)
            ]);

        var result = CommercialAnalystCalculator.Calculate(input);

        result.CommissionAmount.Should().Be(1200m); // 1000 + 200 interna
        result.BaseSalary.Should().Be(300m);
        result.TotalAmount.Should().Be(1200m + 300m + 1000m + 140m);
    }

    [Fact]
    public void Calculate_ShouldApplyProportionalMinimum()
    {
        var collaborator = CreateCollaborator();
        collaborator.AdmissionDate = new DateOnly(Year, March, 16);

        var input = CreateInput(
            baseSalary: 3100m,
            collaborator: collaborator,
            projectEntries: []);

        var expectedMinimum = decimal.Round(3100m * (16m / 31m), 2, MidpointRounding.AwayFromZero);

        var result = CommercialAnalystCalculator.Calculate(input);

        result.BaseSalary.Should().Be(expectedMinimum);
        result.TotalAmount.Should().Be(expectedMinimum);
    }

    [Fact]
    public void Calculate_ShouldSubtractDeductions()
    {
        var level = CreateLevelWithoutSalesBonus();
        var input = CreateInput(
            level: level,
            baseSalary: 1500m,
            projectEntries:
            [
                CreateProjectWithCommissionProxy(sales: 25_000m)
            ],
            deductions: [100m]);

        var result = CommercialAnalystCalculator.Calculate(input);

        result.TotalAmount.Should().Be(1000m + 500m + 1000m - 100m);
    }

    private static CommercialAnalystProjectEntryInput CreateProjectWithCommissionProxy(decimal sales) =>
        new(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            FtdTotal: 0,
            FtdSuperbet: 0,
            false, false, 0,
            SalesAmount: sales,
            false, false,
            Rev: 0m);

    private static PayrollEntryInput CreateInput(
        CareerLevel? level = null,
        decimal? baseSalary = null,
        IReadOnlyList<CommercialAnalystProjectEntryInput>? projectEntries = null,
        int betanoInternaCount = 0,
        int betanoMundoBetCount = 0,
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
            CommercialProjectEntries = projectEntries ?? [],
            BetanoInternaCount = betanoInternaCount,
            BetanoMundoBetCount = betanoMundoBetCount,
            BonusEntries = (bonuses ?? []).Select(v => new BonusEntryInput(null, v)).ToList(),
            DeductionEntries = (deductions ?? []).Select(v => new DeductionEntryInput(v)).ToList()
        };

    private static Department CreateDepartment() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Comercial",
            CalculationType = CalculationProfile.CommercialAnalyst
        };

    private static CareerLevel CreateLevelWithoutSalesBonus()
    {
        var level = CreateLevel();
        level.SalesBonusEvery = 0m;
        return level;
    }

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Analista Comercial Júnior",
            Profile = CalculationProfile.CommercialAnalyst,
            BaseSalary = 1500m,
            FtdRateBase = 2m,
            FtdRateWithGoal = 2.5m,
            FtdRateWithSuperGoal = 3m,
            FtdSuperbetRate = 5m,
            FtdBonusEvery = 250,
            FtdBonusValue = 350m,
            DefaultCpaValue = 0m,
            SalesPctBase = 4m,
            SalesPctWithGoal = 5m,
            SalesPctWithSuperGoal = 6m,
            SalesBonusEvery = 20_000m,
            SalesBonusValue = 250m,
            RevPct = 1m,
            BetanoInternaValue = 200m,
            BetanoMundoBetValue = 70m
        };

    private static Collaborator CreateCollaborator() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Ana Comercial",
            DepartmentId = Guid.NewGuid()
        };
}

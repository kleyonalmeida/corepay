using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class CommercialAnalystProjectCommissionCalculatorTests
{
    [Fact]
    public void CalculateProject_ShouldApplyCpaCount()
    {
        var level = CreateLevel();
        level.DefaultCpaValue = 35m;
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            FtdTotal: 0,
            FtdSuperbet: 0,
            false, false,
            CpaCount: 4,
            SalesAmount: 0m,
            false, false,
            Rev: 0m);

        var result = CommercialAnalystProjectCommissionCalculator.CalculateProject(entry, level);

        result.Commission.Should().Be(140m);
    }

    [Fact]
    public void CalculateProject_ShouldUseTwoGoalTier_WhenBothGoalsReached()
    {
        var level = CreateLevel();
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            FtdTotal: 100,
            FtdSuperbet: 0,
            IsFtdGoalReached: true,
            IsProjectFtdGoalReached: true,
            CpaCount: 0,
            SalesAmount: 10_000m,
            IsSalesGoalReached: true,
            IsProjectSalesGoalReached: true,
            Rev: 0m);

        var result = CommercialAnalystProjectCommissionCalculator.CalculateProject(entry, level);

        // 100*3 + 10000*6% = 300 + 600 = 900
        result.Commission.Should().Be(900m);
    }

    [Fact]
    public void CalculateProject_ShouldApplyFullFormula_WithZeroGoals()
    {
        var level = CreateLevel();
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            FtdTotal: 100,
            FtdSuperbet: 10,
            IsFtdGoalReached: false,
            IsProjectFtdGoalReached: false,
            CpaCount: 0,
            SalesAmount: 10_000m,
            IsSalesGoalReached: false,
            IsProjectSalesGoalReached: false,
            Rev: 0m);

        var result = CommercialAnalystProjectCommissionCalculator.CalculateProject(entry, level);

        // 90*2 + 10*5 + 0 FTD bonus + 0 CPA + 400 sales + 0 sales bonus + 0 rev = 630
        result.Commission.Should().Be(630m);
        result.IgamingFtdCount.Should().Be(90);
        result.PerProjectFtdBonus.Should().Be(0m);
    }

    [Fact]
    public void CalculateProject_ShouldClampIgamingFtdToZero_WhenSuperbetExceedsTotal()
    {
        var level = CreateLevel();
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            FtdTotal: 5,
            FtdSuperbet: 8,
            false, false, 0, 0m, false, false, 0m);

        var result = CommercialAnalystProjectCommissionCalculator.CalculateProject(entry, level);

        result.IgamingFtdCount.Should().Be(0);
        result.Commission.Should().Be(40m); // 8 * 5
    }

    [Theory]
    [InlineData(250, 350)]
    [InlineData(500, 700)]
    public void CalculateProject_ShouldApplyFtdBonusPerProject(int igamingFtd, decimal expectedBonus)
    {
        var level = CreateLevel();
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            FtdTotal: igamingFtd,
            FtdSuperbet: 0,
            false, false, 0, 0m, false, false, 0m);

        var result = CommercialAnalystProjectCommissionCalculator.CalculateProject(entry, level);

        result.PerProjectFtdBonus.Should().Be(expectedBonus);
    }

    [Fact]
    public void CalculateProject_ShouldSkipFtdBonus_WhenEveryIsZero()
    {
        var level = CreateLevel();
        level.FtdBonusEvery = 0;
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            FtdTotal: 500,
            FtdSuperbet: 0,
            false, false, 0, 0m, false, false, 0m);

        var result = CommercialAnalystProjectCommissionCalculator.CalculateProject(entry, level);

        result.PerProjectFtdBonus.Should().Be(0m);
    }

    [Fact]
    public void CalculateProject_ShouldApplySalesBonusEvery20k()
    {
        var level = CreateLevel();
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            0, 0, false, false, 0,
            SalesAmount: 40_000m,
            false, false,
            Rev: 0m);

        var result = CommercialAnalystProjectCommissionCalculator.CalculateProject(entry, level);

        // 40000*4% = 1600 + floor(40000/20000)*250 = 500 => 2100
        result.Commission.Should().Be(2100m);
    }

    [Fact]
    public void CalculateProject_ShouldSkipSalesBonus_WhenEveryIsZero()
    {
        var level = CreateLevel();
        level.SalesBonusEvery = 0m;
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            0, 0, false, false, 0,
            SalesAmount: 40_000m,
            false, false,
            Rev: 0m);

        var result = CommercialAnalystProjectCommissionCalculator.CalculateProject(entry, level);

        result.Commission.Should().Be(1600m);
    }

    [Fact]
    public void CalculateProject_ShouldApplyRevPct()
    {
        var level = CreateLevel();
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            0, 0, false, false, 0, 0m, false, false,
            Rev: 10_000m);

        var result = CommercialAnalystProjectCommissionCalculator.CalculateProject(entry, level);

        result.Commission.Should().Be(100m);
    }

    [Fact]
    public void CalculateProject_ShouldRoundPerComponent()
    {
        var level = CreateLevel();
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            0, 0, false, false, 0,
            SalesAmount: 33_333m,
            false, false,
            Rev: 0m);

        var result = CommercialAnalystProjectCommissionCalculator.CalculateProject(entry, level);

        // 33333*4% = 1333.32 + floor(33333/20000)*250 = 250
        result.Commission.Should().Be(1583.32m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenEntriesEmptyOrLevelNull()
    {
        var level = CreateLevel();

        CommercialAnalystProjectCommissionCalculator.Calculate([], level).Should().Be(0m);
        CommercialAnalystProjectCommissionCalculator.Calculate(
            [new CommercialAnalystProjectEntryInput(
                Guid.NewGuid(), ProjectPlatform.Lastlink, 1, 0, false, false, 0, 0m, false, false, 0m)],
            null).Should().Be(0m);
    }

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.CommercialAnalyst,
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
            RevPct = 1m
        };
}

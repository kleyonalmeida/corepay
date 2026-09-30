using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class CommercialSupervisorProjectCommissionCalculatorTests
{
    [Theory]
    [InlineData(10, 3, false, false, 14.1)] // 3*4 + 7*0.3 = 12 + 2.1
    [InlineData(10, 3, true, false, 18.5)] // 3*5 + 7*0.5 = 15 + 3.5
    public void CalculateProject_ShouldUseMaxZeroForOtherHouses(
        int ftdTotal,
        int ftdSuperbet,
        bool ftdGoal,
        bool salesGoal,
        decimal expectedFtdCommission)
    {
        var level = CreateLevel();
        var entry = new SupervisorProjectEntryInput(
            Guid.NewGuid(),
            ftdTotal,
            ftdSuperbet,
            SalesAmount: 0m,
            AnalystRev: 0m,
            ftdGoal,
            salesGoal);

        var result = CommercialSupervisorProjectCommissionCalculator.CalculateProject(entry, level);

        result.Should().Be(expectedFtdCommission);
    }

    [Fact]
    public void CalculateProject_ShouldTreatOtherFtdAsZero_WhenSuperbetExceedsTotal()
    {
        var level = CreateLevel();
        var entry = new SupervisorProjectEntryInput(
            Guid.NewGuid(),
            FtdTotal: 5,
            FtdSuperbet: 8,
            SalesAmount: 0m,
            AnalystRev: 0m,
            IsProjectFtdGoalReached: false,
            IsProjectSalesGoalReached: false);

        var result = CommercialSupervisorProjectCommissionCalculator.CalculateProject(entry, level);

        result.Should().Be(32m); // 8 * 4
    }

    [Theory]
    [InlineData(100_000, false, 500)]
    [InlineData(100_000, true, 800)]
    public void CalculateProject_ShouldApplySalesPct(decimal sales, bool salesGoal, decimal expectedSalesCommission)
    {
        var level = CreateLevel();
        var entry = new SupervisorProjectEntryInput(
            Guid.NewGuid(),
            FtdTotal: 0,
            FtdSuperbet: 0,
            SalesAmount: sales,
            AnalystRev: 0m,
            IsProjectFtdGoalReached: false,
            IsProjectSalesGoalReached: salesGoal);

        var result = CommercialSupervisorProjectCommissionCalculator.CalculateProject(entry, level);

        result.Should().Be(expectedSalesCommission);
    }

    [Fact]
    public void CalculateProject_ShouldApplyRevPctToAnalystRev()
    {
        var level = CreateLevel();
        var entry = new SupervisorProjectEntryInput(
            Guid.NewGuid(),
            FtdTotal: 0,
            FtdSuperbet: 0,
            SalesAmount: 0m,
            AnalystRev: 1000m,
            IsProjectFtdGoalReached: false,
            IsProjectSalesGoalReached: false);

        var result = CommercialSupervisorProjectCommissionCalculator.CalculateProject(entry, level);

        result.Should().Be(100m);
    }

    [Fact]
    public void CalculateProject_ShouldAddRecargaAndBonusCpa()
    {
        var level = CreateLevel();
        var entry = new SupervisorProjectEntryInput(
            Guid.NewGuid(),
            FtdTotal: 0,
            FtdSuperbet: 0,
            SalesAmount: 0m,
            AnalystRev: 0m,
            IsProjectFtdGoalReached: false,
            IsProjectSalesGoalReached: false,
            DeviceRecharge: 150m,
            BonusCpa: 75m);

        var result = CommercialSupervisorProjectCommissionCalculator.CalculateProject(entry, level);

        result.Should().Be(225m);
    }

    [Fact]
    public void CalculateProject_ShouldRoundPerProject_BeforeSum()
    {
        var level = CreateLevel();
        var entry = new SupervisorProjectEntryInput(
            Guid.NewGuid(),
            FtdTotal: 0,
            FtdSuperbet: 0,
            SalesAmount: 33_333m,
            AnalystRev: 0m,
            IsProjectFtdGoalReached: false,
            IsProjectSalesGoalReached: false);

        var result = CommercialSupervisorProjectCommissionCalculator.CalculateProject(entry, level);

        result.Should().Be(166.67m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenEntriesEmptyOrLevelNull()
    {
        var level = CreateLevel();

        CommercialSupervisorProjectCommissionCalculator.Calculate([], level).Should().Be(0m);
        CommercialSupervisorProjectCommissionCalculator.Calculate(
            [new SupervisorProjectEntryInput(
                Guid.NewGuid(), 1, 0, 0m, 0m, false, false)],
            null).Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldSumAllProjects()
    {
        var level = CreateLevel();
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();

        var result = CommercialSupervisorProjectCommissionCalculator.Calculate(
            [
                new SupervisorProjectEntryInput(projectA, 10, 3, 0m, 0m, false, false),
                new SupervisorProjectEntryInput(projectB, 0, 0, 100_000m, 1000m, false, true)
            ],
            level);

        result.Should().Be(14.1m + 800m + 100m);
    }

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Supervisor",
            Profile = CalculationProfile.CommercialSupervisor,
            SupFtdSuperbetNoGoal = 4m,
            SupFtdSuperbetWithGoal = 5m,
            SupFtdOtherNoGoal = 0.3m,
            SupFtdOtherWithGoal = 0.5m,
            SupSalesPctNoGoal = 0.5m,
            SupSalesPctWithGoal = 0.8m,
            SupRevPct = 10m
        };
}

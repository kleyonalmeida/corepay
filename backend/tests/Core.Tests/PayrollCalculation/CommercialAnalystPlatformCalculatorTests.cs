using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class CommercialAnalystPlatformCalculatorTests
{
    [Fact]
    public void CalculateProject_ShouldIgnoreGoalTier_ForPlatformPct()
    {
        var level = CreateLevel();
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            0, 0, false, false, 0,
            SalesAmount: 10_000m,
            IsSalesGoalReached: true,
            IsProjectSalesGoalReached: true,
            Rev: 0m);

        var result = CommercialAnalystPlatformCalculator.CalculateProject(entry, level);

        result.Should().Be(400m);
    }

    [Fact]
    public void CalculateProject_ShouldUseBasePct_ForLastlink()
    {
        var level = CreateLevel();
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Lastlink,
            0, 0, false, false, 0,
            SalesAmount: 10_000m,
            IsSalesGoalReached: true,
            IsProjectSalesGoalReached: true,
            Rev: 0m);

        var result = CommercialAnalystPlatformCalculator.CalculateProject(entry, level);

        result.Should().Be(400m);
    }

    [Fact]
    public void CalculateProject_ShouldCapHublaAt4Pct_EvenWhenLevelIs5()
    {
        var level = CreateLevel();
        level.SalesPctBase = 5m;
        var entry = new CommercialAnalystProjectEntryInput(
            Guid.NewGuid(),
            ProjectPlatform.Hubla,
            0, 0, false, false, 0,
            SalesAmount: 10_000m,
            false, false,
            Rev: 0m);

        var result = CommercialAnalystPlatformCalculator.CalculateProject(entry, level);

        result.Should().Be(400m);
    }

    [Fact]
    public void Calculate_ShouldSumAllProjects()
    {
        var level = CreateLevel();
        var entries = new[]
        {
            new CommercialAnalystProjectEntryInput(
                Guid.NewGuid(), ProjectPlatform.Lastlink,
                0, 0, false, false, 0, 5_000m, false, false, 0m),
            new CommercialAnalystProjectEntryInput(
                Guid.NewGuid(), ProjectPlatform.Hubla,
                0, 0, false, false, 0, 5_000m, false, false, 0m)
        };

        CommercialAnalystPlatformCalculator.Calculate(entries, level).Should().Be(400m);
    }

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.CommercialAnalyst,
            SalesPctBase = 4m,
            SalesPctWithGoal = 5m,
            SalesPctWithSuperGoal = 6m
        };
}

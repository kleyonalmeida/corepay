using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class CommercialAnalystCombinedFtdBonusCalculatorTests
{
    [Fact]
    public void CalculateExtra_ShouldReturnZero_When250Plus250AcrossProjects()
    {
        var level = CreateLevel();
        var breakdowns = new[]
        {
            new CommercialAnalystProjectCommissionCalculator.ProjectBreakdown(0m, 250, 350m),
            new CommercialAnalystProjectCommissionCalculator.ProjectBreakdown(0m, 250, 350m)
        };

        var extra = CommercialAnalystCombinedFtdBonusCalculator.CalculateExtra(breakdowns, level);

        extra.Should().Be(0m);
    }

    [Fact]
    public void CalculateExtra_ShouldReturn350_When400Plus100AcrossProjects()
    {
        var level = CreateLevel();
        var breakdowns = new[]
        {
            new CommercialAnalystProjectCommissionCalculator.ProjectBreakdown(0m, 400, 350m),
            new CommercialAnalystProjectCommissionCalculator.ProjectBreakdown(0m, 100, 0m)
        };

        var extra = CommercialAnalystCombinedFtdBonusCalculator.CalculateExtra(breakdowns, level);

        extra.Should().Be(350m);
    }

    [Fact]
    public void CalculateExtra_ShouldReturnZero_WhenEveryIsZero()
    {
        var level = CreateLevel();
        level.FtdBonusEvery = 0;
        var breakdowns = new[]
        {
            new CommercialAnalystProjectCommissionCalculator.ProjectBreakdown(0m, 500, 0m)
        };

        CommercialAnalystCombinedFtdBonusCalculator.CalculateExtra(breakdowns, level)
            .Should().Be(0m);
    }

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.CommercialAnalyst,
            FtdBonusEvery = 250,
            FtdBonusValue = 350m
        };
}

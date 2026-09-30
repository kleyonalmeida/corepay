using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class CommercialAnalystRateSelectorTests
{
    [Theory]
    [InlineData(false, false, 2.0)]
    [InlineData(true, false, 2.5)]
    [InlineData(false, true, 2.5)]
    [InlineData(true, true, 3.0)]
    public void SelectFtd_ShouldUseGoalCount(
        bool personalGoal,
        bool projectGoal,
        double expectedFtdRate)
    {
        var level = CreateLevel();
        var rates = CommercialAnalystRateSelector.SelectFtd(level, personalGoal, projectGoal);

        rates.FtdRate.Should().Be((decimal)expectedFtdRate);
    }

    [Theory]
    [InlineData(false, false, 4.0)]
    [InlineData(true, false, 5.0)]
    [InlineData(false, true, 5.0)]
    [InlineData(true, true, 6.0)]
    public void SelectSales_ShouldUseGoalCount(
        bool personalGoal,
        bool projectGoal,
        double expectedSalesPct)
    {
        var level = CreateLevel();
        var rates = CommercialAnalystRateSelector.SelectSales(level, personalGoal, projectGoal);

        rates.SalesPct.Should().Be((decimal)expectedSalesPct);
    }

    [Fact]
    public void SelectFtd_ShouldReturnZero_WhenLevelNull()
    {
        var rates = CommercialAnalystRateSelector.SelectFtd(null, true, true);

        rates.FtdRate.Should().Be(0m);
    }

    private static CareerLevel CreateLevel() =>
        new()
        {
            Id = Guid.NewGuid(),
            Profile = CalculationProfile.CommercialAnalyst,
            FtdRateBase = 2m,
            FtdRateWithGoal = 2.5m,
            FtdRateWithSuperGoal = 3m,
            SalesPctBase = 4m,
            SalesPctWithGoal = 5m,
            SalesPctWithSuperGoal = 6m
        };
}

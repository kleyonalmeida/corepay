using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;

namespace Core.Tests.PayrollCalculation;

public class CommercialSupervisorRateSelectorTests
{
    [Fact]
    public void Select_ShouldUseNoGoalRates_WhenProjectGoalsNotReached()
    {
        var level = CreateLevel();

        var rates = CommercialSupervisorRateSelector.Select(level, ftdGoal: false, salesGoal: false);

        rates.FtdSuperbetRate.Should().Be(4m);
        rates.FtdOtherRate.Should().Be(0.3m);
        rates.SalesPct.Should().Be(0.5m);
        rates.RevPct.Should().Be(10m);
    }

    [Fact]
    public void Select_ShouldUseWithGoalRates_WhenProjectFtdGoalReached()
    {
        var level = CreateLevel();

        var rates = CommercialSupervisorRateSelector.Select(level, ftdGoal: true, salesGoal: false);

        rates.FtdSuperbetRate.Should().Be(5m);
        rates.FtdOtherRate.Should().Be(0.5m);
        rates.SalesPct.Should().Be(0.5m);
        rates.RevPct.Should().Be(10m);
    }

    [Fact]
    public void Select_ShouldUseWithGoalSalesPct_WhenProjectSalesGoalReached()
    {
        var level = CreateLevel();

        var rates = CommercialSupervisorRateSelector.Select(level, ftdGoal: false, salesGoal: true);

        rates.FtdSuperbetRate.Should().Be(4m);
        rates.FtdOtherRate.Should().Be(0.3m);
        rates.SalesPct.Should().Be(0.8m);
        rates.RevPct.Should().Be(10m);
    }

    [Fact]
    public void Select_ShouldReturnZeroRates_WhenCareerLevelIsNull()
    {
        var rates = CommercialSupervisorRateSelector.Select(null, ftdGoal: true, salesGoal: true);

        rates.FtdSuperbetRate.Should().Be(0m);
        rates.FtdOtherRate.Should().Be(0m);
        rates.SalesPct.Should().Be(0m);
        rates.RevPct.Should().Be(0m);
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

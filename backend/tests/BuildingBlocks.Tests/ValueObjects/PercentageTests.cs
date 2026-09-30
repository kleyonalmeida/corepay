using BuildingBlocks.ValueObjects;
using FluentAssertions;

namespace BuildingBlocks.Tests.ValueObjects;

public class PercentageTests
{
    [Fact]
    public void FromPercentPoints_ShouldStoreExplicitPercent()
    {
        var percentage = Percentage.FromPercentPoints(2.0m);

        percentage.PercentPoints.Should().Be(2.0m);
    }

    [Fact]
    public void ToFactor_ShouldDivideByOneHundred()
    {
        var percentage = Percentage.FromPercentPoints(2.0m);

        percentage.ToFactor().Should().Be(0.02m);
    }

    [Fact]
    public void ApplyTo_ShouldMultiplyMoneyByFactor()
    {
        var percentage = Percentage.FromPercentPoints(2.0m);
        var money = Money.FromDecimal(1000m);

        var result = percentage.ApplyTo(money);

        result.Amount.Should().Be(20m);
    }
}

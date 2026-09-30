using BuildingBlocks.ValueObjects;
using FluentAssertions;

namespace BuildingBlocks.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_ShouldDefaultToBrl()
    {
        var money = Money.FromDecimal(100.50m);

        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be(Currency.Brl);
    }

    [Fact]
    public void Add_ShouldPreservePrecisionWithoutImplicitRounding()
    {
        var left = Money.FromDecimal(10.005m);
        var right = Money.FromDecimal(0.004m);

        var sum = left + right;

        sum.Amount.Should().Be(10.009m);
    }

    [Fact]
    public void RoundToCurrencyScale_ShouldRoundToTwoDecimals()
    {
        var money = Money.FromDecimal(10.005m);

        var rounded = money.RoundToCurrencyScale();

        rounded.Amount.Should().Be(10.01m);
    }

    [Fact]
    public void Equality_ShouldCompareAmountAndCurrency()
    {
        var left = Money.FromDecimal(100m);
        var right = Money.FromDecimal(100m);
        var different = Money.FromDecimal(99m);

        left.Should().Be(right);
        left.Should().NotBe(different);
    }
}

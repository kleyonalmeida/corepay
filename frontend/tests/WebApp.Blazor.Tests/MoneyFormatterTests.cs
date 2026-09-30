using FluentAssertions;
using WebApp.Blazor.Formatting;

namespace WebApp.Blazor.Tests;

public class MoneyFormatterTests
{
    [Fact]
    public void FormatMoney_FormatsStandardValue()
    {
        MoneyFormatter.FormatMoney(1234.56m).Should().Be("R$ 1.234,56");
    }

    [Fact]
    public void FormatMoney_FormatsZero()
    {
        MoneyFormatter.FormatMoney(0m).Should().Be("R$ 0,00");
    }

    [Fact]
    public void FormatMoney_FormatsNegativeValue()
    {
        MoneyFormatter.FormatMoney(-99.9m).Should().Be("-R$ 99,90");
    }

    [Fact]
    public void FormatMoney_FormatsLargeValue()
    {
        MoneyFormatter.FormatMoney(1_000_000m).Should().Be("R$ 1.000.000,00");
    }

    [Fact]
    public void FormatMoney_RoundsToTwoDecimalPlaces()
    {
        MoneyFormatter.FormatMoney(10.005m).Should().Be("R$ 10,01");
    }
}

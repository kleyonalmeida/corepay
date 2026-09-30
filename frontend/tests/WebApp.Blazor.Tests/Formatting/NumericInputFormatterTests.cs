using FluentAssertions;
using WebApp.Blazor.Formatting;

namespace WebApp.Blazor.Tests.Formatting;

public class NumericInputFormatterTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("1", "0,01")]
    [InlineData("12", "0,12")]
    [InlineData("123456", "1.234,56")]
    [InlineData("01000asdasdawe", "10,00")]
    [InlineData("R$ 1.234,56", "1.234,56")]
    public void FormatInput_MoneyCents_FormatsBrazilianCurrency(string raw, string expected)
    {
        NumericInputFormatter.FormatInput(NumericInputKind.MoneyCents, raw).Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("250", "250")]
    [InlineData("1250", "1.250")]
    [InlineData("abc12x34", "1.234")]
    public void FormatInput_Integer_AllowsDigitsOnly(string raw, string expected)
    {
        NumericInputFormatter.FormatInput(NumericInputKind.Integer, raw).Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("12,5", "12,5")]
    [InlineData("12.5", "12,5")]
    [InlineData("abc12,34xyz", "12,34")]
    [InlineData("1,2,3", "1,23")]
    public void FormatInput_Percent_AllowsDecimalComma(string raw, string expected)
    {
        NumericInputFormatter.FormatInput(NumericInputKind.Percent, raw).Should().Be(expected);
    }

    [Fact]
    public void TryParse_MoneyCents_ParsesFormattedValue()
    {
        NumericInputFormatter.TryParse(NumericInputKind.MoneyCents, "1.234,56", out var result).Should().BeTrue();
        result.Should().Be(1234.56m);
    }

    [Fact]
    public void TryParse_Integer_ParsesFormattedValue()
    {
        NumericInputFormatter.TryParse(NumericInputKind.Integer, "1.250", out var result).Should().BeTrue();
        result.Should().Be(1250m);
    }

    [Fact]
    public void FormatFromDecimal_MoneyCents_UsesPtBr()
    {
        NumericInputFormatter.FormatFromDecimal(NumericInputKind.MoneyCents, 1234.56m).Should().Be("1.234,56");
    }

    [Fact]
    public void NormalizeDisplay_MoneyCents_ConvertsLegacyInvariantValue()
    {
        NumericInputFormatter.NormalizeDisplay(NumericInputKind.MoneyCents, "1234.56")
            .Should().Be("1.234,56");
    }
}

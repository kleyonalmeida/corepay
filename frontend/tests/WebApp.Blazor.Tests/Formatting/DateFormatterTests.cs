using FluentAssertions;
using WebApp.Blazor.Formatting;

namespace WebApp.Blazor.Tests.Formatting;

public class DateFormatterTests
{
    [Fact]
    public void FormatDate_FormatsBrazilianDate()
    {
        DateFormatter.FormatDate(new DateOnly(2024, 3, 1)).Should().Be("01/03/2024");
    }

    [Fact]
    public void FormatDate_Null_ReturnsDash()
    {
        DateFormatter.FormatDate(null).Should().Be("—");
    }
}

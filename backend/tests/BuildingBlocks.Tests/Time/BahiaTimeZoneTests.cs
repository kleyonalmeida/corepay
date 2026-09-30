using BuildingBlocks.Time;
using FluentAssertions;

namespace BuildingBlocks.Tests.Time;

public class BahiaTimeZoneTests
{
    [Fact]
    public void ZoneId_ShouldBeAmericaBahia()
    {
        BahiaTimeZone.ZoneId.Should().Be("America/Bahia");
    }

    [Fact]
    public void ToLocal_ShouldConvertUtcToBahia()
    {
        var utc = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

        var local = BahiaTimeZone.ToLocal(utc);

        local.Offset.Should().Be(TimeSpan.FromHours(-3));
        local.DateTime.Should().Be(new DateTime(2026, 3, 15, 9, 0, 0));
    }

    [Fact]
    public void GetCivilMonthYear_ShouldUseBahiaCalendar()
    {
        var utc = new DateTimeOffset(2026, 1, 1, 2, 30, 0, TimeSpan.Zero);

        var (month, year) = BahiaTimeZone.GetCivilMonthYear(utc);

        month.Should().Be(12);
        year.Should().Be(2025);
    }
}

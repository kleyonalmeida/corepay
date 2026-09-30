namespace BuildingBlocks.Time;

public static class BahiaTimeZone
{
    public const string ZoneId = "America/Bahia";

    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(ZoneId);

    public static DateTimeOffset ToLocal(DateTimeOffset utcInstant) =>
        TimeZoneInfo.ConvertTime(utcInstant, Zone);

    public static (int Month, int Year) GetCivilMonthYear(DateTimeOffset utcInstant)
    {
        var local = ToLocal(utcInstant);
        return (local.Month, local.Year);
    }
}

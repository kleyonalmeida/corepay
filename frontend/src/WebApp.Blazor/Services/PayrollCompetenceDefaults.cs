namespace WebApp.Blazor.Services;

public static class PayrollCompetenceDefaults
{
    private static readonly TimeZoneInfo BahiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bahia");

    public static (int Month, int Year) CurrentCompetence()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BahiaTimeZone);
        return (now.Month, now.Year);
    }

    public static IReadOnlyList<(int Value, string Label)> MonthOptions { get; } =
    [
        (1, "Janeiro"),
        (2, "Fevereiro"),
        (3, "Março"),
        (4, "Abril"),
        (5, "Maio"),
        (6, "Junho"),
        (7, "Julho"),
        (8, "Agosto"),
        (9, "Setembro"),
        (10, "Outubro"),
        (11, "Novembro"),
        (12, "Dezembro")
    ];

    public static IReadOnlyList<int> YearOptions()
    {
        var currentYear = CurrentCompetence().Year;
        return Enumerable.Range(currentYear - 1, 4).ToList();
    }
}

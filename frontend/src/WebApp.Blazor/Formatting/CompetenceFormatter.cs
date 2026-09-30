using System.Globalization;

namespace WebApp.Blazor.Formatting;

public static class CompetenceFormatter
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Format(int month, int year)
    {
        var text = new DateOnly(year, month, 1).ToString("MMM/yyyy", PtBr);
        var parts = text.Split('/');
        if (parts.Length != 2)
        {
            return text;
        }

        var monthLabel = parts[0].TrimEnd('.');
        if (monthLabel.Length > 0)
        {
            monthLabel = char.ToUpper(monthLabel[0], PtBr) + monthLabel[1..];
        }

        return $"{monthLabel}/{parts[1]}";
    }
}

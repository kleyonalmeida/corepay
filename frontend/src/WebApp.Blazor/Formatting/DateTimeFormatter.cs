using System.Globalization;

namespace WebApp.Blazor.Formatting;

public static class DateTimeFormatter
{
    private static readonly CultureInfo BrazilianCulture = CultureInfo.GetCultureInfo("pt-BR");

    public static string FormatDateTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", BrazilianCulture);
}

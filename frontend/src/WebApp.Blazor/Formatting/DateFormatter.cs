using System.Globalization;

namespace WebApp.Blazor.Formatting;

public static class DateFormatter
{
    private static readonly CultureInfo BrazilianCulture = CultureInfo.GetCultureInfo("pt-BR");

    public static string FormatDate(DateOnly? value) =>
        value?.ToString("dd/MM/yyyy", BrazilianCulture) ?? "—";
}

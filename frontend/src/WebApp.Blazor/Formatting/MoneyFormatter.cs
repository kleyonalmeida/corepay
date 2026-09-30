using System.Globalization;

namespace WebApp.Blazor.Formatting;

public static class MoneyFormatter
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string FormatMoney(decimal amount) =>
        amount.ToString("C2", PtBr);
}

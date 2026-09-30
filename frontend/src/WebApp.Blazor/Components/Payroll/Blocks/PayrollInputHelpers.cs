using WebApp.Blazor.Formatting;

namespace WebApp.Blazor.Components.Payroll.Blocks;

internal static class PayrollInputHelpers
{
    public static decimal ParseMoney(string? value) =>
        DecimalInputParser.TryParse(value, out var parsed) ? parsed : 0m;

    public static int ParseInt(string? value) =>
        DecimalInputParser.TryParseInt(value, out var parsed) ? parsed : 0;

    public static string FormatMoney(decimal value) =>
        DecimalInputParser.FormatMoneyInput(value);

    public static string FormatInt(int value) =>
        DecimalInputParser.FormatInt(value);

    public static string FormatPercent(decimal value) =>
        DecimalInputParser.FormatDecimal(value);
}

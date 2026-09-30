using System.Globalization;

namespace WebApp.Blazor.Formatting;

public static class DecimalInputParser
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static bool TryParse(string? value, out decimal result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0;
            return false;
        }

        return decimal.TryParse(value, NumberStyles.Number, PtBr, out result)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    public static string FormatDecimal(decimal value) =>
        value.ToString("0.##", PtBr);

    public static string FormatMoneyInput(decimal value) =>
        value.ToString("N2", PtBr);

    public static bool TryParseInt(string? value, out int result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0;
            return false;
        }

        return int.TryParse(value, NumberStyles.Integer | NumberStyles.AllowThousands, PtBr, out result)
            || int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    public static string FormatInt(int value) =>
        value.ToString(PtBr);
}

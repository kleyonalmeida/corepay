using System.Globalization;
using System.Text;

namespace WebApp.Blazor.Formatting;

public static class NumericInputFormatter
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private const int MaxDigits = 15;

    public static string FormatInput(NumericInputKind kind, string? rawInput) =>
        kind switch
        {
            NumericInputKind.MoneyCents => FormatMoneyCents(rawInput),
            NumericInputKind.Integer => FormatInteger(rawInput),
            NumericInputKind.Percent => FormatPercent(rawInput),
            _ => rawInput ?? string.Empty
        };

    public static string FormatFromDecimal(NumericInputKind kind, decimal value) =>
        kind switch
        {
            NumericInputKind.MoneyCents => DecimalInputParser.FormatMoneyInput(value),
            NumericInputKind.Integer => DecimalInputParser.FormatInt((int)value),
            NumericInputKind.Percent => DecimalInputParser.FormatDecimal(value),
            _ => value.ToString(PtBr)
        };

    public static string FormatFromInt(NumericInputKind kind, int value) =>
        kind switch
        {
            NumericInputKind.Integer => DecimalInputParser.FormatInt(value),
            NumericInputKind.MoneyCents => DecimalInputParser.FormatMoneyInput(value),
            NumericInputKind.Percent => DecimalInputParser.FormatDecimal(value),
            _ => value.ToString(PtBr)
        };

    public static bool TryParse(NumericInputKind kind, string? display, out decimal result)
    {
        if (kind == NumericInputKind.Integer)
        {
            if (DecimalInputParser.TryParseInt(display, out var intResult))
            {
                result = intResult;
                return true;
            }

            result = 0;
            return false;
        }

        return DecimalInputParser.TryParse(display, out result);
    }

    public static string NormalizeDisplay(NumericInputKind kind, string? value)
    {
        if (kind == NumericInputKind.Text || string.IsNullOrWhiteSpace(value))
        {
            return value ?? string.Empty;
        }

        if (kind == NumericInputKind.MoneyCents
            && value.Contains('.')
            && !value.Contains(',')
            && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantAmount))
        {
            return FormatFromDecimal(kind, invariantAmount);
        }

        if (TryParse(kind, value, out var parsed))
        {
            return FormatFromDecimal(kind, parsed);
        }

        return FormatInput(kind, value);
    }

    private static string ExtractDigits(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        var digits = new StringBuilder(raw.Length);
        foreach (var character in raw)
        {
            if (char.IsDigit(character))
            {
                digits.Append(character);
            }
        }

        var normalized = digits.ToString();
        if (normalized.Length > MaxDigits)
        {
            normalized = normalized[^MaxDigits..];
        }

        return normalized;
    }

    private static string FormatMoneyCents(string? rawInput)
    {
        var digits = ExtractDigits(rawInput);
        if (string.IsNullOrEmpty(digits))
        {
            return string.Empty;
        }

        if (!long.TryParse(digits, out var cents))
        {
            return string.Empty;
        }

        return (cents / 100m).ToString("N2", PtBr);
    }

    private static string FormatInteger(string? rawInput)
    {
        var digits = ExtractDigits(rawInput);
        if (string.IsNullOrEmpty(digits))
        {
            return string.Empty;
        }

        if (!long.TryParse(digits, out var number))
        {
            return string.Empty;
        }

        return number.ToString("N0", PtBr);
    }

    private static string FormatPercent(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return string.Empty;
        }

        var normalized = new StringBuilder(rawInput.Length);
        var commaSeen = false;

        foreach (var character in rawInput)
        {
            if (char.IsDigit(character))
            {
                normalized.Append(character);
                continue;
            }

            if ((character == ',' || character == '.') && !commaSeen)
            {
                normalized.Append(',');
                commaSeen = true;
            }
        }

        return normalized.ToString();
    }
}

using System.Globalization;

namespace WebApp.Blazor.Formatting;

public static class DateInputParser
{
    public static bool TryParseDateOnly(string? value, out DateOnly result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        return DateOnly.TryParseExact(
                   value.Trim(),
                   "yyyy-MM-dd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out result)
               || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    public static string FormatDateInput(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
}

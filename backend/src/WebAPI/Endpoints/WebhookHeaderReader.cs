namespace WebAPI.Endpoints;

internal static class WebhookHeaderReader
{
    public const int TimestampMaxLength = 32;
    public const int SignatureMaxLength = 128;
    public const int IdempotencyKeyMaxLength = 64;

    public static bool TryReadSingleHeader(
        IHeaderDictionary headers,
        string headerName,
        int maxLength,
        out string value,
        out string? errorCode,
        out string? errorMessage)
    {
        value = string.Empty;
        errorCode = null;
        errorMessage = null;

        if (!headers.TryGetValue(headerName, out var values))
        {
            errorCode = "facilities.header_missing";
            errorMessage = $"{headerName} header is required.";
            return false;
        }

        if (values.Count != 1)
        {
            errorCode = "facilities.header_duplicate";
            errorMessage = $"{headerName} header must appear exactly once.";
            return false;
        }

        value = values[0] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            errorCode = "facilities.header_missing";
            errorMessage = $"{headerName} header is required.";
            return false;
        }

        if (value.Length > maxLength)
        {
            errorCode = "facilities.header_too_long";
            errorMessage = $"{headerName} header exceeds the allowed length.";
            return false;
        }

        return true;
    }
}

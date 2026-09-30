using BuildingBlocks.Results;

namespace Core.Security;

public static class SafeExternalUrlValidator
{
    public const int MaxUrlLength = 1024;

    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        Uri.UriSchemeHttps
    };

    public static Result<string?> Validate(string? url, IReadOnlyList<string>? allowedHosts = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result<string?>.Success(null);
        }

        var trimmed = url.Trim();
        if (trimmed.Length > MaxUrlLength)
        {
            return Result<string?>.Failure(Error.Validation(
                "url.too_long",
                $"URL must not exceed {MaxUrlLength} characters."));
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return Result<string?>.Failure(Error.Validation(
                "url.invalid",
                "URL must be an absolute HTTPS address."));
        }

        if (!AllowedSchemes.Contains(uri.Scheme))
        {
            return Result<string?>.Failure(Error.Validation(
                "url.scheme_not_allowed",
                "Only HTTPS URLs are allowed."));
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return Result<string?>.Failure(Error.Validation(
                "url.credentials_not_allowed",
                "URLs with embedded credentials are not allowed."));
        }

        if (allowedHosts is { Count: > 0 }
            && !allowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            return Result<string?>.Failure(Error.Validation(
                "url.host_not_allowed",
                "URL host is not in the allowed list."));
        }

        return Result<string?>.Success(uri.ToString());
    }
}

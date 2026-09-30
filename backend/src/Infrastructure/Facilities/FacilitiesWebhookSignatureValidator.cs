using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Results;
using Microsoft.Extensions.Options;

namespace Infrastructure.Facilities;

public sealed class FacilitiesWebhookSignatureValidator : IFacilitiesWebhookSignatureValidator
{
    private const string SignaturePrefix = "sha256=";
    private const int ReplayWindowSeconds = 300;

    private readonly FacilitiesOptions _options;
    private readonly TimeProvider _timeProvider;

    public FacilitiesWebhookSignatureValidator(
        IOptions<FacilitiesOptions> options,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public Result Validate(string? timestampHeader, string? signatureHeader, ReadOnlyMemory<byte> rawBody)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            return Result.Failure(
                Error.Unauthorized("facilities.webhook_secret_missing", "Facilities webhook secret is not configured."));
        }

        if (string.IsNullOrWhiteSpace(timestampHeader))
        {
            return Result.Failure(
                Error.Unauthorized("facilities.timestamp_missing", "Facilities timestamp header is required."));
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return Result.Failure(
                Error.Unauthorized("facilities.signature_missing", "Facilities signature header is required."));
        }

        if (!long.TryParse(timestampHeader.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestampSeconds))
        {
            return Result.Failure(
                Error.Unauthorized("facilities.timestamp_invalid", "Facilities timestamp header is invalid."));
        }

        var requestInstant = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
        var now = _timeProvider.GetUtcNow();
        var deltaSeconds = Math.Abs((now - requestInstant).TotalSeconds);
        if (deltaSeconds > ReplayWindowSeconds)
        {
            return Result.Failure(
                Error.Unauthorized("facilities.timestamp_expired", "Facilities timestamp is outside the allowed replay window."));
        }

        if (!TryDecodeSignature(signatureHeader.Trim(), out var providedSignature))
        {
            return Result.Failure(
                Error.Unauthorized("facilities.signature_invalid", "Facilities signature header is invalid."));
        }

        var expectedSignature = ComputeSignature(_options.WebhookSecret, timestampHeader.Trim(), rawBody);
        if (!CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
        {
            return Result.Failure(
                Error.Unauthorized("facilities.signature_mismatch", "Facilities signature verification failed."));
        }

        return Result.Success();
    }

    private static bool TryDecodeSignature(string signatureHeader, out byte[] signatureBytes)
    {
        signatureBytes = [];

        if (!signatureHeader.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hex = signatureHeader[SignaturePrefix.Length..];
        const int sha256HexLength = 64;
        if (hex.Length != sha256HexLength || !IsLowerHex(hex))
        {
            return false;
        }

        try
        {
            signatureBytes = Convert.FromHexString(hex);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsLowerHex(string value)
    {
        foreach (var character in value)
        {
            var isDigit = character is >= '0' and <= '9';
            var isLowerHex = character is >= 'a' and <= 'f';
            if (!isDigit && !isLowerHex)
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] ComputeSignature(string secret, string timestamp, ReadOnlyMemory<byte> rawBody)
    {
        var prefix = Encoding.UTF8.GetBytes($"{timestamp}.");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        hmac.TransformBlock(prefix, 0, prefix.Length, null, 0);
        hmac.TransformFinalBlock(rawBody.ToArray(), 0, rawBody.Length);
        return hmac.Hash ?? [];
    }
}

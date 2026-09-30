using BuildingBlocks.Results;

namespace Infrastructure.Facilities;

public interface IFacilitiesWebhookSignatureValidator
{
    Result Validate(string? timestampHeader, string? signatureHeader, ReadOnlyMemory<byte> rawBody);
}

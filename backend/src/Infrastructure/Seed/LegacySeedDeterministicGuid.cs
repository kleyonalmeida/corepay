using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Seed;

public static class DeterministicGuid
{
    private const string Namespace = "corepay-pro:legacy-data-seed:v1";

    public static Guid Create(string entityType, string sourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);

        var input = Encoding.UTF8.GetBytes($"{Namespace}\n{entityType}\n{sourceKey}");
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(input, hash);

        Span<byte> guidBytes = stackalloc byte[16];
        hash[..16].CopyTo(guidBytes);

        // UUID v5 layout and RFC 4122 variant, with SHA-256 as the requested digest.
        guidBytes[6] = (byte)((guidBytes[6] & 0x0f) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3f) | 0x80);
        return new Guid(guidBytes, bigEndian: true);
    }
}

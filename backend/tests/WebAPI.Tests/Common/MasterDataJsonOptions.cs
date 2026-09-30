using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Tests.Common;

internal static class MasterDataJsonOptions
{
    public static JsonSerializerOptions Instance { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };
}

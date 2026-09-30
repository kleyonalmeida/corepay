using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Payrolls;

internal static class PayrollJsonOptions
{
    public static JsonSerializerOptions Instance { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}

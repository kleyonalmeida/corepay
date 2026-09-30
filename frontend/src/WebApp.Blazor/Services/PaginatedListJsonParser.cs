using System.Text.Json;

namespace WebApp.Blazor.Services;

internal static class PaginatedListJsonParser
{
    public static PaginatedListPayload<T>? TryParse<T>(string json, JsonSerializerOptions options)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                var items = JsonSerializer.Deserialize<List<T>>(json, options);
                if (items is null)
                {
                    return null;
                }

                return new PaginatedListPayload<T>(items, items.Count, Page: 1, PageSize: items.Count);
            }

            var payload = JsonSerializer.Deserialize<PaginatedListEnvelopeDto<T>>(json, options);
            if (payload?.Items is null)
            {
                return null;
            }

            return new PaginatedListPayload<T>(
                payload.Items,
                payload.TotalCount,
                payload.Page,
                payload.PageSize);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

internal sealed record PaginatedListEnvelopeDto<T>(
    IReadOnlyList<T>? Items,
    int TotalCount,
    int Page,
    int PageSize);

internal sealed record PaginatedListPayload<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

using System.Text.Json;

namespace WebApp.Blazor.Tests.Common;

internal static class CollaboratorTestJson
{
    public static string WrapList(object[] items) =>
        JsonSerializer.Serialize(new
        {
            items,
            totalCount = items.Length,
            page = 1,
            pageSize = 30
        });
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class NotificationApiService(HttpClient httpClient) : INotificationApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<NotificationsListResult> GetAsync(
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildListUrl(page, pageSize), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new NotificationsListResult(NotificationApiStatus.Error);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new NotificationsListResult(NotificationApiStatus.Error);
        }

        var data = await response.Content.ReadFromJsonAsync<NotificationsListDto>(JsonOptions, cancellationToken);
        return data is null
            ? new NotificationsListResult(NotificationApiStatus.Error)
            : new NotificationsListResult(NotificationApiStatus.Success, data);
    }

    public async Task<NotificationMarkReadResult> MarkReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PutAsync(BuildMarkReadUrl(notificationId), null, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new NotificationMarkReadResult(NotificationApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            return new NotificationMarkReadResult(
                NotificationApiStatus.NotFound,
                ErrorCode: error?.Error,
                Message: error?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new NotificationMarkReadResult(NotificationApiStatus.Error);
        }

        return new NotificationMarkReadResult(NotificationApiStatus.Success);
    }

    internal static string BuildListUrl(int? page = null, int? pageSize = null)
    {
        var parameters = new List<string>();
        if (page is not null)
        {
            parameters.Add($"page={page.Value}");
        }

        if (pageSize is not null)
        {
            parameters.Add($"pageSize={pageSize.Value}");
        }

        return parameters.Count == 0
            ? "api/v1/notifications"
            : $"api/v1/notifications?{string.Join('&', parameters)}";
    }

    internal static string BuildMarkReadUrl(Guid notificationId) =>
        $"api/v1/notifications/{notificationId}/read";

    private static async Task<ApiErrorResponse?> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ApiErrorResponse(string? Error, string? Message);
}

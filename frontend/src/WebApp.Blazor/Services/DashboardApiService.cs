using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class DashboardApiService(HttpClient httpClient) : IDashboardApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<DashboardResult> GetAsync(
        DashboardQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildUrl(query), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new DashboardResult(DashboardApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            return new DashboardResult(
                DashboardApiStatus.ValidationError,
                ErrorCode: error?.Error,
                Message: error?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new DashboardResult(DashboardApiStatus.Error);
        }

        var dashboard = await response.Content.ReadFromJsonAsync<DashboardDto>(JsonOptions, cancellationToken);
        return dashboard is null
            ? new DashboardResult(DashboardApiStatus.Error)
            : new DashboardResult(DashboardApiStatus.Success, dashboard);
    }

    internal static string BuildUrl(DashboardQuery? query = null)
    {
        var parameters = new List<string>();
        if (query?.Month is not null)
        {
            parameters.Add($"month={query.Month.Value}");
        }

        if (query?.Year is not null)
        {
            parameters.Add($"year={query.Year.Value}");
        }

        return parameters.Count == 0
            ? "api/v1/dashboard"
            : $"api/v1/dashboard?{string.Join("&", parameters)}";
    }

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

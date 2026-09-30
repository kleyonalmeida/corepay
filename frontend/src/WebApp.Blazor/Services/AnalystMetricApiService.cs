using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Blazor.Services;

public sealed class AnalystMetricApiService(HttpClient httpClient) : IAnalystMetricApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<AnalystMetricListResult> GetMetricsAsync(
        AnalystMetricListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildListUrl(query), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new AnalystMetricListResult(AnalystMetricApiStatus.Error);
        }

        if (response.IsSuccessStatusCode)
        {
            var metrics = await response.Content.ReadFromJsonAsync<List<AnalystMetricDto>>(
                JsonOptions,
                cancellationToken);
            return metrics is null
                ? new AnalystMetricListResult(AnalystMetricApiStatus.Error)
                : new AnalystMetricListResult(AnalystMetricApiStatus.Success, metrics);
        }

        var error = await ReadErrorAsync(response, cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => new(AnalystMetricApiStatus.Forbidden, ErrorCode: error?.Error, Message: error?.Message),
            HttpStatusCode.BadRequest => new(AnalystMetricApiStatus.ValidationError, ErrorCode: error?.Error, Message: error?.Message),
            _ => new(AnalystMetricApiStatus.Error, ErrorCode: error?.Error, Message: error?.Message)
        };
    }

    public Task<AnalystMetricMutationResult> CreateMetricAsync(
        AnalystMetricRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Post, "api/v1/analyst-metrics", request, cancellationToken);

    public Task<AnalystMetricMutationResult> UpdateMetricAsync(
        Guid id,
        AnalystMetricRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Put, $"api/v1/analyst-metrics/{id}", request, cancellationToken);

    internal static string BuildListUrl(AnalystMetricListQuery? query)
    {
        if (query is null)
        {
            return "api/v1/analyst-metrics";
        }

        var parameters = new List<string>();
        Add(parameters, "month", query.Month);
        Add(parameters, "year", query.Year);
        Add(parameters, "departmentId", query.DepartmentId);
        Add(parameters, "collaboratorId", query.CollaboratorId);
        Add(parameters, "projectId", query.ProjectId);
        return parameters.Count == 0
            ? "api/v1/analyst-metrics"
            : $"api/v1/analyst-metrics?{string.Join("&", parameters)}";
    }

    private async Task<AnalystMetricMutationResult> SendMutationAsync(
        HttpMethod method,
        string url,
        AnalystMetricRequest request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = method == HttpMethod.Post
                ? await httpClient.PostAsJsonAsync(url, request, JsonOptions, cancellationToken)
                : await httpClient.PutAsJsonAsync(url, request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new AnalystMetricMutationResult(AnalystMetricApiStatus.Error);
        }

        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var metric = await response.Content.ReadFromJsonAsync<AnalystMetricDto>(JsonOptions, cancellationToken);
            return metric is null
                ? new AnalystMetricMutationResult(AnalystMetricApiStatus.Error)
                : new AnalystMetricMutationResult(AnalystMetricApiStatus.Success, metric);
        }

        var error = await ReadErrorAsync(response, cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new(AnalystMetricApiStatus.ValidationError, ErrorCode: error?.Error, Message: error?.Message),
            HttpStatusCode.NotFound => new(AnalystMetricApiStatus.NotFound, ErrorCode: error?.Error, Message: error?.Message),
            HttpStatusCode.Conflict => new(AnalystMetricApiStatus.Conflict, ErrorCode: error?.Error, Message: error?.Message),
            HttpStatusCode.Forbidden => new(AnalystMetricApiStatus.Forbidden, ErrorCode: error?.Error, Message: error?.Message),
            _ => new(AnalystMetricApiStatus.Error, ErrorCode: error?.Error, Message: error?.Message)
        };
    }

    private static void Add<T>(ICollection<string> parameters, string key, T? value)
        where T : struct
    {
        if (value is not null)
        {
            parameters.Add($"{key}={value.Value}");
        }
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
}

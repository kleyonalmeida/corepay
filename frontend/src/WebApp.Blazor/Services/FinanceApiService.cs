using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class FinanceApiService(HttpClient httpClient) : IFinanceApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<FinanceSummaryResult> GetSummaryAsync(
        FinanceSummaryQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildSummaryUrl(query), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new FinanceSummaryResult(FinanceApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new FinanceSummaryResult(
                FinanceApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new FinanceSummaryResult(FinanceApiStatus.Error);
        }

        var summary = await response.Content.ReadFromJsonAsync<FinanceSummaryDto>(JsonOptions, cancellationToken);
        return summary is null
            ? new FinanceSummaryResult(FinanceApiStatus.Error)
            : new FinanceSummaryResult(FinanceApiStatus.Success, summary);
    }

    internal static string BuildSummaryUrl(FinanceSummaryQuery? query)
    {
        if (query is null)
        {
            return "api/v1/finance/summary";
        }

        var parameters = new List<string>();
        if (query.Month is not null)
        {
            parameters.Add($"month={query.Month.Value}");
        }

        if (query.Year is not null)
        {
            parameters.Add($"year={query.Year.Value}");
        }

        if (query.DepartmentId is not null)
        {
            parameters.Add($"departmentId={query.DepartmentId.Value}");
        }

        if (query.ProjectId is not null)
        {
            parameters.Add($"projectId={query.ProjectId.Value}");
        }

        return parameters.Count == 0
            ? "api/v1/finance/summary"
            : $"api/v1/finance/summary?{string.Join("&", parameters)}";
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

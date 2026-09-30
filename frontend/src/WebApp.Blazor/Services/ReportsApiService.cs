using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class ReportsApiService(HttpClient httpClient) : IReportsApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public Task<ReportsResult> GetPayrollReportAsync(
        ReportsQuery? query = null,
        CancellationToken cancellationToken = default) =>
        SendJsonAsync<ReportsDto>(
            BuildUrl(query),
            dto => new ReportsResult(ReportsApiStatus.Success, dto),
            cancellationToken);

    public Task<ReportsExportResult> ExportPayrollReportAsync(
        ReportsQuery? query = null,
        CancellationToken cancellationToken = default) =>
        SendBinaryAsync(BuildExportUrl(query), cancellationToken);

    internal static string BuildUrl(ReportsQuery? query = null)
    {
        var parameters = BuildQueryParameters(query);
        return parameters.Count == 0
            ? "api/v1/reports/payroll"
            : $"api/v1/reports/payroll?{string.Join("&", parameters)}";
    }

    internal static string BuildExportUrl(ReportsQuery? query = null)
    {
        var parameters = BuildQueryParameters(query);
        return parameters.Count == 0
            ? "api/v1/reports/payroll/export"
            : $"api/v1/reports/payroll/export?{string.Join("&", parameters)}";
    }

    private static List<string> BuildQueryParameters(ReportsQuery? query)
    {
        var parameters = new List<string>();
        if (query?.Year is not null)
        {
            parameters.Add($"year={query.Year.Value}");
        }

        if (query?.DepartmentId is not null)
        {
            parameters.Add($"departmentId={query.DepartmentId.Value}");
        }

        if (query?.ProjectId is not null)
        {
            parameters.Add($"projectId={query.ProjectId.Value}");
        }

        return parameters;
    }

    private async Task<ReportsResult> SendJsonAsync<T>(
        string url,
        Func<T, ReportsResult> onSuccess,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new ReportsResult(ReportsApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ReportsResult(ReportsApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            return new ReportsResult(
                ReportsApiStatus.ValidationError,
                ErrorCode: error?.Error,
                Message: error?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ReportsResult(ReportsApiStatus.Error);
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return payload is null
            ? new ReportsResult(ReportsApiStatus.Error)
            : onSuccess(payload);
    }

    private async Task<ReportsExportResult> SendBinaryAsync(
        string url,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new ReportsExportResult(ReportsApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ReportsExportResult(ReportsApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            return new ReportsExportResult(
                ReportsApiStatus.ValidationError,
                ErrorCode: error?.Error,
                Message: error?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ReportsExportResult(ReportsApiStatus.Error);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length == 0)
        {
            return new ReportsExportResult(ReportsApiStatus.Error);
        }

        return new ReportsExportResult(
            ReportsApiStatus.Success,
            FileBytes: bytes,
            FileName: ParseFileName(response) ?? "relatorio-folha.xlsx",
            ContentType: response.Content.Headers.ContentType?.MediaType
                ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static string? ParseFileName(HttpResponseMessage response)
    {
        var fileName = response.Content.Headers.ContentDisposition?.FileName
            ?? response.Content.Headers.ContentDisposition?.FileNameStar;

        return string.IsNullOrWhiteSpace(fileName)
            ? null
            : fileName.Trim('"');
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

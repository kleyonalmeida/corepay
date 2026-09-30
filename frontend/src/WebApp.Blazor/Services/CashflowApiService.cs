using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class CashflowApiService(HttpClient httpClient) : ICashflowApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<CashflowListResult> GetEntriesAsync(
        CashflowListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildListUrl(query), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new CashflowListResult(CashflowApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CashflowListResult(CashflowApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new CashflowListResult(
                CashflowApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new CashflowListResult(CashflowApiStatus.Error);
        }

        var data = await response.Content.ReadFromJsonAsync<CashflowListResponseDto>(JsonOptions, cancellationToken);
        return data is null
            ? new CashflowListResult(CashflowApiStatus.Error)
            : new CashflowListResult(CashflowApiStatus.Success, data);
    }

    public async Task<CashflowDetailResult> GetEntryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"api/v1/cashflow/{id}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new CashflowDetailResult(CashflowApiStatus.Error);
        }

        return await MapDetailResponseAsync(response, cancellationToken);
    }

    public async Task<CashflowCreateResult> CreateEntryAsync(
        CashflowCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("api/v1/cashflow", request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new CashflowCreateResult(CashflowApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var data = await response.Content.ReadFromJsonAsync<CashflowCreateResponseDto>(JsonOptions, cancellationToken);
            return data is null
                ? new CashflowCreateResult(CashflowApiStatus.Error)
                : new CashflowCreateResult(CashflowApiStatus.Success, data);
        }

        return await MapCreateErrorAsync(response, cancellationToken);
    }

    public Task<CashflowMutationResult> UpdateEntryAsync(
        Guid id,
        CashflowUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Put, $"api/v1/cashflow/{id}", request, cancellationToken);

    public async Task<CashflowDeleteResult> DeleteEntryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.DeleteAsync($"api/v1/cashflow/{id}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new CashflowDeleteResult(CashflowApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return new CashflowDeleteResult(CashflowApiStatus.Success);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CashflowDeleteResult(CashflowApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new CashflowDeleteResult(
                CashflowApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        return new CashflowDeleteResult(CashflowApiStatus.Error);
    }

    public async Task<CashflowReportResult> GetReportAsync(
        CashflowReportQuery query,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildReportUrl(query), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new CashflowReportResult(CashflowApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CashflowReportResult(CashflowApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new CashflowReportResult(
                CashflowApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new CashflowReportResult(CashflowApiStatus.Error);
        }

        var data = await response.Content.ReadFromJsonAsync<CashflowReportResponseDto>(JsonOptions, cancellationToken);
        return data is null
            ? new CashflowReportResult(CashflowApiStatus.Error)
            : new CashflowReportResult(CashflowApiStatus.Success, data);
    }

    public async Task<CashflowInstallmentGroupResult> GetInstallmentGroupAsync(
        Guid compraId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"api/v1/cashflow/installments/{compraId}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new CashflowInstallmentGroupResult(CashflowApiStatus.Error);
        }

        if (response.IsSuccessStatusCode)
        {
            var entries = await response.Content.ReadFromJsonAsync<List<CashflowEntryDto>>(JsonOptions, cancellationToken);
            return entries is null
                ? new CashflowInstallmentGroupResult(CashflowApiStatus.Error)
                : new CashflowInstallmentGroupResult(CashflowApiStatus.Success, entries);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new CashflowInstallmentGroupResult(CashflowApiStatus.NotFound);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CashflowInstallmentGroupResult(CashflowApiStatus.Forbidden);
        }

        return new CashflowInstallmentGroupResult(CashflowApiStatus.Error);
    }

    internal static string BuildReportUrl(CashflowReportQuery query) =>
        $"api/v1/cashflow/report?month={query.Month}&year={query.Year}";

    internal static string BuildListUrl(CashflowListQuery? query)
    {
        if (query is null)
        {
            return "api/v1/cashflow";
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

        if (query.Type is not null)
        {
            parameters.Add($"type={JsonNamingPolicy.CamelCase.ConvertName(query.Type.Value.ToString())}");
        }

        if (query.Category is not null)
        {
            parameters.Add($"category={JsonNamingPolicy.CamelCase.ConvertName(query.Category.Value.ToString())}");
        }

        if (query.ProjectId is not null)
        {
            parameters.Add($"projectId={query.ProjectId.Value}");
        }

        if (query.DepartmentId is not null)
        {
            parameters.Add($"departmentId={query.DepartmentId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            parameters.Add($"search={Uri.EscapeDataString(query.Search)}");
        }

        if (query.Page is not null)
        {
            parameters.Add($"page={query.Page.Value}");
        }

        if (query.PageSize is not null)
        {
            parameters.Add($"pageSize={query.PageSize.Value}");
        }

        if (!query.IncludeFilterOptions)
        {
            parameters.Add("includeFilterOptions=false");
        }

        return parameters.Count == 0
            ? "api/v1/cashflow"
            : $"api/v1/cashflow?{string.Join("&", parameters)}";
    }

    private async Task<CashflowMutationResult> SendMutationAsync(
        HttpMethod method,
        string url,
        CashflowUpdateRequest request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = method == HttpMethod.Put
                ? await httpClient.PutAsJsonAsync(url, request, JsonOptions, cancellationToken)
                : throw new InvalidOperationException("Unsupported mutation method.");
        }
        catch (HttpRequestException)
        {
            return new CashflowMutationResult(CashflowApiStatus.Error);
        }

        return await MapMutationResponseAsync(response, cancellationToken);
    }

    private static async Task<CashflowDetailResult> MapDetailResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var entry = await response.Content.ReadFromJsonAsync<CashflowEntryDto>(JsonOptions, cancellationToken);
            return entry is null
                ? new CashflowDetailResult(CashflowApiStatus.Error)
                : new CashflowDetailResult(CashflowApiStatus.Success, entry);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CashflowDetailResult(CashflowApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new CashflowDetailResult(
                CashflowApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        return new CashflowDetailResult(CashflowApiStatus.Error);
    }

    private static async Task<CashflowMutationResult> MapMutationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var entry = await response.Content.ReadFromJsonAsync<CashflowEntryDto>(JsonOptions, cancellationToken);
            return entry is null
                ? new CashflowMutationResult(CashflowApiStatus.Error)
                : new CashflowMutationResult(CashflowApiStatus.Success, entry);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CashflowMutationResult(CashflowApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new CashflowMutationResult(
                CashflowApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new CashflowMutationResult(
                CashflowApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new CashflowMutationResult(CashflowApiStatus.Error);
    }

    private static async Task<CashflowCreateResult> MapCreateErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CashflowCreateResult(CashflowApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new CashflowCreateResult(
                CashflowApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new CashflowCreateResult(CashflowApiStatus.Error);
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

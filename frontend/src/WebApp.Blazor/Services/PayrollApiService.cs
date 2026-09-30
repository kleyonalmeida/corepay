using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class PayrollApiService(HttpClient httpClient) : IPayrollApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<PayrollListResult> GetPayrollsAsync(
        PayrollListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildListUrl(query), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollListResult(PayrollApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new PayrollListResult(
                PayrollApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PayrollListResult(PayrollApiStatus.Error);
        }

        string json;
        try
        {
            json = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollListResult(PayrollApiStatus.Error);
        }

        var payload = PaginatedListJsonParser.TryParse<PayrollListItemDto>(json, JsonOptions);
        return payload is null
            ? new PayrollListResult(PayrollApiStatus.Error)
            : new PayrollListResult(
                PayrollApiStatus.Success,
                payload.Items,
                payload.TotalCount,
                payload.Page,
                payload.PageSize);
    }

    public async Task<PayrollAccessResult> GetPayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"api/v1/payrolls/{id}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollAccessResult(PayrollAccessStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new PayrollAccessResult(PayrollAccessStatus.NotFound);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new PayrollAccessResult(PayrollAccessStatus.Forbidden);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PayrollAccessResult(PayrollAccessStatus.Error);
        }

        var payroll = await response.Content.ReadFromJsonAsync<PayrollDetailDto>(JsonOptions, cancellationToken);
        if (payroll is null)
        {
            return new PayrollAccessResult(PayrollAccessStatus.Error);
        }

        return new PayrollAccessResult(PayrollAccessStatus.Found, payroll);
    }

    public async Task<PayrollFormOptionsResult> GetFormOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync("api/v1/payrolls/form-options", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollFormOptionsResult(PayrollApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new PayrollFormOptionsResult(
                PayrollApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PayrollFormOptionsResult(PayrollApiStatus.Error);
        }

        var options = await response.Content.ReadFromJsonAsync<PayrollFormOptionsDto>(JsonOptions, cancellationToken);
        return options is null
            ? new PayrollFormOptionsResult(PayrollApiStatus.Error)
            : new PayrollFormOptionsResult(PayrollApiStatus.Success, options);
    }

    public async Task<PayrollMutationResult> CreatePayrollAsync(
        CreatePayrollRequestDto request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("api/v1/payrolls", request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollMutationResult(PayrollApiStatus.Error);
        }

        return await ReadMutationResultAsync(response, cancellationToken);
    }

    public async Task<PayrollMutationResult> UpdatePayrollAsync(
        Guid id,
        UpdatePayrollRequestDto request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PutAsJsonAsync($"api/v1/payrolls/{id}", request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollMutationResult(PayrollApiStatus.Error);
        }

        return await ReadMutationResultAsync(response, cancellationToken);
    }

    public async Task<PayrollEntryPreviewResult> PreviewEntryAsync(
        Guid payrollId,
        Guid entryId,
        PreviewPayrollEntryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(
                $"api/v1/payrolls/{payrollId}/entries/{entryId}/preview",
                request,
                JsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollEntryPreviewResult(PayrollApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new PayrollEntryPreviewResult(
                PayrollApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new PayrollEntryPreviewResult(
                PayrollApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new PayrollEntryPreviewResult(
                PayrollApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new PayrollEntryPreviewResult(
                PayrollApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PayrollEntryPreviewResult(PayrollApiStatus.Error);
        }

        var preview = await response.Content.ReadFromJsonAsync<PayrollEntryPreviewDto>(JsonOptions, cancellationToken);
        return preview is null
            ? new PayrollEntryPreviewResult(PayrollApiStatus.Error)
            : new PayrollEntryPreviewResult(PayrollApiStatus.Success, preview);
    }

    public async Task<PayrollMutationResult> SubmitPayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync($"api/v1/payrolls/{id}/submit", null, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollMutationResult(PayrollApiStatus.Error);
        }

        return await ReadMutationResultAsync(response, cancellationToken);
    }

    public Task<PayrollMutationResult> ApprovePayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        PostMutationAsync($"api/v1/payrolls/{id}/approve", cancellationToken);

    public async Task<PayrollMutationResult> RejectPayrollAsync(
        Guid id,
        RejectPayrollRequestDto request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(
                $"api/v1/payrolls/{id}/reject",
                request,
                JsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollMutationResult(PayrollApiStatus.Error);
        }

        return await ReadMutationResultAsync(response, cancellationToken);
    }

    public Task<PayrollMutationResult> RecalculatePayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        PostMutationAsync($"api/v1/payrolls/{id}/recalculate", cancellationToken);

    public async Task<PayrollDeleteResult> DeletePayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.DeleteAsync($"api/v1/payrolls/{id}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollDeleteResult(PayrollApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new PayrollDeleteResult(
                PayrollApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new PayrollDeleteResult(
                PayrollApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PayrollDeleteResult(PayrollApiStatus.Error);
        }

        return new PayrollDeleteResult(PayrollApiStatus.Success);
    }

    public Task<PayrollMutationResult> PayPayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        PostMutationAsync($"api/v1/payrolls/{id}/pay", cancellationToken);

    public async Task<PayrollMutationResult> AddCollaboratorEntryAsync(
        Guid payrollId,
        AddCollaboratorEntryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(
                $"api/v1/payrolls/{payrollId}/entries",
                request,
                JsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollMutationResult(PayrollApiStatus.Error);
        }

        return await ReadMutationResultAsync(response, cancellationToken);
    }

    public Task<PayrollMutationResult> ApproveEntryAsync(
        Guid payrollId,
        Guid entryId,
        CancellationToken cancellationToken = default) =>
        PostMutationAsync($"api/v1/payrolls/{payrollId}/entries/{entryId}/approve", cancellationToken);

    public async Task<PayrollMutationResult> PayEntryAsync(
        Guid payrollId,
        Guid entryId,
        SetEntryPaidRequestDto request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(
                $"api/v1/payrolls/{payrollId}/entries/{entryId}/pay",
                request,
                JsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollMutationResult(PayrollApiStatus.Error);
        }

        return await ReadMutationResultAsync(response, cancellationToken);
    }

    public async Task<PayrollMutationResult> SetEntryNfAsync(
        Guid payrollId,
        Guid entryId,
        SetEntryNfRequestDto request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PutAsJsonAsync(
                $"api/v1/payrolls/{payrollId}/entries/{entryId}/nf",
                request,
                JsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollMutationResult(PayrollApiStatus.Error);
        }

        return await ReadMutationResultAsync(response, cancellationToken);
    }

    private async Task<PayrollMutationResult> PostMutationAsync(
        string url,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync(url, null, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollMutationResult(PayrollApiStatus.Error);
        }

        return await ReadMutationResultAsync(response, cancellationToken);
    }

    public async Task<PayrollDuplicateResult> DuplicatePayrollAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync($"api/v1/payrolls/{id}/duplicate", null, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PayrollDuplicateResult(PayrollApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new PayrollDuplicateResult(
                PayrollApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new PayrollDuplicateResult(
                PayrollApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new PayrollDuplicateResult(
                PayrollApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PayrollDuplicateResult(PayrollApiStatus.Error);
        }

        var payroll = await response.Content.ReadFromJsonAsync<PayrollSummaryDto>(JsonOptions, cancellationToken);
        return payroll is null
            ? new PayrollDuplicateResult(PayrollApiStatus.Error)
            : new PayrollDuplicateResult(PayrollApiStatus.Success, payroll);
    }

    private static async Task<PayrollMutationResult> ReadMutationResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new PayrollMutationResult(
                PayrollApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new PayrollMutationResult(
                PayrollApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new PayrollMutationResult(
                PayrollApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new PayrollMutationResult(
                PayrollApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PayrollMutationResult(PayrollApiStatus.Error);
        }

        var payroll = await response.Content.ReadFromJsonAsync<PayrollDetailDto>(JsonOptions, cancellationToken);
        return payroll is null
            ? new PayrollMutationResult(PayrollApiStatus.Error)
            : new PayrollMutationResult(PayrollApiStatus.Success, payroll);
    }

    private static string BuildListUrl(PayrollListQuery? query)
    {
        if (query is null)
        {
            return "api/v1/payrolls";
        }

        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            parameters.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        if (query.Month is not null)
        {
            parameters.Add($"month={query.Month.Value}");
        }

        if (query.Year is not null)
        {
            parameters.Add($"year={query.Year.Value}");
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            parameters.Add($"status={Uri.EscapeDataString(query.Status)}");
        }

        if (query.DepartmentId is not null)
        {
            parameters.Add($"departmentId={query.DepartmentId.Value}");
        }

        if (query.Page is not null)
        {
            parameters.Add($"page={query.Page.Value}");
        }

        if (query.PageSize is not null)
        {
            parameters.Add($"pageSize={query.PageSize.Value}");
        }

        return parameters.Count == 0
            ? "api/v1/payrolls"
            : $"api/v1/payrolls?{string.Join('&', parameters)}";
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

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class TrafficInvestmentApiService(HttpClient httpClient) : ITrafficInvestmentApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<TrafficInvestmentListResult> GetInvestmentsAsync(
        TrafficInvestmentListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildInvestmentsUrl(query), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new TrafficInvestmentListResult(TrafficInvestmentApiStatus.Error);
        }

        return await MapInvestmentListResponseAsync(response, cancellationToken);
    }

    public async Task<TrafficInvestmentDetailResult> GetInvestmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"api/v1/traffic-investments/{id}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new TrafficInvestmentDetailResult(TrafficInvestmentApiStatus.Error);
        }

        return await MapInvestmentDetailResponseAsync(response, cancellationToken);
    }

    public Task<TrafficInvestmentMutationResult> CreateInvestmentAsync(
        TrafficInvestmentRequest request,
        CancellationToken cancellationToken = default) =>
        SendInvestmentMutationAsync(HttpMethod.Post, "api/v1/traffic-investments", request, cancellationToken);

    public Task<TrafficInvestmentMutationResult> UpdateInvestmentAsync(
        Guid id,
        TrafficInvestmentRequest request,
        CancellationToken cancellationToken = default) =>
        SendInvestmentMutationAsync(HttpMethod.Put, $"api/v1/traffic-investments/{id}", request, cancellationToken);

    public async Task<TrafficProjectDepositListResult> GetProjectDepositsAsync(
        TrafficProjectDepositListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildDepositsUrl(query), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new TrafficProjectDepositListResult(TrafficInvestmentApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new TrafficProjectDepositListResult(TrafficInvestmentApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new TrafficProjectDepositListResult(
                TrafficInvestmentApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new TrafficProjectDepositListResult(TrafficInvestmentApiStatus.Error);
        }

        var deposits = await response.Content.ReadFromJsonAsync<List<TrafficProjectDepositDto>>(JsonOptions, cancellationToken);
        return deposits is null
            ? new TrafficProjectDepositListResult(TrafficInvestmentApiStatus.Error)
            : new TrafficProjectDepositListResult(TrafficInvestmentApiStatus.Success, deposits);
    }

    public async Task<TrafficProjectDepositMutationResult> CreateProjectDepositAsync(
        TrafficProjectDepositRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("api/v1/traffic-deposits", request, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new TrafficProjectDepositMutationResult(TrafficInvestmentApiStatus.Error);
        }

        return await MapProjectDepositMutationResponseAsync(response, cancellationToken);
    }

    internal static string BuildInvestmentsUrl(TrafficInvestmentListQuery? query)
    {
        if (query is null)
        {
            return "api/v1/traffic-investments";
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

        if (query.ProjectId is not null)
        {
            parameters.Add($"projectId={query.ProjectId.Value}");
        }

        return parameters.Count == 0
            ? "api/v1/traffic-investments"
            : $"api/v1/traffic-investments?{string.Join("&", parameters)}";
    }

    internal static string BuildDepositsUrl(TrafficProjectDepositListQuery? query)
    {
        if (query is null)
        {
            return "api/v1/traffic-deposits";
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

        if (query.ProjectId is not null)
        {
            parameters.Add($"projectId={query.ProjectId.Value}");
        }

        return parameters.Count == 0
            ? "api/v1/traffic-deposits"
            : $"api/v1/traffic-deposits?{string.Join("&", parameters)}";
    }

    private async Task<TrafficInvestmentMutationResult> SendInvestmentMutationAsync(
        HttpMethod method,
        string url,
        TrafficInvestmentRequest request,
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
            return new TrafficInvestmentMutationResult(TrafficInvestmentApiStatus.Error);
        }

        return await MapInvestmentMutationResponseAsync(response, cancellationToken);
    }

    private static async Task<TrafficInvestmentListResult> MapInvestmentListResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var investments = await response.Content.ReadFromJsonAsync<List<TrafficInvestmentListItemDto>>(
                JsonOptions,
                cancellationToken);
            return investments is null
                ? new TrafficInvestmentListResult(TrafficInvestmentApiStatus.Error)
                : new TrafficInvestmentListResult(TrafficInvestmentApiStatus.Success, investments);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new TrafficInvestmentListResult(TrafficInvestmentApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new TrafficInvestmentListResult(
                TrafficInvestmentApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new TrafficInvestmentListResult(TrafficInvestmentApiStatus.Error);
    }

    private static async Task<TrafficInvestmentDetailResult> MapInvestmentDetailResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var investment = await response.Content.ReadFromJsonAsync<TrafficInvestmentDto>(JsonOptions, cancellationToken);
            return investment is null
                ? new TrafficInvestmentDetailResult(TrafficInvestmentApiStatus.Error)
                : new TrafficInvestmentDetailResult(TrafficInvestmentApiStatus.Success, investment);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new TrafficInvestmentDetailResult(TrafficInvestmentApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new TrafficInvestmentDetailResult(
                TrafficInvestmentApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        return new TrafficInvestmentDetailResult(TrafficInvestmentApiStatus.Error);
    }

    private static async Task<TrafficInvestmentMutationResult> MapInvestmentMutationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var investment = await response.Content.ReadFromJsonAsync<TrafficInvestmentDto>(JsonOptions, cancellationToken);
            return investment is null
                ? new TrafficInvestmentMutationResult(TrafficInvestmentApiStatus.Error)
                : new TrafficInvestmentMutationResult(TrafficInvestmentApiStatus.Success, investment);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new TrafficInvestmentMutationResult(TrafficInvestmentApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new TrafficInvestmentMutationResult(
                TrafficInvestmentApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new TrafficInvestmentMutationResult(
                TrafficInvestmentApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new TrafficInvestmentMutationResult(
                TrafficInvestmentApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new TrafficInvestmentMutationResult(TrafficInvestmentApiStatus.Error);
    }

    private static async Task<TrafficProjectDepositMutationResult> MapProjectDepositMutationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var deposit = await response.Content.ReadFromJsonAsync<TrafficProjectDepositDto>(JsonOptions, cancellationToken);
            return deposit is null
                ? new TrafficProjectDepositMutationResult(TrafficInvestmentApiStatus.Error)
                : new TrafficProjectDepositMutationResult(TrafficInvestmentApiStatus.Success, deposit);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new TrafficProjectDepositMutationResult(TrafficInvestmentApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new TrafficProjectDepositMutationResult(
                TrafficInvestmentApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new TrafficProjectDepositMutationResult(TrafficInvestmentApiStatus.Error);
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

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class CareerLevelApiService(HttpClient httpClient) : ICareerLevelApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<CareerLevelListResult> GetCareerLevelsAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync("api/v1/career-levels", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new CareerLevelListResult(CareerLevelApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CareerLevelListResult(CareerLevelApiStatus.Forbidden);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new CareerLevelListResult(CareerLevelApiStatus.Error);
        }

        var careerLevels = await response.Content.ReadFromJsonAsync<List<CareerLevelDto>>(JsonOptions, cancellationToken);
        return careerLevels is null
            ? new CareerLevelListResult(CareerLevelApiStatus.Error)
            : new CareerLevelListResult(CareerLevelApiStatus.Success, careerLevels);
    }

    public Task<CareerLevelMutationResult> CreateCareerLevelAsync(
        CareerLevelRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Post, "api/v1/career-levels", request, cancellationToken);

    public Task<CareerLevelMutationResult> UpdateCareerLevelAsync(
        Guid id,
        CareerLevelRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Put, $"api/v1/career-levels/{id}", request, cancellationToken);

    private async Task<CareerLevelMutationResult> SendMutationAsync(
        HttpMethod method,
        string url,
        CareerLevelRequest request,
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
            return new CareerLevelMutationResult(CareerLevelApiStatus.Error);
        }

        return await MapMutationResponseAsync(response, cancellationToken);
    }

    private static async Task<CareerLevelMutationResult> MapMutationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var careerLevel = await response.Content.ReadFromJsonAsync<CareerLevelDto>(JsonOptions, cancellationToken);
            return careerLevel is null
                ? new CareerLevelMutationResult(CareerLevelApiStatus.Error)
                : new CareerLevelMutationResult(CareerLevelApiStatus.Success, careerLevel);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CareerLevelMutationResult(CareerLevelApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new CareerLevelMutationResult(
                CareerLevelApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new CareerLevelMutationResult(
                CareerLevelApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new CareerLevelMutationResult(
                CareerLevelApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new CareerLevelMutationResult(CareerLevelApiStatus.Error);
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

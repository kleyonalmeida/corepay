using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class DepartmentApiService(HttpClient httpClient) : IDepartmentApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<DepartmentListResult> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync("api/v1/departments", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new DepartmentListResult(DepartmentApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new DepartmentListResult(DepartmentApiStatus.Forbidden);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new DepartmentListResult(DepartmentApiStatus.Error);
        }

        var departments = await response.Content.ReadFromJsonAsync<List<DepartmentDto>>(JsonOptions, cancellationToken);
        return departments is null
            ? new DepartmentListResult(DepartmentApiStatus.Error)
            : new DepartmentListResult(DepartmentApiStatus.Success, departments);
    }

    public Task<DepartmentMutationResult> CreateDepartmentAsync(
        DepartmentRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Post, "api/v1/departments", request, cancellationToken);

    public Task<DepartmentMutationResult> UpdateDepartmentAsync(
        Guid id,
        DepartmentRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Put, $"api/v1/departments/{id}", request, cancellationToken);

    private async Task<DepartmentMutationResult> SendMutationAsync(
        HttpMethod method,
        string url,
        DepartmentRequest request,
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
            return new DepartmentMutationResult(DepartmentApiStatus.Error);
        }

        return await MapMutationResponseAsync(response, cancellationToken);
    }

    private static async Task<DepartmentMutationResult> MapMutationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var department = await response.Content.ReadFromJsonAsync<DepartmentDto>(JsonOptions, cancellationToken);
            return department is null
                ? new DepartmentMutationResult(DepartmentApiStatus.Error)
                : new DepartmentMutationResult(DepartmentApiStatus.Success, department);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new DepartmentMutationResult(DepartmentApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new DepartmentMutationResult(
                DepartmentApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new DepartmentMutationResult(
                DepartmentApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new DepartmentMutationResult(
                DepartmentApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new DepartmentMutationResult(DepartmentApiStatus.Error);
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

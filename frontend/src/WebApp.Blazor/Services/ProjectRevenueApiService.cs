using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Blazor.Services;

public sealed class ProjectRevenueApiService(HttpClient httpClient) : IProjectRevenueApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ProjectRevenueListResult> GetRevenuesAsync(
        ProjectRevenueListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildListUrl(query), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new ProjectRevenueListResult(ProjectRevenueApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ProjectRevenueListResult(ProjectRevenueApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new ProjectRevenueListResult(
                ProjectRevenueApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ProjectRevenueListResult(ProjectRevenueApiStatus.Error);
        }

        var revenues = await response.Content.ReadFromJsonAsync<List<ProjectRevenueDto>>(JsonOptions, cancellationToken);
        return revenues is null
            ? new ProjectRevenueListResult(ProjectRevenueApiStatus.Error)
            : new ProjectRevenueListResult(ProjectRevenueApiStatus.Success, revenues);
    }

    public async Task<ProjectRevenueDetailResult> GetRevenueByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"api/v1/project-revenues/{id}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new ProjectRevenueDetailResult(ProjectRevenueApiStatus.Error);
        }

        return await MapDetailResponseAsync(response, cancellationToken);
    }

    public Task<ProjectRevenueMutationResult> CreateRevenueAsync(
        ProjectRevenueRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Post, "api/v1/project-revenues", request, cancellationToken);

    public Task<ProjectRevenueMutationResult> UpdateRevenueAsync(
        Guid id,
        ProjectRevenueRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Put, $"api/v1/project-revenues/{id}", request, cancellationToken);

    internal static string BuildListUrl(ProjectRevenueListQuery? query)
    {
        if (query is null)
        {
            return "api/v1/project-revenues";
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
            ? "api/v1/project-revenues"
            : $"api/v1/project-revenues?{string.Join("&", parameters)}";
    }

    private async Task<ProjectRevenueMutationResult> SendMutationAsync(
        HttpMethod method,
        string url,
        ProjectRevenueRequest request,
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
            return new ProjectRevenueMutationResult(ProjectRevenueApiStatus.Error);
        }

        return await MapMutationResponseAsync(response, cancellationToken);
    }

    private static async Task<ProjectRevenueDetailResult> MapDetailResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var revenue = await response.Content.ReadFromJsonAsync<ProjectRevenueDto>(JsonOptions, cancellationToken);
            return revenue is null
                ? new ProjectRevenueDetailResult(ProjectRevenueApiStatus.Error)
                : new ProjectRevenueDetailResult(ProjectRevenueApiStatus.Success, revenue);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ProjectRevenueDetailResult(ProjectRevenueApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new ProjectRevenueDetailResult(
                ProjectRevenueApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        return new ProjectRevenueDetailResult(ProjectRevenueApiStatus.Error);
    }

    private static async Task<ProjectRevenueMutationResult> MapMutationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var revenue = await response.Content.ReadFromJsonAsync<ProjectRevenueDto>(JsonOptions, cancellationToken);
            return revenue is null
                ? new ProjectRevenueMutationResult(ProjectRevenueApiStatus.Error)
                : new ProjectRevenueMutationResult(ProjectRevenueApiStatus.Success, revenue);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ProjectRevenueMutationResult(ProjectRevenueApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new ProjectRevenueMutationResult(
                ProjectRevenueApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new ProjectRevenueMutationResult(
                ProjectRevenueApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new ProjectRevenueMutationResult(
                ProjectRevenueApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new ProjectRevenueMutationResult(ProjectRevenueApiStatus.Error);
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

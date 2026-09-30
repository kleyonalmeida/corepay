using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class ProjectApiService(HttpClient httpClient) : IProjectApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<ProjectListResult> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync("api/v1/projects", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new ProjectListResult(ProjectApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ProjectListResult(ProjectApiStatus.Forbidden);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ProjectListResult(ProjectApiStatus.Error);
        }

        var projects = await response.Content.ReadFromJsonAsync<List<ProjectDto>>(JsonOptions, cancellationToken);
        return projects is null
            ? new ProjectListResult(ProjectApiStatus.Error)
            : new ProjectListResult(ProjectApiStatus.Success, projects);
    }

    public Task<ProjectMutationResult> CreateProjectAsync(
        ProjectRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Post, "api/v1/projects", request, cancellationToken);

    public Task<ProjectMutationResult> UpdateProjectAsync(
        Guid id,
        ProjectRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Put, $"api/v1/projects/{id}", request, cancellationToken);

    private async Task<ProjectMutationResult> SendMutationAsync(
        HttpMethod method,
        string url,
        ProjectRequest request,
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
            return new ProjectMutationResult(ProjectApiStatus.Error);
        }

        return await MapMutationResponseAsync(response, cancellationToken);
    }

    private static async Task<ProjectMutationResult> MapMutationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var project = await response.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions, cancellationToken);
            return project is null
                ? new ProjectMutationResult(ProjectApiStatus.Error)
                : new ProjectMutationResult(ProjectApiStatus.Success, project);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ProjectMutationResult(ProjectApiStatus.Forbidden);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new ProjectMutationResult(
                ProjectApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new ProjectMutationResult(
                ProjectApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new ProjectMutationResult(
                ProjectApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new ProjectMutationResult(ProjectApiStatus.Error);
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

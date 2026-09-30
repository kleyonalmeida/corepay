using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Blazor.Services;

public sealed class CollaboratorApiService(HttpClient httpClient) : ICollaboratorApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<CollaboratorListResult> GetCollaboratorsAsync(
        CollaboratorListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(BuildListUrl(query), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new CollaboratorListResult(CollaboratorApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CollaboratorListResult(CollaboratorApiStatus.Forbidden);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new CollaboratorListResult(CollaboratorApiStatus.Error);
        }

        string json;
        try
        {
            json = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new CollaboratorListResult(CollaboratorApiStatus.Error);
        }

        return TryParseCollaboratorsList(json)
            ?? new CollaboratorListResult(CollaboratorApiStatus.Error);
    }

    public async Task<CollaboratorGetResult> GetCollaboratorByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"api/v1/collaborators/{id}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new CollaboratorGetResult(CollaboratorApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new CollaboratorGetResult(
                CollaboratorApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new CollaboratorGetResult(
                CollaboratorApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new CollaboratorGetResult(CollaboratorApiStatus.Error);
        }

        var collaborator = await response.Content.ReadFromJsonAsync<CollaboratorDto>(JsonOptions, cancellationToken);
        return collaborator is null
            ? new CollaboratorGetResult(CollaboratorApiStatus.Error)
            : new CollaboratorGetResult(CollaboratorApiStatus.Success, collaborator);
    }

    public Task<CollaboratorMutationResult> CreateCollaboratorAsync(
        CollaboratorRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Post, "api/v1/collaborators", request, cancellationToken);

    public Task<CollaboratorMutationResult> UpdateCollaboratorAsync(
        Guid id,
        CollaboratorRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Put, $"api/v1/collaborators/{id}", request, cancellationToken);

    private async Task<CollaboratorMutationResult> SendMutationAsync(
        HttpMethod method,
        string url,
        CollaboratorRequest request,
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
            return new CollaboratorMutationResult(CollaboratorApiStatus.Error);
        }

        return await MapMutationResponseAsync(response, cancellationToken);
    }

    private static async Task<CollaboratorMutationResult> MapMutationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var collaborator = await response.Content.ReadFromJsonAsync<CollaboratorDto>(JsonOptions, cancellationToken);
            return collaborator is null
                ? new CollaboratorMutationResult(CollaboratorApiStatus.Error)
                : new CollaboratorMutationResult(CollaboratorApiStatus.Success, collaborator);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new CollaboratorMutationResult(
                CollaboratorApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new CollaboratorMutationResult(
                CollaboratorApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new CollaboratorMutationResult(
                CollaboratorApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new CollaboratorMutationResult(CollaboratorApiStatus.Error);
    }

    private static CollaboratorListResult? TryParseCollaboratorsList(string json)
    {
        var payload = PaginatedListJsonParser.TryParse<CollaboratorDto>(json, JsonOptions);
        return payload is null
            ? null
            : new CollaboratorListResult(
                CollaboratorApiStatus.Success,
                payload.Items,
                payload.TotalCount,
                payload.Page,
                payload.PageSize);
    }

    private static string BuildListUrl(CollaboratorListQuery? query)
    {
        if (query is null)
        {
            return "api/v1/collaborators";
        }

        var parameters = new List<string>();
        if (query.DepartmentId is not null)
        {
            parameters.Add($"departmentId={query.DepartmentId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            parameters.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        if (query.IsActive is not null)
        {
            parameters.Add($"isActive={(query.IsActive.Value ? "true" : "false")}");
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
            ? "api/v1/collaborators"
            : $"api/v1/collaborators?{string.Join('&', parameters)}";
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

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Blazor.Services;

public sealed class UserApiService(HttpClient httpClient) : IUserApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<UserListResult> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync("api/v1/users", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new UserListResult(UserApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new UserListResult(UserApiStatus.Forbidden);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new UserListResult(UserApiStatus.Error);
        }

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(JsonOptions, cancellationToken);
        return users is null
            ? new UserListResult(UserApiStatus.Error)
            : new UserListResult(UserApiStatus.Success, users);
    }

    public Task<UserMutationResult> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Post, "api/v1/users", request, cancellationToken);

    public Task<UserMutationResult> UpdateUserAsync(
        string id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Put, $"api/v1/users/{id}", request, cancellationToken);

    public async Task<UserDeleteResult> DeleteUserAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.DeleteAsync($"api/v1/users/{id}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new UserDeleteResult(UserApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return new UserDeleteResult(UserApiStatus.Success);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new UserDeleteResult(
                UserApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new UserDeleteResult(
                UserApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        return new UserDeleteResult(UserApiStatus.Error);
    }

    private async Task<UserMutationResult> SendMutationAsync(
        HttpMethod method,
        string url,
        object request,
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
            return new UserMutationResult(UserApiStatus.Error);
        }

        return await MapMutationResponseAsync(response, cancellationToken);
    }

    private static async Task<UserMutationResult> MapMutationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions, cancellationToken);
            return user is null
                ? new UserMutationResult(UserApiStatus.Error)
                : new UserMutationResult(UserApiStatus.Success, user);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var forbidden = await ReadErrorAsync(response, cancellationToken);
            return new UserMutationResult(
                UserApiStatus.Forbidden,
                ErrorCode: forbidden?.Error,
                Message: forbidden?.Message);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = await ReadErrorAsync(response, cancellationToken);
            return new UserMutationResult(
                UserApiStatus.NotFound,
                ErrorCode: notFound?.Error,
                Message: notFound?.Message);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await ReadErrorAsync(response, cancellationToken);
            return new UserMutationResult(
                UserApiStatus.Conflict,
                ErrorCode: conflict?.Error,
                Message: conflict?.Message);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validation = await ReadErrorAsync(response, cancellationToken);
            return new UserMutationResult(
                UserApiStatus.ValidationError,
                ErrorCode: validation?.Error,
                Message: validation?.Message);
        }

        return new UserMutationResult(UserApiStatus.Error);
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

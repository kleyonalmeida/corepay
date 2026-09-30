using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WebApp.Blazor.Services;

public sealed class RoleApiService(HttpClient httpClient) : IRoleApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<RoleListResult> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync("api/v1/roles", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new RoleListResult(RoleApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new RoleListResult(RoleApiStatus.Forbidden);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new RoleListResult(RoleApiStatus.Error);
        }

        var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>(JsonOptions, cancellationToken);
        return roles is null
            ? new RoleListResult(RoleApiStatus.Error)
            : new RoleListResult(RoleApiStatus.Success, roles);
    }

    public async Task<PermissionListResult> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync("api/v1/permissions", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new PermissionListResult(RoleApiStatus.Error);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new PermissionListResult(RoleApiStatus.Forbidden);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new PermissionListResult(RoleApiStatus.Error);
        }

        var permissions = await response.Content.ReadFromJsonAsync<List<PermissionDto>>(JsonOptions, cancellationToken);
        return permissions is null
            ? new PermissionListResult(RoleApiStatus.Error)
            : new PermissionListResult(RoleApiStatus.Success, permissions);
    }

    public Task<RoleMutationResult> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Post, "api/v1/roles", request, cancellationToken);

    public Task<RoleMutationResult> UpdateRoleAsync(
        string id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync(HttpMethod.Put, $"api/v1/roles/{id}", request, cancellationToken);

    private async Task<RoleMutationResult> SendMutationAsync(
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
            return new RoleMutationResult(RoleApiStatus.Error);
        }

        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
        {
            var role = await response.Content.ReadFromJsonAsync<RoleDto>(JsonOptions, cancellationToken);
            return role is null
                ? new RoleMutationResult(RoleApiStatus.Error)
                : new RoleMutationResult(RoleApiStatus.Success, role);
        }

        var error = await ReadErrorAsync(response, cancellationToken);
        var status = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => RoleApiStatus.ValidationError,
            HttpStatusCode.NotFound => RoleApiStatus.NotFound,
            HttpStatusCode.Conflict => RoleApiStatus.Conflict,
            HttpStatusCode.Forbidden => RoleApiStatus.Forbidden,
            _ => RoleApiStatus.Error
        };

        return new RoleMutationResult(status, ErrorCode: error?.Error, Message: error?.Message);
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

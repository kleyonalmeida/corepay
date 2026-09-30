using System.Net;
using System.Net.Http.Json;
using Core.Domain;
using FluentAssertions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.MasterData;

[Collection("WebApiIntegration")]
public class ProjectsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public ProjectsEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    [Fact]
    public async Task CreateProject_ShouldPersistPlatformAndFlags()
    {
        var client = await CreateAdminClientAsync();
        var payload = MasterDataTestHelper.CreateProjectPayload(
            name: $"Hubla {Guid.NewGuid():N}",
            client: "Cliente Hubla",
            platform: "hubla",
            isDefaultAllocationTarget: true);

        var createResponse = await client.PostAsJsonAsync("/api/v1/projects", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ProjectApiResponse>(MasterDataJsonOptions.Instance);
        created.Should().NotBeNull();
        created!.Platform.Should().Be(ProjectPlatform.Hubla);
        created.Client.Should().Be("Cliente Hubla");
        created.IsDefaultAllocationTarget.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProject_WithInvalidPlatform_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var payload = MasterDataTestHelper.CreateProjectPayload(
            name: $"BadPlatform {Guid.NewGuid():N}",
            platform: "stripe");

        var response = await client.PostAsJsonAsync("/api/v1/projects", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProject_WithNumericPlatform_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        using var content = new StringContent(
            $$"""{"name":"NumProject","client":"C","platform":0,"isActive":true,"isDefaultAllocationTarget":false,"excludesGoalBonus":false,"excludesSupervisorFixedAllocation":false}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/projects", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record ProjectApiResponse(
        Guid Id,
        string Name,
        string? Client,
        ProjectPlatform Platform,
        bool IsActive,
        bool IsDefaultAllocationTarget,
        bool ExcludesGoalBonus,
        bool ExcludesSupervisorFixedAllocation);
}

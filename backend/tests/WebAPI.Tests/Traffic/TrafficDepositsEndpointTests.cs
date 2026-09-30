using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebAPI.Tests.Common;
using WebAPI.Tests.MasterData;

namespace WebAPI.Tests.Traffic;

[Collection("WebApiIntegration")]
public class TrafficDepositsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public TrafficDepositsEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTrafficDeposit_ShouldReturnCreated()
    {
        var client = await CreateAdminClientAsync();
        var projectId = await CreateProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/traffic-deposits",
            TrafficInvestmentsTestHelper.CreateProjectDepositPayload(projectId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<TrafficProjectDepositApiResponse>();
        created.Should().NotBeNull();
        created!.Amount.Should().Be(500m);
        created.DepositDate.Should().Be(new DateOnly(2026, 9, 15));
    }

    [Fact]
    public async Task GetTrafficDeposits_ShouldFilterByMonthAndYear()
    {
        var client = await CreateAdminClientAsync();
        var projectId = await CreateProjectAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/traffic-deposits",
            TrafficInvestmentsTestHelper.CreateProjectDepositPayload(projectId));
        createResponse.EnsureSuccessStatusCode();

        var listResponse = await client.GetAsync(
            $"/api/v1/traffic-deposits?month=9&year=2026&projectId={projectId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<List<TrafficProjectDepositApiResponse>>();
        list.Should().NotBeNull();
        list!.Should().ContainSingle();
        list[0].Amount.Should().Be(500m);
    }

    [Fact]
    public async Task CreateTrafficDeposit_WithZeroAmount_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var projectId = await CreateProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/traffic-deposits",
            TrafficInvestmentsTestHelper.CreateProjectDepositPayload(projectId, amount: 0m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("trafficdeposits.amount_required");
    }

    [Fact]
    public async Task CreateTrafficDeposit_WithDirectorToken_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.PostAsJsonAsync(
            "/api/v1/traffic-deposits",
            TrafficInvestmentsTestHelper.CreateProjectDepositPayload(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<Guid> CreateProjectAsync()
    {
        var adminClient = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(adminClient, token);

        var payload = MasterDataTestHelper.CreateProjectPayload(name: $"Projeto {Guid.NewGuid():N}");
        var response = await adminClient.PostAsJsonAsync("/api/v1/projects", payload);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<ProjectApiResponse>();
        return created!.Id;
    }

    private sealed record ProjectApiResponse(Guid Id, string Name);

    private sealed record TrafficProjectDepositApiResponse(
        Guid Id,
        Guid ProjectId,
        string ProjectName,
        DateOnly DepositDate,
        decimal Amount,
        string? Notes);

    private sealed record ApiErrorResponse(string? Error, string? Message);
}

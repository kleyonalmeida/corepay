using System.Net;
using System.Net.Http.Json;
using Core.Domain;
using FluentAssertions;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.MasterData;

[Collection("WebApiIntegration")]
public class DepartmentsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public DepartmentsEndpointTests(CorePayWebApplicationFactory factory)
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
    public async Task CreateDepartment_ShouldPersistAndRoundTrip()
    {
        var client = await CreateAdminClientAsync();
        var payload = MasterDataTestHelper.CreateDepartmentPayload(
            name: $"Gerência {Guid.NewGuid():N}",
            calculationType: "management");

        var createResponse = await client.PostAsJsonAsync("/api/v1/departments", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<DepartmentApiResponse>(MasterDataJsonOptions.Instance);
        created.Should().NotBeNull();
        Guid.TryParse(created!.Id.ToString(), out _).Should().BeTrue();
        created.Name.Should().Contain("Gerência");
        created.CalculationType.Should().Be(CalculationProfile.Management);
        created.LowRevenueThreshold.Should().Be(200_000m);
        created.LowRevenueBonusPct.Should().Be(0.4m);

        var getResponse = await client.GetAsync($"/api/v1/departments/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateDepartment_WithDuplicateName_ShouldReturnConflict()
    {
        var client = await CreateAdminClientAsync();
        var name = $"DupDept-{Guid.NewGuid():N}";
        var payload = MasterDataTestHelper.CreateDepartmentPayload(name: name);

        (await client.PostAsJsonAsync("/api/v1/departments", payload)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/v1/departments", payload)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateDepartment_WithEmptyName_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var payload = MasterDataTestHelper.CreateDepartmentPayload(name: "   ");

        var response = await client.PostAsJsonAsync("/api/v1/departments", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDepartmentById_WhenMissing_ShouldReturnNotFound()
    {
        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/v1/departments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateDepartment_WithInvalidCalculationType_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var payload = MasterDataTestHelper.CreateDepartmentPayload(
            name: $"Invalid-{Guid.NewGuid():N}",
            calculationType: "not-a-profile");

        var response = await client.PostAsJsonAsync("/api/v1/departments", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDepartment_WithNumericCalculationType_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        using var content = new StringContent(
            $$"""{"name":"NumDept","calculationType":0,"goalBonusPercentage":0,"lowRevenueThreshold":200000,"lowRevenueBonusPct":0.4,"isActive":true,"isAllocatedFixed":false,"routesFixedToLimaKarttos":false}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/departments", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record DepartmentApiResponse(
        Guid Id,
        string Name,
        CalculationProfile CalculationType,
        decimal GoalBonusPercentage,
        decimal LowRevenueThreshold,
        decimal LowRevenueBonusPct,
        string? Description,
        bool IsActive,
        bool IsAllocatedFixed,
        bool RoutesFixedToLimaKarttos);
}

using System.Net;
using System.Net.Http.Json;
using Core.Domain;
using FluentAssertions;
using Infrastructure.Seed;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.MasterData;

[Collection("WebApiIntegration")]
public class CareerLevelsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public CareerLevelsEndpointTests(CorePayWebApplicationFactory factory)
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
    public async Task CreateCommercialAnalystJunior_ShouldPersistCommercialFields()
    {
        var client = await CreateAdminClientAsync();
        var departmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var payload = MasterDataTestHelper.CreateCareerLevelPayload(
            name: $"Analista {Guid.NewGuid():N}",
            departmentId: departmentId,
            profile: "commercialAnalyst",
            ftdRateBase: 2m,
            salesPctBase: 4m,
            ftdBonusEvery: 250,
            ftdBonusValue: 350m);

        var createResponse = await client.PostAsJsonAsync("/api/v1/career-levels", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CareerLevelApiResponse>(MasterDataJsonOptions.Instance);
        created.Should().NotBeNull();
        created!.FtdRateBase.Should().Be(2m);
        created.SalesPctBase.Should().Be(4m);
        created.FtdBonusEvery.Should().Be(250);
        created.FtdBonusValue.Should().Be(350m);
        created.Profile.Should().Be(CalculationProfile.CommercialAnalyst);
    }

    [Fact]
    public async Task CreateCareerLevel_WithUnknownDepartment_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var payload = MasterDataTestHelper.CreateCareerLevelPayload(
            name: $"Orphan {Guid.NewGuid():N}",
            departmentId: Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/v1/career-levels", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCareerLevel_WithInvalidProfile_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var payload = MasterDataTestHelper.CreateCareerLevelPayload(
            name: $"Invalid {Guid.NewGuid():N}",
            profile: "unknownProfile");

        var response = await client.PostAsJsonAsync("/api/v1/career-levels", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCareerLevelById_WhenMissing_ShouldReturnNotFound()
    {
        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/v1/career-levels/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record CareerLevelApiResponse(
        Guid Id,
        string Name,
        Guid? DepartmentId,
        CalculationProfile Profile,
        bool IsActive,
        decimal BaseSalary,
        decimal FtdRateBase,
        decimal SalesPctBase,
        int FtdBonusEvery,
        decimal FtdBonusValue);
}

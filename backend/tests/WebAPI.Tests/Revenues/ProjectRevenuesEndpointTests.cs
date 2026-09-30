using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;
using WebAPI.Tests.MasterData;

namespace WebAPI.Tests.Revenues;

[Collection("WebApiIntegration")]
public class ProjectRevenuesEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public ProjectRevenuesEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProjectRevenue_ShouldCalculateValueAsSum()
    {
        var client = await CreateFinancialClientAsync();
        var projectId = await CreateProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/project-revenues",
            ProjectRevenuesTestHelper.CreatePayload(projectId, valueIgaming: 1000m, valueVendas: 500m));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ProjectRevenueApiResponse>();
        created.Should().NotBeNull();
        created!.ValueIgaming.Should().Be(1000m);
        created.ValueVendas.Should().Be(500m);
        created.Value.Should().Be(1500m);
        created.Month.Should().Be(9);
        created.Year.Should().Be(2026);
    }

    [Fact]
    public async Task GetProjectRevenues_ShouldFilterByMonthAndYear()
    {
        var client = await CreateFinancialClientAsync();
        var projectId = await CreateProjectAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/project-revenues",
            ProjectRevenuesTestHelper.CreatePayload(projectId, month: 8, year: 2026));
        createResponse.EnsureSuccessStatusCode();

        var listResponse = await client.GetAsync("/api/v1/project-revenues?month=8&year=2026");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<List<ProjectRevenueApiResponse>>();
        list.Should().NotBeNull();
        list!.Should().ContainSingle();
        list[0].Value.Should().Be(1500m);
    }

    [Fact]
    public async Task GetProjectRevenueById_ShouldReturnRecord()
    {
        var client = await CreateFinancialClientAsync();
        var projectId = await CreateProjectAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/project-revenues",
            ProjectRevenuesTestHelper.CreatePayload(projectId));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<ProjectRevenueApiResponse>();
        created.Should().NotBeNull();

        var getResponse = await client.GetAsync($"/api/v1/project-revenues/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ProjectRevenueApiResponse>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.Value.Should().Be(1500m);
    }

    [Fact]
    public async Task UpdateProjectRevenue_ShouldRecalculateValue()
    {
        var client = await CreateFinancialClientAsync();
        var projectId = await CreateProjectAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/project-revenues",
            ProjectRevenuesTestHelper.CreatePayload(projectId));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<ProjectRevenueApiResponse>();
        created.Should().NotBeNull();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/project-revenues/{created!.Id}",
            ProjectRevenuesTestHelper.CreateUpdatePayload(
                projectId,
                valueIgaming: 2000m,
                valueVendas: 300m,
                groupPercentage: 12.5m,
                notes: "Atualizado"));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ProjectRevenueApiResponse>();
        updated.Should().NotBeNull();
        updated!.Value.Should().Be(2300m);
        updated.GroupPercentage.Should().Be(12.5m);
        updated.Notes.Should().Be("Atualizado");
    }

    [Fact]
    public async Task CreateProjectRevenue_WithDuplicateCompetence_ShouldReturnConflict()
    {
        var client = await CreateFinancialClientAsync();
        var projectId = await CreateProjectAsync();

        var payload = ProjectRevenuesTestHelper.CreatePayload(projectId);
        var first = await client.PostAsJsonAsync("/api/v1/project-revenues", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/api/v1/project-revenues", payload);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateProjectRevenue_WithUnknownProject_ShouldReturnBadRequest()
    {
        var client = await CreateFinancialClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/project-revenues",
            ProjectRevenuesTestHelper.CreatePayload(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProjectRevenue_WithZeroValues_ShouldReturnBadRequest()
    {
        var client = await CreateFinancialClientAsync();
        var projectId = await CreateProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/project-revenues",
            ProjectRevenuesTestHelper.CreatePayload(projectId, valueIgaming: 0m, valueVendas: 0m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Error.Should().Be("projectrevenues.values_required");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.AppDbContext>();
        var count = await dbContext.ProjectRevenues.CountAsync(r =>
            r.ProjectId == projectId && r.Month == 9 && r.Year == 2026);
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreateProjectRevenue_WithNegativeValues_ShouldReturnBadRequest()
    {
        var client = await CreateFinancialClientAsync();
        var projectId = await CreateProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/project-revenues",
            ProjectRevenuesTestHelper.CreatePayload(projectId, valueIgaming: -1m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProjectRevenue_WithInvalidGroupPercentage_ShouldReturnBadRequest()
    {
        var client = await CreateFinancialClientAsync();
        var projectId = await CreateProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/project-revenues",
            ProjectRevenuesTestHelper.CreatePayload(projectId, groupPercentage: 101m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProjectRevenues_WithInvalidMonth_ShouldReturnBadRequest()
    {
        var client = await CreateFinancialClientAsync();

        var response = await client.GetAsync("/api/v1/project-revenues?month=13");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProjectRevenues_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/project-revenues");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjectRevenues_WithManagerToken_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/project-revenues");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProjectRevenue_WithManagerToken_ShouldReturnCreated()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        var projectId = await CreateProjectAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/project-revenues",
            ProjectRevenuesTestHelper.CreatePayload(projectId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProjectRevenue_WithDirectorToken_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.PostAsJsonAsync(
            "/api/v1/project-revenues",
            ProjectRevenuesTestHelper.CreatePayload(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProjectRevenues_WithDirectorToken_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/project-revenues");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> CreateFinancialClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateFinancialUserAsync(_factory);
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

    private sealed record ProjectRevenueApiResponse(
        Guid Id,
        Guid ProjectId,
        string ProjectName,
        int Month,
        int Year,
        decimal ValueIgaming,
        decimal ValueVendas,
        decimal Value,
        decimal GroupPercentage,
        string? Notes);

    private sealed record ApiErrorResponse(string? Error, string? Message);
}

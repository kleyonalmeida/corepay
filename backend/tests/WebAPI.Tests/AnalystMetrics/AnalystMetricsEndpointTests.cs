using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebAPI.Tests.Collaborators;
using WebAPI.Tests.Common;
using WebAPI.Tests.MasterData;

namespace WebAPI.Tests.AnalystMetrics;

[Collection("WebApiIntegration")]
public class AnalystMetricsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public AnalystMetricsEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAndUpdate_ShouldPersistIntegerCounts()
    {
        var admin = await CreateAdminClientAsync();
        var departmentId = await CreateDepartmentAsync(admin);
        var collaboratorId = await CreateCollaboratorAsync(admin, departmentId);
        var projectId = await CreateProjectAsync(admin);

        var createResponse = await admin.PostAsJsonAsync(
            "/api/v1/analyst-metrics",
            AnalystMetricsTestHelper.CreatePayload(collaboratorId, projectId));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<AnalystMetricApiResponse>();
        created.Should().NotBeNull();
        created!.FtdTotal.Should().Be(120);
        created.CpaCount.Should().Be(35);
        created.DepartmentId.Should().Be(departmentId);

        var updateResponse = await admin.PutAsJsonAsync(
            $"/api/v1/analyst-metrics/{created.Id}",
            AnalystMetricsTestHelper.CreatePayload(
                collaboratorId,
                projectId,
                ftdTotal: 150,
                cpaCount: 42));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AnalystMetricApiResponse>();
        updated!.FtdTotal.Should().Be(150);
        updated.CpaCount.Should().Be(42);
    }

    [Fact]
    public async Task List_ShouldFilterByCompetenceDepartmentCollaboratorAndProject()
    {
        var admin = await CreateAdminClientAsync();
        var departmentId = await CreateDepartmentAsync(admin);
        var collaboratorId = await CreateCollaboratorAsync(admin, departmentId);
        var projectId = await CreateProjectAsync(admin);
        (await admin.PostAsJsonAsync(
            "/api/v1/analyst-metrics",
            AnalystMetricsTestHelper.CreatePayload(collaboratorId, projectId, month: 8)))
            .EnsureSuccessStatusCode();

        var response = await admin.GetAsync(
            $"/api/v1/analyst-metrics?month=8&year=2026&departmentId={departmentId}&collaboratorId={collaboratorId}&projectId={projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<AnalystMetricApiResponse>>();
        items.Should().ContainSingle(item =>
            item.CollaboratorId == collaboratorId && item.ProjectId == projectId && item.Month == 8);
    }

    [Fact]
    public async Task CreateDuplicate_ShouldReturnConflict()
    {
        var admin = await CreateAdminClientAsync();
        var departmentId = await CreateDepartmentAsync(admin);
        var collaboratorId = await CreateCollaboratorAsync(admin, departmentId);
        var projectId = await CreateProjectAsync(admin);
        var payload = AnalystMetricsTestHelper.CreatePayload(collaboratorId, projectId);

        (await admin.PostAsJsonAsync("/api/v1/analyst-metrics", payload)).EnsureSuccessStatusCode();
        var duplicate = await admin.PostAsJsonAsync("/api/v1/analyst-metrics", payload);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(0, 2026, 1, 1)]
    [InlineData(13, 2026, 1, 1)]
    [InlineData(9, 1999, 1, 1)]
    [InlineData(9, 2026, -1, 1)]
    [InlineData(9, 2026, 1, -1)]
    public async Task CreateWithInvalidValues_ShouldReturnBadRequest(
        int month,
        int year,
        int ftdTotal,
        int cpaCount)
    {
        var admin = await CreateAdminClientAsync();
        var response = await admin.PostAsJsonAsync(
            "/api/v1/analyst-metrics",
            AnalystMetricsTestHelper.CreatePayload(
                Guid.NewGuid(),
                Guid.NewGuid(),
                month,
                year,
                ftdTotal,
                cpaCount));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Manager_ShouldOnlyReadAndWriteOwnDepartments()
    {
        var admin = await CreateAdminClientAsync();
        var allowedDepartmentId = await CreateDepartmentAsync(admin);
        var forbiddenDepartmentId = await CreateDepartmentAsync(admin);
        var allowedCollaboratorId = await CreateCollaboratorAsync(admin, allowedDepartmentId);
        var forbiddenCollaboratorId = await CreateCollaboratorAsync(admin, forbiddenDepartmentId);
        var projectId = await CreateProjectAsync(admin);

        var allowedCreate = await admin.PostAsJsonAsync(
            "/api/v1/analyst-metrics",
            AnalystMetricsTestHelper.CreatePayload(allowedCollaboratorId, projectId));
        allowedCreate.EnsureSuccessStatusCode();
        var forbiddenCreate = await admin.PostAsJsonAsync(
            "/api/v1/analyst-metrics",
            AnalystMetricsTestHelper.CreatePayload(forbiddenCollaboratorId, projectId));
        forbiddenCreate.EnsureSuccessStatusCode();
        var forbiddenMetric = await forbiddenCreate.Content.ReadFromJsonAsync<AnalystMetricApiResponse>();

        var manager = await CreateManagerClientAsync(allowedDepartmentId);
        var list = await manager.GetFromJsonAsync<List<AnalystMetricApiResponse>>(
            $"/api/v1/analyst-metrics?projectId={projectId}");
        list.Should().ContainSingle(item => item.CollaboratorId == allowedCollaboratorId);

        var getForbidden = await manager.GetAsync($"/api/v1/analyst-metrics/{forbiddenMetric!.Id}");
        getForbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var createOwn = await manager.PostAsJsonAsync(
            "/api/v1/analyst-metrics",
            AnalystMetricsTestHelper.CreatePayload(allowedCollaboratorId, projectId, month: 10));
        createOwn.StatusCode.Should().Be(HttpStatusCode.Created);

        var createOutside = await manager.PostAsJsonAsync(
            "/api/v1/analyst-metrics",
            AnalystMetricsTestHelper.CreatePayload(forbiddenCollaboratorId, projectId, month: 10));
        createOutside.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ManagerFilteringForbiddenDepartment_ShouldReturnForbidden()
    {
        var admin = await CreateAdminClientAsync();
        var allowedDepartmentId = await CreateDepartmentAsync(admin);
        var forbiddenDepartmentId = await CreateDepartmentAsync(admin);
        var manager = await CreateManagerClientAsync(allowedDepartmentId);

        var response = await manager.GetAsync(
            $"/api/v1/analyst-metrics?departmentId={forbiddenDepartmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Director_ShouldReadButNotWrite_AndFinancialShouldHaveNoAccess()
    {
        var director = _factory.CreateClient();
        var (_, _, directorToken) = await AuthTestHelper.CreateDirectorUserAsync(_factory);
        AuthTestHelper.SetBearerToken(director, directorToken);

        (await director.GetAsync("/api/v1/analyst-metrics")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await director.PostAsJsonAsync(
            "/api/v1/analyst-metrics",
            AnalystMetricsTestHelper.CreatePayload(Guid.NewGuid(), Guid.NewGuid())))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var financial = _factory.CreateClient();
        var (_, _, financialToken) = await AuthTestHelper.CreateFinancialUserAsync(_factory);
        AuthTestHelper.SetBearerToken(financial, financialToken);
        (await financial.GetAsync("/api/v1/analyst-metrics")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListWithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/analyst-metrics");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<HttpClient> CreateManagerClientAsync(params Guid[] departmentIds)
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory, departmentIds);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private static async Task<Guid> CreateDepartmentAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync(
            "/api/v1/departments",
            MasterDataTestHelper.CreateDepartmentPayload(name: $"Setor {Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
    }

    private static async Task<Guid> CreateCollaboratorAsync(HttpClient admin, Guid departmentId)
    {
        var response = await admin.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: $"Analista {Guid.NewGuid():N}",
                departmentId: departmentId,
                email: null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync(
            "/api/v1/projects",
            MasterDataTestHelper.CreateProjectPayload(name: $"Projeto {Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
    }

    private sealed record IdResponse(Guid Id);

    private sealed record AnalystMetricApiResponse(
        Guid Id,
        Guid CollaboratorId,
        string CollaboratorName,
        Guid DepartmentId,
        string DepartmentName,
        Guid ProjectId,
        string ProjectName,
        int Month,
        int Year,
        int FtdTotal,
        int CpaCount);
}

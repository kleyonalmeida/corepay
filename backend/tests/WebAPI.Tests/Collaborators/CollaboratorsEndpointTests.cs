using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;
using WebAPI.Tests.MasterData;
using WebAPI.Tests.Seed;

namespace WebAPI.Tests.Collaborators;

[Collection("WebApiIntegration")]
public class CollaboratorsEndpointTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public CollaboratorsEndpointTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCollaborators_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/collaborators");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCollaborators_WithUserToken_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateUserWithoutPermissionsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/collaborators");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCollaborators_AsAdmin_ShouldReturnSeededCollaboratorsOrderedByName()
    {
        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync("/api/v1/collaborators");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var collaborators = await ReadCollaboratorsAsync(response);
        collaborators.Should().HaveCountGreaterThanOrEqualTo(2);
        collaborators.Select(c => c.Name).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);

        var active = collaborators.Single(c => c.Name == DevelopmentFixtureData.ActiveCollaboratorName);
        active.DepartmentName.Should().Be("Analistas Comerciais");
        active.CareerLevelName.Should().Be("Analista Comercial Júnior");
        active.JobTitle.Should().Be("Analista Comercial Demo");
        active.PixKey.Should().Be(DevelopmentFixtureData.ActiveCollaboratorPixKey);
        active.BaseSalary.Should().Be(3500m);
        active.IsActive.Should().BeTrue();

        var inactive = collaborators.Single(c => c.Name == DevelopmentFixtureData.InactiveCollaboratorName);
        inactive.IsActive.Should().BeFalse();
        inactive.DismissalDate.Should().Be(new DateOnly(2025, 3, 15));
    }

    [Fact]
    public async Task GetCollaborators_WithDepartmentFilter_ShouldReturnOnlyMatchingDepartment()
    {
        var client = await CreateAdminClientAsync();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var response = await client.GetAsync($"/api/v1/collaborators?departmentId={commercialDepartmentId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var collaborators = await ReadCollaboratorsAsync(response);
        collaborators.Should().NotBeNull();
        collaborators!.Should().OnlyContain(c => c.DepartmentId == commercialDepartmentId);
        collaborators.Should().ContainSingle(c => c.Name == DevelopmentFixtureData.ActiveCollaboratorName);
    }

    [Fact]
    public async Task GetCollaborators_WithSearchFilter_ShouldMatchNameJobTitleOrCareerLevel()
    {
        var client = await CreateAdminClientAsync();

        var byName = await client.GetAsync("/api/v1/collaborators?search=Ana");
        byName.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadCollaboratorsAsync(byName))!
            .Should().ContainSingle(c => c.Name == DevelopmentFixtureData.ActiveCollaboratorName);

        var byJobTitle = await client.GetAsync("/api/v1/collaborators?search=Especialista");
        byJobTitle.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadCollaboratorsAsync(byJobTitle))!
            .Should().ContainSingle(c => c.Name == DevelopmentFixtureData.InactiveCollaboratorName);

        var byLevel = await client.GetAsync("/api/v1/collaborators?search=J%C3%BAnior");
        byLevel.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadCollaboratorsAsync(byLevel))!
            .Should().ContainSingle(c => c.Name == DevelopmentFixtureData.ActiveCollaboratorName);
    }

    [Fact]
    public async Task GetCollaborators_WithActiveFilter_ShouldReturnOnlyActiveCollaborators()
    {
        var client = await CreateAdminClientAsync();

        var activeResponse = await client.GetAsync("/api/v1/collaborators?isActive=true");
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadCollaboratorsAsync(activeResponse))!
            .Should().OnlyContain(c => c.IsActive);

        var inactiveResponse = await client.GetAsync("/api/v1/collaborators?isActive=false");
        inactiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadCollaboratorsAsync(inactiveResponse))!
            .Should().OnlyContain(c => !c.IsActive);
    }

    [Fact]
    public async Task GetCollaboratorById_WhenMissing_ShouldReturnNotFound()
    {
        var client = await CreateAdminClientAsync();
        var response = await client.GetAsync($"/api/v1/collaborators/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Manager_WithSingleDepartment_ShouldOnlySeeOwnCollaborators()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);

        var client = await CreateManagerClientAsync(commercialDepartmentId);

        var listResponse = await client.GetAsync("/api/v1/collaborators");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var collaborators = await ReadCollaboratorsAsync(listResponse);
        collaborators.Should().NotBeNull();
        collaborators!.Should().ContainSingle(c => c.Name == DevelopmentFixtureData.ActiveCollaboratorName);
        collaborators.Should().NotContain(c => c.Name == DevelopmentFixtureData.InactiveCollaboratorName);
        collaborators.Should().OnlyContain(c => c.DepartmentId == commercialDepartmentId);

        var trafficCollaboratorId = await AuthTestHelper.GetCollaboratorIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Collaborators.PaidTrafficInactive);

        var getByIdResponse = await client.GetAsync($"/api/v1/collaborators/{trafficCollaboratorId}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var forbiddenFilterResponse = await client.GetAsync(
            $"/api/v1/collaborators?departmentId={trafficDepartmentId}");
        forbiddenFilterResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_WithZeroDepartments_ShouldReturnEmptyCollaboratorList()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var response = await client.GetAsync("/api/v1/collaborators");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var collaborators = await ReadCollaboratorsAsync(response);
        collaborators.Should().NotBeNull();
        collaborators!.Should().BeEmpty();
    }

    [Fact]
    public async Task Manager_WithOwnDepartmentCollaborator_ShouldReturnOkById()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var commercialCollaboratorId = await AuthTestHelper.GetCollaboratorIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Collaborators.CommercialAnalystActive);

        var client = await CreateManagerClientAsync(commercialDepartmentId);
        var response = await client.GetAsync($"/api/v1/collaborators/{commercialCollaboratorId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var collaborator = await response.Content.ReadFromJsonAsync<CollaboratorApiResponse>();
        collaborator!.Name.Should().Be(DevelopmentFixtureData.ActiveCollaboratorName);
    }

    [Fact]
    public async Task CreateCollaborator_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(departmentId: commercialDepartmentId));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCollaborator_WithUserToken_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateUserWithoutPermissionsAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);

        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(departmentId: commercialDepartmentId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCollaborator_AsAdmin_ShouldReturnCreatedWithLocation()
    {
        var client = await CreateAdminClientAsync();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var commercialLevelId = await GetCareerLevelIdBySeedKeyAsync(SeedKeys.CareerLevels.CommercialAnalystJunior);

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Carla Comercial",
                departmentId: commercialDepartmentId,
                careerLevelId: commercialLevelId,
                calculationProfileOverride: "commercialAnalyst"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<CollaboratorApiResponse>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Carla Comercial");
        created.DepartmentId.Should().Be(commercialDepartmentId);
        created.CareerLevelId.Should().Be(commercialLevelId);
        created.CalculationProfileOverride.Should().Be("commercialAnalyst");
        created.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCollaborator_WithEmptyName_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(name: "   ", departmentId: commercialDepartmentId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCollaborator_InactiveWithoutDismissalDate_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                departmentId: commercialDepartmentId,
                isActive: false,
                dismissalDate: null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCollaborator_InactiveWithDismissalDate_ShouldPersistInactiveCollaborator()
    {
        var client = await CreateAdminClientAsync();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Inativo Teste",
                departmentId: commercialDepartmentId,
                isActive: false,
                dismissalDate: "2025-03-15"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CollaboratorApiResponse>();
        created!.IsActive.Should().BeFalse();
        created.DismissalDate.Should().Be(new DateOnly(2025, 3, 15));

        var listResponse = await client.GetAsync("/api/v1/collaborators?isActive=false");
        var inactiveCollaborators = await ReadCollaboratorsAsync(listResponse);
        inactiveCollaborators!.Should().Contain(c => c.Name == "Inativo Teste" && c.DismissalDate == new DateOnly(2025, 3, 15));
    }

    [Fact]
    public async Task CreateCollaborator_WithInvalidDepartment_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(departmentId: Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCollaborator_WithMismatchedCareerLevel_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficLevelId = await GetCareerLevelIdBySeedKeyAsync(SeedKeys.CareerLevels.PaidTrafficSenior);

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                departmentId: commercialDepartmentId,
                careerLevelId: trafficLevelId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCollaborator_WithNegativeSalary_ShouldReturnBadRequest()
    {
        var client = await CreateAdminClientAsync();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                departmentId: commercialDepartmentId,
                baseSalary: -100m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCollaborator_WhenMissing_ShouldReturnNotFound()
    {
        var client = await CreateAdminClientAsync();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/collaborators/{Guid.NewGuid()}",
            CollaboratorTestHelper.CreateCollaboratorPayload(departmentId: commercialDepartmentId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCollaborator_ReactivatingCollaborator_ShouldClearDismissalDate()
    {
        var client = await CreateAdminClientAsync();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Reativar Teste",
                departmentId: commercialDepartmentId,
                isActive: false,
                dismissalDate: "2025-03-15"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CollaboratorApiResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/collaborators/{created!.Id}",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Reativar Teste",
                departmentId: commercialDepartmentId,
                isActive: true,
                dismissalDate: null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<CollaboratorApiResponse>();
        updated!.IsActive.Should().BeTrue();
        updated.DismissalDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCollaborator_InactivatingWithDismissalDate_ShouldPersistDismissalDate()
    {
        var client = await CreateAdminClientAsync();
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Inativar Teste",
                departmentId: commercialDepartmentId,
                isActive: true));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CollaboratorApiResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/collaborators/{created!.Id}",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Inativar Teste",
                departmentId: commercialDepartmentId,
                isActive: false,
                dismissalDate: "2025-03-15"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await client.GetAsync("/api/v1/collaborators?isActive=false");
        var inactiveCollaborators = await ReadCollaboratorsAsync(listResponse);
        inactiveCollaborators!.Should().Contain(c =>
            c.Name == "Inativar Teste"
            && c.IsActive == false
            && c.DismissalDate == new DateOnly(2025, 3, 15));
    }

    [Fact]
    public async Task Manager_CreateInOwnDepartment_ShouldReturnCreated()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var client = await CreateManagerClientAsync(commercialDepartmentId);

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Manager Create",
                departmentId: commercialDepartmentId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Manager_CreateOutsideDepartment_ShouldReturnForbidden()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);
        var client = await CreateManagerClientAsync(commercialDepartmentId);

        var response = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Manager Forbidden",
                departmentId: trafficDepartmentId,
                isActive: false,
                dismissalDate: "2025-03-15"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_UpdateOwnDepartmentCollaborator_ShouldReturnOk()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var client = await CreateManagerClientAsync(commercialDepartmentId);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Manager Update",
                departmentId: commercialDepartmentId));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CollaboratorApiResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/collaborators/{created!.Id}",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Manager Update OK",
                departmentId: commercialDepartmentId,
                baseSalary: 3600m,
                isActive: true));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<CollaboratorApiResponse>())!.Name.Should().Be("Manager Update OK");
    }

    [Fact]
    public async Task Manager_UpdateOutsideDepartmentCollaborator_ShouldReturnForbidden()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);
        var trafficCollaboratorId = await AuthTestHelper.GetCollaboratorIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Collaborators.PaidTrafficInactive);
        var trafficLevelId = await GetCareerLevelIdBySeedKeyAsync(SeedKeys.CareerLevels.PaidTrafficSenior);
        var client = await CreateManagerClientAsync(commercialDepartmentId);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/collaborators/{trafficCollaboratorId}",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: DevelopmentFixtureData.InactiveCollaboratorName,
                departmentId: trafficDepartmentId,
                careerLevelId: trafficLevelId,
                jobTitle: "Especialista de Tráfego",
                admissionDate: "2023-06-15",
                dismissalDate: "2025-03-15",
                pixKey: DevelopmentFixtureData.InactiveCollaboratorPixKey,
                baseSalary: 4200m,
                email: DevelopmentFixtureData.InactiveCollaboratorEmail,
                isActive: false));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_UpdateMovingCollaboratorToForbiddenDepartment_ShouldReturnForbidden()
    {
        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.CommercialAnalysts);
        var trafficDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            _factory.Services,
            SeedKeys.Departments.PaidTraffic);
        var trafficLevelId = await GetCareerLevelIdBySeedKeyAsync(SeedKeys.CareerLevels.PaidTrafficSenior);
        var client = await CreateManagerClientAsync(commercialDepartmentId);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/collaborators",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Manager Move Test",
                departmentId: commercialDepartmentId));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CollaboratorApiResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/collaborators/{created!.Id}",
            CollaboratorTestHelper.CreateCollaboratorPayload(
                name: "Manager Move Test",
                departmentId: trafficDepartmentId,
                careerLevelId: trafficLevelId,
                isActive: true));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Director_ShouldSeeAllCollaboratorsWithoutDepartmentRestriction()
    {
        var client = _factory.CreateClient();
        var token = await AuthTestHelper.LoginAsync(
            client,
            CorePayWebApplicationFactory.SuperAdminEmail,
            CorePayWebApplicationFactory.SuperAdminPassword);
        AuthTestHelper.SetBearerToken(client, token);

        var email = $"director-{Guid.NewGuid():N}@corepay.test";
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
            var user = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                UserName = email,
                DisplayName = "Director Test",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "TestPassword123!");
            await userManager.AddToRoleAsync(user, Core.Auth.AppRoles.Director);
        }

        var directorClient = _factory.CreateClient();
        var directorToken = await AuthTestHelper.LoginAsync(directorClient, email, "TestPassword123!");
        AuthTestHelper.SetBearerToken(directorClient, directorToken);

        var response = await directorClient.GetAsync("/api/v1/collaborators");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var collaborators = await ReadCollaboratorsAsync(response);
        collaborators!.Should().Contain(c => c.Name == DevelopmentFixtureData.ActiveCollaboratorName);
        collaborators.Should().Contain(c => c.Name == DevelopmentFixtureData.InactiveCollaboratorName);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateAdminUserAsync(_factory);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<HttpClient> CreateManagerClientAsync(Guid departmentId)
    {
        var client = _factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(_factory, departmentId);
        AuthTestHelper.SetBearerToken(client, token);
        return client;
    }

    private async Task<Guid> GetCareerLevelIdBySeedKeyAsync(string seedKey)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.SeedEntities
            .Where(s => s.Key == seedKey)
            .Select(s => s.EntityId)
            .FirstAsync();
    }

    private static async Task<IReadOnlyList<CollaboratorApiResponse>> ReadCollaboratorsAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<CollaboratorsListApiResponse>();
        payload.Should().NotBeNull();
        return payload!.Items;
    }

    private sealed record CollaboratorsListApiResponse(
        IReadOnlyList<CollaboratorApiResponse> Items,
        int TotalCount,
        int Page,
        int PageSize);

    private sealed record CollaboratorApiResponse(
        Guid Id,
        string Name,
        Guid DepartmentId,
        string DepartmentName,
        Guid? CareerLevelId,
        string? CareerLevelName,
        string? JobTitle,
        DateOnly? AdmissionDate,
        DateOnly? DismissalDate,
        string? PixKey,
        decimal? BaseSalary,
        string? Email,
        string? PhotoUrl,
        bool IsActive,
        string? CalculationProfileOverride);
}

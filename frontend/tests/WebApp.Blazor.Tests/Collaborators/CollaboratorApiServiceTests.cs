using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;
using WebApp.Blazor.Tests.Common;

namespace WebApp.Blazor.Tests.Collaborators;

public class CollaboratorApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;

    public CollaboratorApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<ICollaboratorApiService, CollaboratorApiService>();
    }

    [Fact]
    public async Task GetCollaboratorsAsync_Success_ReturnsCollaborators()
    {
        ConfigureResponse(
            HttpMethod.Get,
            "/api/v1/collaborators",
            HttpStatusCode.OK,
            CreateCollaboratorListJson());

        var service = Services.GetRequiredService<ICollaboratorApiService>();
        var result = await service.GetCollaboratorsAsync();

        result.Status.Should().Be(CollaboratorApiStatus.Success);
        result.Collaborators.Should().HaveCount(1);
        result.Collaborators![0].Name.Should().Be("Ana Comercial");
        result.Collaborators[0].BaseSalary.Should().Be(3500m);
    }

    [Fact]
    public async Task GetCollaboratorsAsync_WithQueryParams_SerializesFilters()
    {
        string? capturedPath = null;
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            capturedPath = request.RequestUri!.PathAndQuery;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CollaboratorTestJson.WrapList([]))
            };
        });

        var departmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var service = Services.GetRequiredService<ICollaboratorApiService>();
        var result = await service.GetCollaboratorsAsync(new CollaboratorListQuery(
            departmentId,
            "Ana Comercial",
            true));

        result.Status.Should().Be(CollaboratorApiStatus.Success);
        capturedPath.Should().Contain($"departmentId={departmentId}");
        capturedPath.Should().Contain("search=Ana%20Comercial");
        capturedPath.Should().Contain("isActive=true");
    }

    [Fact]
    public async Task GetCollaboratorsAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Get, "/api/v1/collaborators", HttpStatusCode.Forbidden);

        var service = Services.GetRequiredService<ICollaboratorApiService>();
        var result = await service.GetCollaboratorsAsync();

        result.Status.Should().Be(CollaboratorApiStatus.Forbidden);
    }

    [Fact]
    public async Task GetCollaboratorsAsync_LegacyArrayPayload_ReturnsCollaborators()
    {
        ConfigureResponse(
            HttpMethod.Get,
            "/api/v1/collaborators",
            HttpStatusCode.OK,
            $"[{JsonSerializer.Serialize(CreateCollaboratorPayload())}]");

        var service = Services.GetRequiredService<ICollaboratorApiService>();
        var result = await service.GetCollaboratorsAsync();

        result.Status.Should().Be(CollaboratorApiStatus.Success);
        result.Collaborators.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task CreateCollaboratorAsync_Success_ReturnsCollaborator()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/collaborators",
            HttpStatusCode.Created,
            CreateCollaboratorJson());

        var service = Services.GetRequiredService<ICollaboratorApiService>();
        var result = await service.CreateCollaboratorAsync(CreateCollaboratorRequest());

        result.Status.Should().Be(CollaboratorApiStatus.Success);
        result.Collaborator!.Name.Should().Be("Ana Comercial");
    }

    [Fact]
    public async Task CreateCollaboratorAsync_ValidationError_ReturnsValidationError()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/collaborators",
            HttpStatusCode.BadRequest,
            """{"error":"collaborators.dismissal_date_required","message":"Dismissal date is required."}""");

        var service = Services.GetRequiredService<ICollaboratorApiService>();
        var result = await service.CreateCollaboratorAsync(CreateCollaboratorRequest(isActive: false));

        result.Status.Should().Be(CollaboratorApiStatus.ValidationError);
        result.ErrorCode.Should().Be("collaborators.dismissal_date_required");
    }

    [Fact]
    public async Task UpdateCollaboratorAsync_Forbidden_ReturnsForbidden()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        ConfigureResponse(
            HttpMethod.Put,
            $"/api/v1/collaborators/{id}",
            HttpStatusCode.Forbidden,
            """{"error":"collaborators.department_forbidden","message":"Forbidden"}""");

        var service = Services.GetRequiredService<ICollaboratorApiService>();
        var result = await service.UpdateCollaboratorAsync(id, CreateCollaboratorRequest());

        result.Status.Should().Be(CollaboratorApiStatus.Forbidden);
        result.ErrorCode.Should().Be("collaborators.department_forbidden");
    }

    [Fact]
    public async Task GetCollaboratorByIdAsync_Forbidden_ReturnsForbidden()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        ConfigureResponse(
            HttpMethod.Get,
            $"/api/v1/collaborators/{id}",
            HttpStatusCode.Forbidden,
            """{"error":"collaborators.department_forbidden","message":"Forbidden"}""");

        var service = Services.GetRequiredService<ICollaboratorApiService>();
        var result = await service.GetCollaboratorByIdAsync(id);

        result.Status.Should().Be(CollaboratorApiStatus.Forbidden);
        result.ErrorCode.Should().Be("collaborators.department_forbidden");
    }

    private void ConfigureResponse(HttpMethod method, string path, HttpStatusCode status, string? body = null)
    {
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(method);
            request.RequestUri!.PathAndQuery.Should().Be(path);
            return body is null
                ? new HttpResponseMessage(status)
                : new HttpResponseMessage(status)
                {
                    Content = new StringContent(body)
                };
        });
    }

    private static string CreateCollaboratorListJson() =>
        CollaboratorTestJson.WrapList([CreateCollaboratorPayload()]);

    private static string CreateCollaboratorJson() =>
        JsonSerializer.Serialize(CreateCollaboratorPayload());

    private static object CreateCollaboratorPayload() =>
        new
        {
            id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            name = "Ana Comercial",
            departmentId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            departmentName = "Analistas Comerciais",
            careerLevelId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            careerLevelName = "Analista Comercial Júnior",
            jobTitle = "Analista Comercial",
            admissionDate = "2024-03-01",
            dismissalDate = (string?)null,
            pixKey = "11999990001",
            baseSalary = 3500m,
            email = "ana@corepay.test",
            photoUrl = (string?)null,
            isActive = true,
            calculationProfileOverride = (string?)null
        };

    private static CollaboratorRequest CreateCollaboratorRequest(bool isActive = true) =>
        new(
            "Ana Comercial",
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Analista Comercial",
            new DateOnly(2024, 3, 1),
            isActive ? null : new DateOnly(2025, 3, 15),
            "11999990001",
            3500m,
            "ana@corepay.test",
            null,
            isActive,
            null);
}

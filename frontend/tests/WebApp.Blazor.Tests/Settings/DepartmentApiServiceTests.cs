using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.SettingsUi;

public class DepartmentApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid ExistingDepartmentId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");

    public DepartmentApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<IDepartmentApiService, DepartmentApiService>();
    }

    [Fact]
    public async Task GetDepartmentsAsync_Success_ReturnsDepartments()
    {
        ConfigureResponse(
            HttpMethod.Get,
            "/api/v1/departments",
            HttpStatusCode.OK,
            CreateDepartmentListJson());

        var service = Services.GetRequiredService<IDepartmentApiService>();
        var result = await service.GetDepartmentsAsync();

        result.Status.Should().Be(DepartmentApiStatus.Success);
        result.Departments.Should().HaveCount(1);
        result.Departments![0].Name.Should().Be("Gerência");
        result.Departments[0].CalculationType.Should().Be(CalculationProfile.Management);
    }

    [Fact]
    public async Task GetDepartmentsAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Get, "/api/v1/departments", HttpStatusCode.Forbidden);

        var service = Services.GetRequiredService<IDepartmentApiService>();
        var result = await service.GetDepartmentsAsync();

        result.Status.Should().Be(DepartmentApiStatus.Forbidden);
    }

    [Fact]
    public async Task CreateDepartmentAsync_Success_SerializesEnumAsCamelCase()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/departments");
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateDepartmentJson("Tipster", "tipster"))
            };
        });

        var service = Services.GetRequiredService<IDepartmentApiService>();
        var result = await service.CreateDepartmentAsync(CreateRequest("Tipster", CalculationProfile.Tipster));

        result.Status.Should().Be(DepartmentApiStatus.Success);
        result.Department!.CalculationType.Should().Be(CalculationProfile.Tipster);
        capturedBody.Should().Contain("\"calculationType\":\"tipster\"");
    }

    [Fact]
    public async Task CreateDepartmentAsync_ValidationError_ReturnsValidationStatus()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/departments",
            HttpStatusCode.BadRequest,
            """{"error":"departments.name_required","message":"Name is required."}""");

        var service = Services.GetRequiredService<IDepartmentApiService>();
        var result = await service.CreateDepartmentAsync(CreateRequest("", CalculationProfile.Management));

        result.Status.Should().Be(DepartmentApiStatus.ValidationError);
        result.ErrorCode.Should().Be("departments.name_required");
    }

    [Fact]
    public async Task CreateDepartmentAsync_Conflict_ReturnsConflictStatus()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/departments",
            HttpStatusCode.Conflict,
            """{"error":"departments.duplicate","message":"Department already exists."}""");

        var service = Services.GetRequiredService<IDepartmentApiService>();
        var result = await service.CreateDepartmentAsync(CreateRequest("Gerência", CalculationProfile.Management));

        result.Status.Should().Be(DepartmentApiStatus.Conflict);
        result.ErrorCode.Should().Be("departments.duplicate");
    }

    [Fact]
    public async Task UpdateDepartmentAsync_NotFound_ReturnsNotFoundStatus()
    {
        ConfigureResponse(
            HttpMethod.Put,
            $"/api/v1/departments/{ExistingDepartmentId}",
            HttpStatusCode.NotFound,
            """{"error":"departments.not_found","message":"Department not found."}""");

        var service = Services.GetRequiredService<IDepartmentApiService>();
        var result = await service.UpdateDepartmentAsync(
            ExistingDepartmentId,
            CreateRequest("Gerência", CalculationProfile.Management));

        result.Status.Should().Be(DepartmentApiStatus.NotFound);
        result.ErrorCode.Should().Be("departments.not_found");
    }

    [Fact]
    public async Task UpdateDepartmentAsync_Success_ReturnsUpdatedDepartment()
    {
        ConfigureResponse(
            HttpMethod.Put,
            $"/api/v1/departments/{ExistingDepartmentId}",
            HttpStatusCode.OK,
            CreateDepartmentJson("Gerência", "management", ExistingDepartmentId));

        var service = Services.GetRequiredService<IDepartmentApiService>();
        var result = await service.UpdateDepartmentAsync(
            ExistingDepartmentId,
            CreateRequest("Gerência", CalculationProfile.Management));

        result.Status.Should().Be(DepartmentApiStatus.Success);
        result.Department!.Name.Should().Be("Gerência");
    }

    [Fact]
    public async Task CreateDepartmentAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Post, "/api/v1/departments", HttpStatusCode.Forbidden);

        var service = Services.GetRequiredService<IDepartmentApiService>();
        var result = await service.CreateDepartmentAsync(CreateRequest("Tipster", CalculationProfile.Tipster));

        result.Status.Should().Be(DepartmentApiStatus.Forbidden);
    }

    private void ConfigureResponse(HttpMethod method, string path, HttpStatusCode status, string? body = null)
    {
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(method);
            request.RequestUri!.AbsolutePath.Should().Be(path);
            return body is null
                ? new HttpResponseMessage(status)
                : new HttpResponseMessage(status)
                {
                    Content = new StringContent(body)
                };
        });
    }

    private static DepartmentRequest CreateRequest(string name, CalculationProfile profile) =>
        new(
            name,
            profile,
            GoalBonusPercentage: 0m,
            LowRevenueThreshold: 200_000m,
            LowRevenueBonusPct: 0.4m,
            Description: null,
            IsActive: true,
            IsAllocatedFixed: false,
            RoutesFixedToLimaKarttos: false);

    private static string CreateDepartmentListJson() =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = ExistingDepartmentId,
                name = "Gerência",
                calculationType = "management",
                goalBonusPercentage = 0m,
                lowRevenueThreshold = 200_000m,
                lowRevenueBonusPct = 0.4m,
                description = (string?)null,
                isActive = true,
                isAllocatedFixed = false,
                routesFixedToLimaKarttos = false
            }
        });

    private static string CreateDepartmentJson(
        string name,
        string calculationType,
        Guid? id = null) =>
        JsonSerializer.Serialize(new
        {
            id = id ?? Guid.NewGuid(),
            name,
            calculationType,
            goalBonusPercentage = 0m,
            lowRevenueThreshold = 200_000m,
            lowRevenueBonusPct = 0.4m,
            description = (string?)null,
            isActive = true,
            isAllocatedFixed = false,
            routesFixedToLimaKarttos = false
        });
}

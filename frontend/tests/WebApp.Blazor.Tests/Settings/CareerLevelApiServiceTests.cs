using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Blazor.Services;
using WebApp.Blazor.Tests.Auth;

namespace WebApp.Blazor.Tests.SettingsUi;

public class CareerLevelApiServiceTests : BlazorComponentTestContext
{
    private readonly StubHttpMessageHandler _httpHandler;
    private static readonly Guid ExistingCareerLevelId = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa7");
    private static readonly Guid DepartmentId = Guid.Parse("7fa85f64-5717-4562-b3fc-2c963f66afa6");

    public CareerLevelApiServiceTests()
    {
        _httpHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        Services.AddScoped(_ => new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        });
        Services.AddScoped<ICareerLevelApiService, CareerLevelApiService>();
    }

    [Fact]
    public async Task GetCareerLevelsAsync_Success_ReturnsCareerLevels()
    {
        ConfigureResponse(
            HttpMethod.Get,
            "/api/v1/career-levels",
            HttpStatusCode.OK,
            CreateCareerLevelListJson());

        var service = Services.GetRequiredService<ICareerLevelApiService>();
        var result = await service.GetCareerLevelsAsync();

        result.Status.Should().Be(CareerLevelApiStatus.Success);
        result.CareerLevels.Should().HaveCount(1);
        result.CareerLevels![0].Name.Should().Be("Analista Comercial Júnior");
        result.CareerLevels[0].Profile.Should().Be(CalculationProfile.CommercialAnalyst);
        result.CareerLevels[0].FtdRateBase.Should().Be(2m);
        result.CareerLevels[0].SalesPctBase.Should().Be(4m);
        result.CareerLevels[0].FtdBonusEvery.Should().Be(250);
        result.CareerLevels[0].FtdBonusValue.Should().Be(350m);
    }

    [Fact]
    public async Task GetCareerLevelsAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Get, "/api/v1/career-levels", HttpStatusCode.Forbidden);

        var service = Services.GetRequiredService<ICareerLevelApiService>();
        var result = await service.GetCareerLevelsAsync();

        result.Status.Should().Be(CareerLevelApiStatus.Forbidden);
    }

    [Fact]
    public async Task CreateCareerLevelAsync_Success_SerializesEnumAsCamelCase()
    {
        string? capturedBody = null;
        _httpHandler.Configure(request =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/career-levels");
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(CreateCareerLevelJson("Analista Comercial Júnior", "commercialAnalyst"))
            };
        });

        var service = Services.GetRequiredService<ICareerLevelApiService>();
        var result = await service.CreateCareerLevelAsync(CreateAnalystJuniorRequest());

        result.Status.Should().Be(CareerLevelApiStatus.Success);
        result.CareerLevel!.Profile.Should().Be(CalculationProfile.CommercialAnalyst);
        capturedBody.Should().Contain("\"profile\":\"commercialAnalyst\"");
        capturedBody.Should().Contain("\"ftdRateBase\":2");
        capturedBody.Should().Contain("\"salesPctBase\":4");
        capturedBody.Should().Contain("\"ftdBonusEvery\":250");
        capturedBody.Should().Contain("\"ftdBonusValue\":350");
    }

    [Fact]
    public async Task CreateCareerLevelAsync_ValidationError_ReturnsValidationStatus()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/career-levels",
            HttpStatusCode.BadRequest,
            """{"error":"careerlevels.name_required","message":"Name is required."}""");

        var service = Services.GetRequiredService<ICareerLevelApiService>();
        var result = await service.CreateCareerLevelAsync(CreateAnalystJuniorRequest(name: ""));

        result.Status.Should().Be(CareerLevelApiStatus.ValidationError);
        result.ErrorCode.Should().Be("careerlevels.name_required");
    }

    [Fact]
    public async Task CreateCareerLevelAsync_Conflict_ReturnsConflictStatus()
    {
        ConfigureResponse(
            HttpMethod.Post,
            "/api/v1/career-levels",
            HttpStatusCode.Conflict,
            """{"error":"careerlevels.duplicate","message":"Career level already exists."}""");

        var service = Services.GetRequiredService<ICareerLevelApiService>();
        var result = await service.CreateCareerLevelAsync(CreateAnalystJuniorRequest());

        result.Status.Should().Be(CareerLevelApiStatus.Conflict);
        result.ErrorCode.Should().Be("careerlevels.duplicate");
    }

    [Fact]
    public async Task UpdateCareerLevelAsync_NotFound_ReturnsNotFoundStatus()
    {
        ConfigureResponse(
            HttpMethod.Put,
            $"/api/v1/career-levels/{ExistingCareerLevelId}",
            HttpStatusCode.NotFound,
            """{"error":"careerlevels.not_found","message":"Career level not found."}""");

        var service = Services.GetRequiredService<ICareerLevelApiService>();
        var result = await service.UpdateCareerLevelAsync(
            ExistingCareerLevelId,
            CreateAnalystJuniorRequest());

        result.Status.Should().Be(CareerLevelApiStatus.NotFound);
        result.ErrorCode.Should().Be("careerlevels.not_found");
    }

    [Fact]
    public async Task UpdateCareerLevelAsync_Success_ReturnsUpdatedCareerLevel()
    {
        ConfigureResponse(
            HttpMethod.Put,
            $"/api/v1/career-levels/{ExistingCareerLevelId}",
            HttpStatusCode.OK,
            CreateCareerLevelJson("Analista Comercial Júnior", "commercialAnalyst", ExistingCareerLevelId));

        var service = Services.GetRequiredService<ICareerLevelApiService>();
        var result = await service.UpdateCareerLevelAsync(
            ExistingCareerLevelId,
            CreateAnalystJuniorRequest());

        result.Status.Should().Be(CareerLevelApiStatus.Success);
        result.CareerLevel!.Name.Should().Be("Analista Comercial Júnior");
    }

    [Fact]
    public async Task CreateCareerLevelAsync_Forbidden_ReturnsForbidden()
    {
        ConfigureResponse(HttpMethod.Post, "/api/v1/career-levels", HttpStatusCode.Forbidden);

        var service = Services.GetRequiredService<ICareerLevelApiService>();
        var result = await service.CreateCareerLevelAsync(CreateAnalystJuniorRequest());

        result.Status.Should().Be(CareerLevelApiStatus.Forbidden);
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

    private static CareerLevelRequest CreateAnalystJuniorRequest(string name = "Analista Comercial Júnior") =>
        new(
            name,
            DepartmentId,
            CalculationProfile.CommercialAnalyst,
            IsActive: true,
            BaseSalary: 1500m,
            CommissionWithoutGoalPct: 0m,
            CommissionWithGoalPct: 0m,
            CommissionWithSuperGoalPct: 0m,
            GroupCommissionPerPercent: 0m,
            GroupCommissionPer20Percent: 0m,
            DefaultCpaValue: 0m,
            GoalBonusValue: 0m,
            FtdRateBase: 2m,
            FtdRateWithGoal: 2.5m,
            FtdRateWithSuperGoal: 3m,
            FtdSuperbetRate: 5m,
            FtdBonusEvery: 250,
            FtdBonusValue: 350m,
            SalesPctBase: 4m,
            SalesPctWithGoal: 5m,
            SalesPctWithSuperGoal: 6m,
            SalesBonusEvery: 20_000m,
            SalesBonusValue: 250m,
            RevPct: 1m,
            BetanoInternaValue: 200m,
            BetanoMundoBetValue: 70m,
            SupFtdSuperbetNoGoal: 0m,
            SupFtdSuperbetWithGoal: 0m,
            SupFtdOtherNoGoal: 0m,
            SupFtdOtherWithGoal: 0m,
            SupSalesPctNoGoal: 0m,
            SupSalesPctWithGoal: 0m,
            SupRevPct: 0m,
            NetRevenueFactor: 0m,
            NetRevenuePctNoGoal: 0m,
            NetRevenuePctWithGoal: 0m,
            TrafficInvestmentCommissionPct: 0m,
            TrafficCpaEsportiva: 0m,
            TrafficCpaStake: 0m,
            TrafficCpaBetano: 0m,
            TrafficCpaBetMgm: 0m,
            TrafficCpaNovibet: 0m,
            TrafficCpaBetFair: 0m,
            TrafficCpaBlaze: 0m,
            TrafficCpaSuperbet: 0m,
            TrafficCpaHiperbet: 0m,
            TrafficSupBonus: 0m,
            TrafficSupCommissionPct: 0m);

    private static string CreateCareerLevelListJson() =>
        JsonSerializer.Serialize(new[] { CreateCareerLevelObject() });

    private static string CreateCareerLevelJson(
        string name,
        string profile,
        Guid? id = null) =>
        JsonSerializer.Serialize(CreateCareerLevelObject(name, profile, id));

    private static object CreateCareerLevelObject(
        string name = "Analista Comercial Júnior",
        string profile = "commercialAnalyst",
        Guid? id = null) =>
        new
        {
            id = id ?? ExistingCareerLevelId,
            name,
            departmentId = DepartmentId,
            profile,
            isActive = true,
            baseSalary = 1500m,
            commissionWithoutGoalPct = 0m,
            commissionWithGoalPct = 0m,
            commissionWithSuperGoalPct = 0m,
            groupCommissionPerPercent = 0m,
            groupCommissionPer20Percent = 0m,
            defaultCpaValue = 0m,
            goalBonusValue = 0m,
            ftdRateBase = 2m,
            ftdRateWithGoal = 2.5m,
            ftdRateWithSuperGoal = 3m,
            ftdSuperbetRate = 5m,
            ftdBonusEvery = 250,
            ftdBonusValue = 350m,
            salesPctBase = 4m,
            salesPctWithGoal = 5m,
            salesPctWithSuperGoal = 6m,
            salesBonusEvery = 20_000m,
            salesBonusValue = 250m,
            revPct = 1m,
            betanoInternaValue = 200m,
            betanoMundoBetValue = 70m,
            supFtdSuperbetNoGoal = 0m,
            supFtdSuperbetWithGoal = 0m,
            supFtdOtherNoGoal = 0m,
            supFtdOtherWithGoal = 0m,
            supSalesPctNoGoal = 0m,
            supSalesPctWithGoal = 0m,
            supRevPct = 0m,
            netRevenueFactor = 0m,
            netRevenuePctNoGoal = 0m,
            netRevenuePctWithGoal = 0m,
            trafficInvestmentCommissionPct = 0m,
            trafficCpaEsportiva = 0m,
            trafficCpaStake = 0m,
            trafficCpaBetano = 0m,
            trafficCpaBetMgm = 0m,
            trafficCpaNovibet = 0m,
            trafficCpaBetFair = 0m,
            trafficCpaBlaze = 0m,
            trafficCpaSuperbet = 0m,
            trafficCpaHiperbet = 0m,
            trafficSupBonus = 0m,
            trafficSupCommissionPct = 0m
        };
}

using Core.Domain;
using Core.Domain.PayrollCalculation;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Seed;

[Collection("WebApiIntegration")]
public class PayrollCalculationFixturesTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public PayrollCalculationFixturesTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Fixtures_ShouldResolveProfileByExplicitData_NotByName()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var commercialDeptId = seedEntities.Single(s => s.Key == SeedKeys.Departments.CommercialAnalysts).EntityId;
        var trafficDeptId = seedEntities.Single(s => s.Key == SeedKeys.Departments.PaidTraffic).EntityId;
        var commercialLevelId = seedEntities.Single(s => s.Key == SeedKeys.CareerLevels.CommercialAnalystJunior).EntityId;
        var collaboratorId = seedEntities.Single(s => s.Key == SeedKeys.Collaborators.CommercialAnalystActive).EntityId;

        var department = await dbContext.Departments.AsNoTracking()
            .SingleAsync(d => d.Id == commercialDeptId);
        var level = await dbContext.CareerLevels.AsNoTracking()
            .SingleAsync(c => c.Id == commercialLevelId);
        var collaborator = await dbContext.Collaborators.AsNoTracking()
            .SingleAsync(c => c.Id == collaboratorId);

        department.Name = "Setor Renomeado";
        level.Name = "Nível Renomeado";
        collaborator.Name = "Colaborador Renomeado";

        CalculationProfileResolver.Resolve(collaborator, department, level)
            .Should().Be(CalculationProfile.CommercialAnalyst);

        var trafficDepartment = await dbContext.Departments.AsNoTracking()
            .SingleAsync(d => d.Id == trafficDeptId);
        trafficDepartment.Name = "Tráfego Renomeado";

        CalculationProfileResolver.Resolve(collaborator, trafficDepartment, careerLevel: null)
            .Should().Be(CalculationProfile.PaidTraffic);
    }

    [Fact]
    public async Task Fixtures_ShouldIdentifySpecialProjectsByIdAndFlags()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var limaKarttosId = seedEntities.Single(s => s.Key == SeedKeys.Projects.LimaKarttos).EntityId;
        var feiraId = seedEntities.Single(s => s.Key == SeedKeys.Projects.FeiraX).EntityId;
        var threeCSportsId = seedEntities.Single(s => s.Key == SeedKeys.Projects.ThreeCSports).EntityId;
        var affiliatesId = seedEntities.Single(s => s.Key == SeedKeys.Projects.Affiliates).EntityId;

        var projects = await dbContext.Projects.AsNoTracking().ToListAsync();
        var limaKarttos = projects.Single(p => p.Id == limaKarttosId);
        var feira = projects.Single(p => p.Id == feiraId);
        var threeCSports = projects.Single(p => p.Id == threeCSportsId);
        var affiliates = projects.Single(p => p.Id == affiliatesId);

        limaKarttos.Name = "Projeto Renomeado Lima";
        feira.Name = "Evento Renomeado";
        threeCSports.Name = "Esporte Renomeado";
        affiliates.Name = "Parceiros Renomeado";

        ProjectCalculationFlags.IsDefaultAllocationTarget(limaKarttos).Should().BeTrue();
        ProjectCalculationFlags.IsLimaKarttosProject(limaKarttos, limaKarttosId).Should().BeTrue();
        ProjectCalculationFlags.ExcludesGoalBonus(feira).Should().BeTrue();
        ProjectCalculationFlags.IsFeiraProject(feira, feiraId).Should().BeTrue();
        ProjectCalculationFlags.ExcludesSupervisorFixedAllocation(threeCSports).Should().BeTrue();
        ProjectCalculationFlags.IsThreeCSportsProject(threeCSports, threeCSportsId).Should().BeTrue();
        ProjectCalculationFlags.IsAffiliatesProject(affiliates, affiliatesId).Should().BeTrue();
    }

    [Fact]
    public async Task Fixtures_ShouldResolveTrafficCpaManagerUsingSeniorLevel()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var trafficDeptId = seedEntities.Single(s => s.Key == SeedKeys.Departments.PaidTraffic).EntityId;
        var seniorLevelId = seedEntities.Single(s => s.Key == SeedKeys.CareerLevels.PaidTrafficSenior).EntityId;

        var juniorLevel = new CareerLevel
        {
            Id = Guid.NewGuid(),
            Name = "Pleno Renomeado",
            DepartmentId = trafficDeptId,
            Profile = CalculationProfile.PaidTraffic,
            TrafficCpaBetano = 30m
        };

        var seniorLevel = await dbContext.CareerLevels.AsNoTracking()
            .SingleAsync(c => c.Id == seniorLevelId);
        seniorLevel.TrafficCpaBetano = 50m;

        var result = TrafficCpaRateHelper.GetTrafficCpaRate(
            TrafficHouse.Betano,
            TrafficCpaKind.Manager,
            juniorLevel,
            seniorLevel);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(50m);
    }

    [Fact]
    public async Task Fixtures_ShouldPersistPaidTrafficSeniorRates()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var seniorLevelId = seedEntities.Single(s => s.Key == SeedKeys.CareerLevels.PaidTrafficSenior).EntityId;

        var seniorLevel = await dbContext.CareerLevels.AsNoTracking()
            .SingleAsync(c => c.Id == seniorLevelId);

        seniorLevel.TrafficInvestmentCommissionPct.Should().Be(2m);
        seniorLevel.TrafficCpaBetano.Should().Be(50m);
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateAffiliatesProject()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seedEntities = await dbContext.SeedEntities.AsNoTracking().ToListAsync();
        var affiliatesKey = seedEntities.SingleOrDefault(s => s.Key == SeedKeys.Projects.Affiliates);
        affiliatesKey.Should().NotBeNull();

        var affiliates = await dbContext.Projects.AsNoTracking()
            .SingleAsync(p => p.Id == affiliatesKey!.EntityId);

        affiliates.Name.Should().Be("Projeto Demo Affiliates");
        affiliates.Platform.Should().Be(ProjectPlatform.Lastlink);

        var affiliatesDept = await dbContext.Departments.AsNoTracking()
            .SingleAsync(d => d.CalculationType == CalculationProfile.FixedCommissionBonus);

        DepartmentCalculationFlags.IsAffiliatesDepartment(affiliatesDept).Should().BeTrue();
    }
}

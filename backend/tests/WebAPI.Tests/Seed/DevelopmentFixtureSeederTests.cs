using Core.Domain;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Seed;

[Collection("WebApiIntegration")]
public class DevelopmentFixtureSeederTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public DevelopmentFixtureSeederTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateNamedFixturesWithExplicitProfiles()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var departments = await dbContext.Departments.AsNoTracking().ToListAsync();
        departments.Should().Contain(d =>
            d.Name == "Setor Tipster" && d.CalculationType == CalculationProfile.Tipster);
        departments.Should().Contain(d =>
            d.Name == "Tráfego Pago" && d.CalculationType == CalculationProfile.PaidTraffic);
        departments.Should().Contain(d =>
            d.Name == "Líderes de Projetos" && d.CalculationType == CalculationProfile.ProjectLeader);
        departments.Should().Contain(d =>
            d.Name == "Analistas Comerciais" && d.CalculationType == CalculationProfile.CommercialAnalyst);
        departments.Should().Contain(d =>
            d.Name == "Gerência" && d.CalculationType == CalculationProfile.Management);
        departments.Should().Contain(d =>
            d.Name == "Affiliates" && d.CalculationType == CalculationProfile.FixedCommissionBonus);
        departments.Should().Contain(d =>
            d.Name == "Administrativo" && d.IsAllocatedFixed);
        departments.Should().Contain(d =>
            d.Name == "IA Automação" && d.RoutesFixedToLimaKarttos);
        departments.Should().Contain(d =>
            d.Name == "Contingência" && d.RoutesFixedToLimaKarttos);
        departments.Should().Contain(d =>
            d.Name == "Suporte" && d.IsAllocatedFixed);
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateCareerLevelsWithExplicitProfiles()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var levels = await dbContext.CareerLevels
            .Include(c => c.Department)
            .AsNoTracking()
            .ToListAsync();

        levels.Should().Contain(c =>
            c.Name == "Analista Comercial Júnior" &&
            c.Profile == CalculationProfile.CommercialAnalyst &&
            c.FtdRateBase == 2m &&
            c.SalesPctBase == 4m &&
            c.FtdBonusEvery == 250 &&
            c.FtdBonusValue == 350m);
        levels.Should().Contain(c =>
            c.Name == "Supervisor" &&
            c.Profile == CalculationProfile.CommercialSupervisor &&
            c.Department!.Name == "Analistas Comerciais");
        levels.Should().Contain(c =>
            c.Name == "Sênior" &&
            c.Profile == CalculationProfile.PaidTraffic &&
            c.Department!.Name == "Tráfego Pago");
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateCollaboratorsInDistinctDepartments()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var collaborators = await dbContext.Collaborators
            .Include(c => c.Department)
            .Include(c => c.CareerLevel)
            .AsNoTracking()
            .ToListAsync();

        collaborators.Should().Contain(c =>
            c.Name == DevelopmentFixtureData.ActiveCollaboratorName &&
            c.IsActive &&
            c.Email == DevelopmentFixtureData.ActiveCollaboratorEmail &&
            c.Department!.Name == "Analistas Comerciais" &&
            c.CareerLevel!.Name == "Analista Comercial Júnior");
        collaborators.Should().Contain(c =>
            c.Name == DevelopmentFixtureData.InactiveCollaboratorName &&
            !c.IsActive &&
            c.Email == DevelopmentFixtureData.InactiveCollaboratorEmail &&
            c.DismissalDate == new DateOnly(2025, 3, 15) &&
            c.Department!.Name == "Tráfego Pago");
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateProjectsWithExplicitFlags()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var projects = await dbContext.Projects.AsNoTracking().ToListAsync();

        projects.Should().Contain(p =>
            p.Name == "Projeto Demo Lima" && p.IsDefaultAllocationTarget);
        projects.Should().Contain(p =>
            p.Name == "Projeto Demo Feira" && p.ExcludesGoalBonus);
        projects.Should().Contain(p =>
            p.Name == "Projeto Demo 3C" && p.ExcludesSupervisorFixedAllocation);
        projects.Should().Contain(p =>
            p.Name == "Projeto Demo Affiliates" && p.Platform == ProjectPlatform.Lastlink);
        projects.Should().Contain(p =>
            p.Name == "Projeto Lastlink Demo" && p.Platform == ProjectPlatform.Lastlink);
        projects.Should().Contain(p =>
            p.Name == "Projeto Hubla Demo" && p.Platform == ProjectPlatform.Hubla);
    }

    [Fact]
    public async Task RenamedDepartment_ShouldKeepExplicitCalculationProfile()
    {
        await using var factory = new CorePayWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var department = await dbContext.Departments
            .SingleAsync(d => d.CalculationType == CalculationProfile.PaidTraffic);

        department.Name = "Setor Renomeado Sem Regex";
        await dbContext.SaveChangesAsync();

        var reloaded = await dbContext.Departments
            .AsNoTracking()
            .SingleAsync(d => d.Id == department.Id);

        reloaded.CalculationType.Should().Be(CalculationProfile.PaidTraffic);
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new DevelopmentFixtureSeeder(dbContext);

        var departmentsBefore = await dbContext.Departments.CountAsync();
        var projectsBefore = await dbContext.Projects.CountAsync();
        var collaboratorsBefore = await dbContext.Collaborators.CountAsync();

        await seeder.SeedAsync();

        (await dbContext.Departments.CountAsync()).Should().Be(departmentsBefore);
        (await dbContext.Projects.CountAsync()).Should().Be(projectsBefore);
        (await dbContext.Collaborators.CountAsync()).Should().Be(collaboratorsBefore);
    }

    [Fact]
    public async Task ApiStartup_ShouldNotLoadFixtures()
    {
        await using var factory = new CorePayWithoutFixturesWebApplicationFactory();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await dbContext.Departments.CountAsync()).Should().Be(0);
        (await dbContext.Projects.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TestFactory_ShouldLoadFixturesWhenEnabled()
    {
        await using var factory = new CorePayWebApplicationFactory();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (await dbContext.Departments.CountAsync()).Should().BeGreaterThan(0);
        (await dbContext.Collaborators.CountAsync()).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DemoSeed_ShouldCreateIdempotentRoleUsersScopeAndOperationalScenarios()
    {
        await using var factory = new CorePayWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDemoDataSeeder>();

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var manager = await userManager.FindByEmailAsync("manager@corepay.local");

        manager.Should().NotBeNull();
        (await db.UserDepartments.CountAsync(x => x.UserId == manager!.Id)).Should().Be(1);
        (await db.Payrolls.Select(x => x.Status).Distinct().CountAsync()).Should().Be(5);
        (await db.ProjectRevenues.CountAsync()).Should().Be(1);
        (await db.AnalystMetrics.CountAsync()).Should().Be(1);
        (await db.TrafficInvestments.CountAsync()).Should().Be(1);
        (await db.TrafficProjectDeposits.CountAsync()).Should().Be(1);
        (await db.ProjectCosts.CountAsync()).Should().Be(2);
        (await db.Notifications.CountAsync()).Should().Be(4);
    }
}

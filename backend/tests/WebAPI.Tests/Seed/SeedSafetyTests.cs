using FluentAssertions;
using Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Seed;

public sealed class SeedSafetyTests
{
    [Fact]
    public void DemoSeedTypes_ShouldLiveOutsideApplicationAssembly()
    {
        var applicationTypes = typeof(Program).Assembly.GetTypes().Select(t => t.Name);

        applicationTypes.Should().NotContain("DemoScenarioSeeder");
        applicationTypes.Should().NotContain("DemoSeedStartupGuard");
    }

    [Fact]
    public void InfrastructureAssembly_ShouldContainOptInDevelopmentSeeders()
    {
        var infrastructureTypes = typeof(IdentityDataSeeder).Assembly.GetTypes().Select(t => t.Name);

        infrastructureTypes.Should().Contain("DevelopmentFixtureSeeder");
        infrastructureTypes.Should().Contain("DevelopmentDemoDataSeeder");
    }

    [Fact]
    public void SeedOptions_ShouldExposeDisabledByDefaultDevelopmentFlags()
    {
        var options = new SeedOptions();

        options.LoadFixtures.Should().BeFalse();
        options.LoadDemoData.Should().BeFalse();
        options.RoleUsersPassword.Should().BeEmpty();
    }

    [Fact]
    public void SuperAdminSeedOptions_ShouldNotDefineDefaultPassword()
    {
        var passwordProperty = typeof(SuperAdminSeedOptions).GetProperty(nameof(SuperAdminSeedOptions.Password));
        passwordProperty.Should().NotBeNull();

        var defaultInstance = new SuperAdminSeedOptions();
        defaultInstance.Password.Should().BeEmpty();
    }

    [Fact]
    public void ApiServiceProvider_ShouldRegisterInfrastructureDevelopmentSeeders()
    {
        using var factory = new CorePayWithoutFixturesWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        scope.ServiceProvider.GetService<Infrastructure.Seed.DevelopmentFixtureSeeder>().Should().NotBeNull();
        scope.ServiceProvider.GetService<DevelopmentDemoDataSeeder>().Should().NotBeNull();
    }
}

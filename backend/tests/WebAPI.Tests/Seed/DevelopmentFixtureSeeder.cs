using Infrastructure;

namespace WebAPI.Tests.Seed;

/// <summary>Compatibility wrapper preserving the integration-test fixture API.</summary>
public sealed class DevelopmentFixtureSeeder
{
    private readonly Infrastructure.Seed.DevelopmentFixtureSeeder _inner;

    public DevelopmentFixtureSeeder(AppDbContext dbContext) =>
        _inner = new Infrastructure.Seed.DevelopmentFixtureSeeder(dbContext);

    public Task SeedAsync(CancellationToken cancellationToken = default) =>
        _inner.SeedAsync(cancellationToken);
}

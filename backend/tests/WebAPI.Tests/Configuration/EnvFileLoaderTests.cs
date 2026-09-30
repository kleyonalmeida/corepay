using FluentAssertions;
using WebAPI.Configuration;

namespace WebAPI.Tests.Configuration;

[Collection("WebApiIntegration")]
public sealed class EnvFileLoaderTests : IDisposable
{
    private readonly string _root;
    private readonly string _previousWorkingDirectory;
    private readonly Dictionary<string, string?> _previousEnvironment = [];

    public EnvFileLoaderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"corepay-env-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _previousWorkingDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_root);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_previousWorkingDirectory);

        foreach (var (key, value) in _previousEnvironment)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldPreferExistingEnvironmentOverEnvFile()
    {
        Track("ConnectionStrings__DefaultConnection");
        Track("Jwt__Key");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "already-set");
        Environment.SetEnvironmentVariable("Jwt__Key", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        File.WriteAllText(Path.Combine(_root, "docker-compose.yml"), "services: {}");
        File.WriteAllText(
            Path.Combine(_root, ".env"),
            """
            ConnectionStrings__DefaultConnection="Server=localhost;Password=from-file;"
            Jwt__Key=dev-only-signing-key-at-least-32-chars
            """);

        EnvFileLoader.Load();

        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            .Should()
            .Be("already-set");
        Environment.GetEnvironmentVariable("Jwt__Key")
            .Should()
            .Be("dev-only-signing-key-at-least-32-chars");
    }

    [Fact]
    public void Load_ShouldUseEnvExampleWhenEnvFileIsMissingInDevelopment()
    {
        Track("ConnectionStrings__DefaultConnection");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        File.WriteAllText(Path.Combine(_root, "docker-compose.yml"), "services: {}");
        File.WriteAllText(
            Path.Combine(_root, ".env.example"),
            """
            ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=CorePay;User Id=sa;Password=Your_strong_Password123!;TrustServerCertificate=True;MultipleActiveResultSets=true"
            """);

        EnvFileLoader.Load();

        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            .Should()
            .Contain("Your_strong_Password123!");
    }

    [Fact]
    public void Load_ShouldSkipEnvExampleInTesting()
    {
        Track("ConnectionStrings__DefaultConnection");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        File.WriteAllText(Path.Combine(_root, "docker-compose.yml"), "services: {}");
        File.WriteAllText(
            Path.Combine(_root, ".env.example"),
            """
            ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=CorePay;User Id=sa;Password=Your_strong_Password123!;TrustServerCertificate=True;MultipleActiveResultSets=true"
            """);

        EnvFileLoader.Load();

        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            .Should()
            .BeNull();
    }

    private void Track(string key)
    {
        if (!_previousEnvironment.ContainsKey(key))
        {
            _previousEnvironment[key] = Environment.GetEnvironmentVariable(key);
        }
    }
}

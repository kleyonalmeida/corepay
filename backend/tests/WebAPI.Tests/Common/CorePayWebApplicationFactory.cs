using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using WebAPI.Tests.Seed;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebAPI.Tests.Common;

public class CorePayWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "CorePay-Test-Signing-Key-At-Least-32-Chars";
    public const string TestFacilitiesWebhookSecret = "Facilities-Test-Webhook-Secret-At-Least-32-Chars";
    public const string SuperAdminEmail = "superadmin@corepay.test";
    public const string SuperAdminPassword = "TestPassword123!";

    protected virtual bool LoadFixtures => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ApplyCommonSettings(builder);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        if (LoadFixtures)
        {
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var seeder = new DevelopmentFixtureSeeder(dbContext);
            seeder.SeedAsync().GetAwaiter().GetResult();
        }

        return host;
    }

    public static void ApplyCommonSettings(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(WebHostDefaults.EnvironmentKey, "Testing");
        builder.UseSetting("Jwt:Issuer", "CorePay");
        builder.UseSetting("Jwt:Audience", "CorePay");
        builder.UseSetting("Jwt:Key", TestSigningKey);
        builder.UseSetting("Jwt:ExpirationMinutes", "60");
        builder.UseSetting("Seed:SuperAdmin:Email", SuperAdminEmail);
        builder.UseSetting("Seed:SuperAdmin:Password", SuperAdminPassword);
        builder.UseSetting("Seed:RoleUsersPassword", "DemoRolePassword123!");
        builder.UseSetting("Facilities:WebhookSecret", TestFacilitiesWebhookSecret);
    }
}

public sealed class CorePayWithoutFixturesWebApplicationFactory : CorePayWebApplicationFactory
{
    protected override bool LoadFixtures => false;
}

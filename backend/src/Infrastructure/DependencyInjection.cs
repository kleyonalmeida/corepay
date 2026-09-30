using Core.Application.Admin;
using Core.Application.AnalystMetrics;
using Core.Application.Collaborators;
using Core.Application.MasterData;
using Core.Application.Finance;
using Core.Application.Payrolls;
using Core.Application.Cashflow;
using Core.Application.Dashboard;
using Core.Application.Reports;
using Core.Application.Notifications;
using Core.Application.Revenues;
using Core.Application.Traffic;
using Infrastructure.Admin;
using Infrastructure.AnalystMetrics;
using Infrastructure.Collaborators;
using Infrastructure.Finance;
using Infrastructure.MasterData;
using Infrastructure.Payrolls;
using Infrastructure.Cashflow;
using Infrastructure.Dashboard;
using Infrastructure.Reports;
using Infrastructure.Notifications;
using Infrastructure.Revenues;
using Infrastructure.Traffic;
using Infrastructure.Identity;
using Infrastructure.Seed;
using Infrastructure.Facilities;
using Infrastructure.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? hostEnvironment = null)
    {
        services.AddSingleton<TestingInMemoryDatabase>();
        services.AddMemoryCache();
        services.AddScoped<RequestPerformanceMetrics>();
        services.AddScoped<QueryPerformanceInterceptor>();
        services.AddScoped<EntryCalculationContextLoader>();
        services.AddScoped<MasterDataCache>();

        services
            .AddOptions<SeedOptions>()
            .Configure(options =>
            {
                var bound = SeedOptionsConfiguration.BindSeedOptions(configuration);
                options.SuperAdmin = bound.SuperAdmin;
                options.LoadFixtures = bound.LoadFixtures;
                options.LoadDemoData = bound.LoadDemoData;
                options.LoadLegacyData = bound.LoadLegacyData;
                options.LegacyDataDirectory = bound.LegacyDataDirectory;
                options.RoleUsersPassword = bound.RoleUsersPassword;
            });

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var interceptor = serviceProvider.GetRequiredService<QueryPerformanceInterceptor>();
            options.AddInterceptors(interceptor);

            if (hostEnvironment.IsEnvironment("Testing"))
            {
                var databaseName = serviceProvider
                    .GetRequiredService<TestingInMemoryDatabase>()
                    .Name;
                options.UseInMemoryDatabase(databaseName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            }
            else
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            }
        });

        services
            .AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IdentityDataSeeder>();
        services.AddScoped<DevelopmentFixtureSeeder>();
        services.AddScoped<DevelopmentDemoDataSeeder>();
        services.AddScoped<IAdminIdentityStore, AdminIdentityStore>();
        services.AddScoped<IMasterDataStore, MasterDataStore>();
        services.AddScoped<ICollaboratorStore, CollaboratorStore>();
        services.AddScoped<IPayrollStore, PayrollStore>();
        services.AddScoped<IFinanceStore, FinanceStore>();
        services.AddScoped<IDashboardStore, DashboardStore>();
        services.AddScoped<IPayrollReportStore, PayrollReportStore>();
        services.AddScoped<IPayrollReportXlsxExporter, PayrollReportXlsxExporter>();
        services.AddScoped<IProjectRevenueStore, ProjectRevenueStore>();
        services.AddScoped<IAnalystMetricStore, AnalystMetricStore>();
        services.AddScoped<ICashflowStore, CashflowStore>();
        services.AddScoped<ITrafficInvestmentStore, TrafficInvestmentStore>();
        services.AddScoped<INotificationStore, NotificationStore>();
        services.AddSingleton(TimeProvider.System);

        var facilitiesOptionsBuilder = services
            .AddOptions<FacilitiesOptions>()
            .Bind(configuration.GetSection(FacilitiesOptions.SectionName));

        services.AddSingleton<IValidateOptions<FacilitiesOptions>, FacilitiesOptionsValidator>();

        if (hostEnvironment is null || !hostEnvironment.IsEnvironment("Testing"))
        {
            facilitiesOptionsBuilder.ValidateOnStart();
        }

        services.AddSingleton<IFacilitiesWebhookSignatureValidator, FacilitiesWebhookSignatureValidator>();

        return services;
    }
}

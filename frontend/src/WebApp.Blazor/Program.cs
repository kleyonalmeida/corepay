using InfiniLore.Lucide;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebApp.Blazor;
using WebApp.Blazor.Auth;
using WebApp.Blazor.Services;
using SessionIdleOptions = WebApp.Blazor.Services.SessionIdleOptions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

#if DEBUG
// O client WASM pode resolver HostEnvironment como Production; mesclar overrides locais explicitamente.
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
#endif

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? (builder.HostEnvironment.IsDevelopment() ? "http://localhost:5000" : null)
    ?? "http://localhost:5000";

builder.Services.AddCorePayAuth();
builder.Services.Configure<SessionIdleOptions>(
    builder.Configuration.GetSection(SessionIdleOptions.SectionName));
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<SidebarState>();
builder.Services.AddScoped<IDashboardApiService, DashboardApiService>();
builder.Services.AddScoped<IReportsApiService, ReportsApiService>();
builder.Services.AddScoped<IFileDownloadService, FileDownloadService>();
builder.Services.AddScoped<IPayrollApiService, PayrollApiService>();
builder.Services.AddScoped<IFinanceApiService, FinanceApiService>();
builder.Services.AddScoped<IDepartmentApiService, DepartmentApiService>();
builder.Services.AddScoped<ICareerLevelApiService, CareerLevelApiService>();
builder.Services.AddScoped<IProjectApiService, ProjectApiService>();
builder.Services.AddScoped<IProjectRevenueApiService, ProjectRevenueApiService>();
builder.Services.AddScoped<IAnalystMetricApiService, AnalystMetricApiService>();
builder.Services.AddScoped<ICashflowApiService, CashflowApiService>();
builder.Services.AddScoped<IPaymentMethodApiService, PaymentMethodApiService>();
builder.Services.AddScoped<ITrafficInvestmentApiService, TrafficInvestmentApiService>();
builder.Services.AddScoped<IUserApiService, UserApiService>();
builder.Services.AddScoped<IRoleApiService, RoleApiService>();
builder.Services.AddScoped<ICollaboratorApiService, CollaboratorApiService>();
builder.Services.AddScoped<INotificationApiService, NotificationApiService>();
builder.Services.AddScoped<NotificationState>();
builder.Services.AddLucideIcons();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});

var host = builder.Build();

var authService = host.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();

await host.RunAsync();

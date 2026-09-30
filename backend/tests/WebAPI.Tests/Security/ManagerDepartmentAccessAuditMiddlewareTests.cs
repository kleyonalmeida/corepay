using System.Net;
using FluentAssertions;
using Infrastructure.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAPI.Tests.Common;
using WebAPI.Tests.Payroll;

namespace WebAPI.Tests.Security;

[Collection("WebApiIntegration")]
public sealed class ManagerDepartmentAccessAuditMiddlewareTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public ManagerDepartmentAccessAuditMiddlewareTests(CorePayWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task ManagerCrossDepartmentAccess_ShouldEmitAuditLogWithoutSensitiveData()
    {
        var provider = new TestLoggerProvider();
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddProvider(provider);
                });
            }));

        var commercialDepartmentId = await AuthTestHelper.GetDepartmentIdBySeedKeyAsync(
            factory.Services,
            SeedKeys.Departments.CommercialAnalysts);

        var trafficPayroll = await PayrollTestHelper.CreatePaidPayrollAsync(
            factory.Services,
            SeedKeys.Departments.PaidTraffic,
            SeedKeys.Collaborators.PaidTrafficInactive,
            month: 12,
            year: 2030);

        var sensitivePix = "11999998888";
        var sensitivePassword = "SuperSecretPassword123!";

        var client = factory.CreateClient();
        var (_, _, token) = await AuthTestHelper.CreateManagerWithDepartmentsAsync(
            factory,
            commercialDepartmentId);
        AuthTestHelper.SetBearerToken(client, token);

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/payrolls/{trafficPayroll.Id}?pix={sensitivePix}&password={sensitivePassword}");
        request.Headers.Add("X-Sensitive-Header", sensitivePassword);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        provider.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("Manager department access blocked", StringComparison.Ordinal));

        var auditMessage = provider.Entries
            .Where(entry => entry.Level == LogLevel.Warning
                && entry.Message.Contains("Manager department access blocked", StringComparison.Ordinal))
            .Select(entry => entry.Message)
            .Single();
        auditMessage.Should().NotContain(sensitivePix);
        auditMessage.Should().NotContain(sensitivePassword);
        auditMessage.Should().NotContain("pix=");
        auditMessage.Should().NotContain("password=");
    }

    private sealed class TestLoggerProvider : ILoggerProvider
    {
        public List<TestLogEntry> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new TestLogger(Entries);

        public void Dispose()
        {
        }
    }

    private sealed class TestLogger(List<TestLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Add(new TestLogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record TestLogEntry(LogLevel Level, string Message);
}

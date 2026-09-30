using Core.Domain.PayrollCalculation;
using FluentAssertions;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Payroll;

public class PayrollCalculatorDiTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public PayrollCalculatorDiTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ServiceProvider_ShouldResolvePayrollCalculator_WithoutDbContext()
    {
        using var scope = _factory.Services.CreateScope();

        var calculator = scope.ServiceProvider.GetRequiredService<PayrollCalculator>();

        calculator.Should().NotBeNull();
        scope.ServiceProvider.GetService<AppDbContext>().Should().NotBeNull();
    }
}

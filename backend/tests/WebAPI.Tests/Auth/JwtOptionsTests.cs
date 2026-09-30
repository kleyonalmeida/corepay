using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Auth;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Auth;

[Collection("WebApiIntegration")]
public class JwtOptionsTests : IClassFixture<CorePayWebApplicationFactory>
{
    private readonly CorePayWebApplicationFactory _factory;

    public JwtOptionsTests(CorePayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void JwtOptions_ShouldBindFromConfiguration()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtOptions>>().Value;

        options.Issuer.Should().Be("CorePay");
        options.Audience.Should().Be("CorePay");
        options.Key.Should().Be(CorePayWebApplicationFactory.TestSigningKey);
        options.ExpirationMinutes.Should().Be(60);
    }

    [Fact]
    public async Task Authentication_ShouldRegisterJwtBearerScheme()
    {
        using var scope = _factory.Services.CreateScope();
        var schemeProvider = scope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();

        var scheme = await schemeProvider.GetSchemeAsync(JwtOptions.SchemeName);

        scheme.Should().NotBeNull();
        scheme!.Name.Should().Be(JwtOptions.SchemeName);
    }
}

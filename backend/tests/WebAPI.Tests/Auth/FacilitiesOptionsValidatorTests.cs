using FluentAssertions;
using Infrastructure.Facilities;
using Microsoft.Extensions.Options;
using WebAPI.Tests.Common;

namespace WebAPI.Tests.Auth;

public class FacilitiesOptionsValidatorTests
{

    [Fact]
    public void Validate_MissingSecret_ShouldFail()
    {
        var result = FacilitiesOptionsValidator.ValidateSecret(null);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain("Facilities:WebhookSecret is required.");
    }

    [Fact]
    public void Validate_ShortSecret_ShouldFail()
    {
        var result = FacilitiesOptionsValidator.ValidateSecret("too-short");

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain("Facilities:WebhookSecret must be at least 32 characters long.");
    }

    [Fact]
    public void Validate_ValidSecret_ShouldSucceed()
    {
        var result = FacilitiesOptionsValidator.ValidateSecret(
            CorePayWebApplicationFactory.TestFacilitiesWebhookSecret);

        result.Succeeded.Should().BeTrue();
    }
}

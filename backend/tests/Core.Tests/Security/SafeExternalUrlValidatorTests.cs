using Core.Security;
using FluentAssertions;

namespace Core.Tests.Security;

public sealed class SafeExternalUrlValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrWhitespace_ShouldReturnNull(string? url)
    {
        var result = SafeExternalUrlValidator.Validate(url);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Theory]
    [InlineData("https://cdn.example.com/avatar.png")]
    [InlineData("https://images.example.com/path/to/file.jpg?size=100")]
    public void Validate_ValidHttpsUrl_ShouldSucceed(string url)
    {
        var result = SafeExternalUrlValidator.Validate(url);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(url);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/file")]
    [InlineData("http://example.com/image.png")]
    [InlineData("/relative/path.png")]
    public void Validate_UnsafeUrl_ShouldFail(string url)
    {
        SafeExternalUrlValidator.Validate(url).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Validate_UrlWithCredentials_ShouldFail()
    {
        SafeExternalUrlValidator.Validate("https://user:pass@example.com/image.png")
            .IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Validate_DisallowedHost_ShouldFail()
    {
        SafeExternalUrlValidator.Validate(
                "https://evil.example.com/image.png",
                ["cdn.example.com"])
            .IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Validate_AllowedHost_ShouldSucceed()
    {
        SafeExternalUrlValidator.Validate(
                "https://cdn.example.com/image.png",
                ["cdn.example.com"])
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_TooLongUrl_ShouldFail()
    {
        var url = "https://example.com/" + new string('a', SafeExternalUrlValidator.MaxUrlLength);
        SafeExternalUrlValidator.Validate(url).IsSuccess.Should().BeFalse();
    }
}

using BuildingBlocks.Results;
using FluentAssertions;

namespace BuildingBlocks.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_ShouldBeSuccessfulWithoutError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCarryError()
    {
        var error = Error.Validation("invalid", "Invalid input");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void GenericSuccess_ShouldExposeValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_ShouldNotExposeValue()
    {
        var error = Error.NotFound("missing", "Not found");

        var result = Result<int>.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Invoking(r => _ = r.Value).Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(ErrorCategory.Validation)]
    [InlineData(ErrorCategory.NotFound)]
    [InlineData(ErrorCategory.Conflict)]
    [InlineData(ErrorCategory.Unauthorized)]
    [InlineData(ErrorCategory.Forbidden)]
    public void Error_ShouldPreserveCategory(ErrorCategory category)
    {
        var error = new Error(category, "code", "message");

        error.Category.Should().Be(category);
        error.Code.Should().Be("code");
        error.Message.Should().Be("message");
    }
}

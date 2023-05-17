using System.Net;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests;

public class OperatorTests
{
    private Result GetFailedResult()
    {
        return Result.Fail(Error.Create(HttpStatusCode.Unauthorized));
    }

    [Fact]
    public void ConvertToGenericResult_FromFailedResultWithoutError_CastToFailedResult()
    {
        // Arrange
        var result = Result.Fail();

        // Act
        Result<int> genericResult = result;

        // Assert
        genericResult.IsFailed.Should().BeTrue();
        genericResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ConvertToGenericResult_FromFailedResult_WhenResultWithError_CastToFailedResult()
    {
        // Arrange
        var result = Result.Fail(Error.Create(HttpStatusCode.Unauthorized));

        // Act
        Result<int> genericResult = result;

        // Assert
        genericResult.IsFailed.Should().BeTrue();
        genericResult.IsSuccess.Should().BeFalse();
        genericResult.GetError().Should().NotBeNull();
        genericResult.GetError().Value.ErrorCode.Should().Be(nameof(HttpStatusCode.Unauthorized));
    }

    [Fact]
    public void ConvertToGenericResult_FromFailedResult_WhenResultWithErrorAndReturnedFromMethod_CastToFailedResult()
    {
        // Arrange
        var result = Result.Fail(Error.Create(HttpStatusCode.Unauthorized));

        // Act
        Result<int> genericResult = GetFailedResult();

        // Assert
        genericResult.IsFailed.Should().BeTrue();
        genericResult.IsSuccess.Should().BeFalse();
        genericResult.GetError().Should().NotBeNull();
        genericResult.GetError().Value.ErrorCode.Should().Be(nameof(HttpStatusCode.Unauthorized));
    }
}
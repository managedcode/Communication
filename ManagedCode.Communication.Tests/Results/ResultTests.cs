using System;
using System.Net;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Succeed_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Succeed();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();
        result.Problem.Should().BeNull();
    }

    [Fact]
    public void Fail_WithMessage_ShouldCreateFailedResult()
    {
        // Arrange
        const string title = "Operation failed";
        const string detail = "Something went wrong";

        // Act
        var result = Result.Fail(title, detail, HttpStatusCode.BadRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.Title.Should().Be(title);
        result.Problem.Detail.Should().Be(detail);
        result.Problem.StatusCode.Should().Be(400);
    }

    [Fact]
    public void Fail_WithProblem_ShouldCreateFailedResult()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/400", "Bad Request", 400, "Invalid input");

        // Act
        var result = Result.Fail(problem);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void FailValidation_ShouldCreateValidationResult()
    {
        // Act
        var result = Result.FailValidation(
            ("email", "Email is required"),
            ("age", "Age must be greater than 0")
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        result.Problem.Title.Should().Be("Validation Failed");
        
        var validationErrors = result.Problem.GetValidationErrors();
        validationErrors.Should().NotBeNull();
        validationErrors!["email"].Should().Contain("Email is required");
        validationErrors["age"].Should().Contain("Age must be greater than 0");
    }

    [Fact]
    public void FailNotFound_ShouldCreateNotFoundResult()
    {
        // Act
        var result = Result.FailNotFound("Resource not found");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(404);
        result.Problem.Detail.Should().Be("Resource not found");
    }

    [Fact]
    public void FailUnauthorized_ShouldCreateUnauthorizedResult()
    {
        // Act
        var result = Result.FailUnauthorized("Authentication required");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(401);
        result.Problem.Detail.Should().Be("Authentication required");
    }

    [Fact]
    public void FailForbidden_ShouldCreateForbiddenResult()
    {
        // Act
        var result = Result.FailForbidden("Access denied");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(403);
        result.Problem.Detail.Should().Be("Access denied");
    }

    [Fact]
    public void ThrowIfFail_WithSuccessfulResult_ShouldNotThrow()
    {
        // Arrange
        var result = Result.Succeed();

        // Act & Assert
        result.Invoking(r => r.ThrowIfFail()).Should().NotThrow();
    }

    [Fact]
    public void ThrowIfFail_WithFailedResult_ShouldThrow()
    {
        // Arrange
        var result = Result.Fail("Operation failed", "Something went wrong", HttpStatusCode.BadRequest);

        // Act & Assert
        result.Invoking(r => r.ThrowIfFail())
            .Should().Throw<Exception>()
            .WithMessage("Operation failed - Something went wrong - (HTTP 400)");
    }

    [Fact]
    public void ImplicitOperator_FromBool_True_ShouldCreateSuccessfulResult()
    {
        // Act
        Result result = true;

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ImplicitOperator_FromBool_False_ShouldCreateFailedResult()
    {
        // Act
        Result result = false;

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ImplicitOperator_ToBool_ShouldReturnIsSuccess()
    {
        // Arrange
        var successResult = Result.Succeed();
        var failResult = Result.Fail("Failed", "Failed");

        // Act & Assert
        ((bool)successResult).Should().BeTrue();
        ((bool)failResult).Should().BeFalse();
    }

    [Fact]
    public void From_WithException_ShouldCreateFailedResult()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        Func<Result> func = () => throw exception;
        var result = Result.From(func);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.Detail.Should().Be("Test exception");
    }

    [Fact]
    public void Try_WithSuccessfulAction_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var executed = false;

        // Act
        var result = Result.Try(() => executed = true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public void Try_WithExceptionThrowingAction_ShouldCreateFailedResult()
    {
        // Act
        var result = Result.Try(() => throw new InvalidOperationException("Test exception"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.Detail.Should().Be("Test exception");
    }
}
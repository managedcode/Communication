using System;
using System.Net;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ResultTTests
{
    [Fact]
    public void Succeed_WithValue_ShouldCreateSuccessfulResult()
    {
        // Arrange
        const string value = "test value";

        // Act
        var result = Result<string>.Succeed(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();
        result.Value.Should().Be(value);
        result.Problem.Should().BeNull();
    }

    [Fact]
    public void Fail_WithMessage_ShouldCreateFailedResult()
    {
        // Arrange
        const string title = "Operation failed";
        const string detail = "Something went wrong";

        // Act
        var result = Result<string>.Fail(title, detail, HttpStatusCode.BadRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailed.Should().BeTrue();
        result.Value.Should().BeNull();
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
        var result = Result<string>.Fail(problem);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailed.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void FailValidation_ShouldCreateValidationResult()
    {
        // Act
        var result = Result<string>.FailValidation(
            ("email", "Email is required"),
            ("age", "Age must be greater than 0")
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
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
        var result = Result<string>.FailNotFound("Resource not found");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(404);
        result.Problem.Detail.Should().Be("Resource not found");
    }

    [Fact]
    public void ImplicitOperator_FromValue_ShouldCreateSuccessfulResult()
    {
        // Arrange
        const string value = "test";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void ImplicitOperator_FromProblem_ShouldCreateFailedResult()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/400", "Bad Request", 400, "Invalid input");

        // Act
        Result<string> result = problem;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void ImplicitOperator_ToBool_ShouldReturnIsSuccess()
    {
        // Arrange
        var successResult = Result<string>.Succeed("test");
        var failResult = Result<string>.Fail("Failed", "Failed");

        // Act & Assert
        ((bool)successResult).Should().BeTrue();
        ((bool)failResult).Should().BeFalse();
    }

    [Fact]
    public void Map_WithSuccessfulResult_ShouldTransformValue()
    {
        // Arrange
        var result = Result<int>.Succeed(5);

        // Act
        var mappedResult = result.Map(x => x.ToString());

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Value.Should().Be("5");
    }

    [Fact]
    public void Map_WithFailedResult_ShouldReturnFailedResult()
    {
        // Arrange
        var result = Result<int>.Fail("Failed", "Failed");

        // Act
        var mappedResult = result.Map(x => x.ToString());

        // Assert
        mappedResult.IsSuccess.Should().BeFalse();
        mappedResult.Problem.Should().Be(result.Problem);
    }

    [Fact]
    public void Bind_WithSuccessfulResult_ShouldExecuteFunction()
    {
        // Arrange
        var result = Result<int>.Succeed(5);

        // Act
        var boundResult = result.Bind(x => Result<string>.Succeed(x.ToString()));

        // Assert
        boundResult.IsSuccess.Should().BeTrue();
        boundResult.Value.Should().Be("5");
    }

    [Fact]
    public void Bind_WithFailedResult_ShouldReturnFailedResult()
    {
        // Arrange
        var result = Result<int>.Fail("Failed", "Failed");

        // Act
        var boundResult = result.Bind(x => Result<string>.Succeed(x.ToString()));

        // Assert
        boundResult.IsSuccess.Should().BeFalse();
        boundResult.Problem.Should().Be(result.Problem);
    }

    [Fact]
    public void Tap_WithSuccessfulResult_ShouldExecuteAction()
    {
        // Arrange
        var result = Result<int>.Succeed(5);
        var executed = false;

        // Act
        var tappedResult = result.Tap(x => executed = true);

        // Assert
        tappedResult.Should().Be(result);
        executed.Should().BeTrue();
    }

    [Fact]
    public void Match_ShouldExecuteCorrectFunction()
    {
        // Arrange
        var successResult = Result<int>.Succeed(5);
        var failResult = Result<int>.Fail("Failed", "Failed");

        // Act
        var successOutput = successResult.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: p => $"Failed: {p.Detail}"
        );

        var failOutput = failResult.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: p => $"Failed: {p.Detail}"
        );

        // Assert
        successOutput.Should().Be("Success: 5");
        failOutput.Should().Be("Failed: Failed");
    }

    [Fact]
    public void From_WithFunc_ShouldExecuteAndWrapResult()
    {
        // Act
        Func<int> func = () => 42;
        var result = Result<int>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void From_WithExceptionThrowingFunc_ShouldCreateFailedResult()
    {
        // Act
        Func<int> func = () => throw new InvalidOperationException("Test exception");
        var result = Result<int>.From(func);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.Detail.Should().Be("Test exception");
    }

}
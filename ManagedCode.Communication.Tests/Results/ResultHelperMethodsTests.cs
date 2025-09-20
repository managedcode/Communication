using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Results.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Communication.Tests.Results;

public class ResultHelperMethodsTests
{
    [Fact]
    public void Result_TryGetProblem_WithSuccess_ShouldReturnFalse()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.Should().BeFalse();
        problem.Should().BeNull();
    }

    [Fact]
    public void Result_TryGetProblem_WithFailure_ShouldReturnTrueAndProblem()
    {
        // Arrange
        var expectedProblem = Problem.Create("type", "title", 400, "detail");
        var result = Result.Fail(expectedProblem);

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.Should().BeTrue();
        problem.Should().NotBeNull();
        problem.Should().Be(expectedProblem);
    }

    [Fact]
    public void Result_ThrowIfFail_WithSuccess_ShouldReturnFalse()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var threw = result.ThrowIfFail();

        // Assert
        threw.Should().BeFalse();
    }

    [Fact]
    public void Result_ThrowIfFail_WithFailure_ShouldThrowProblemException()
    {
        // Arrange
        var problem = Problem.Create("Server Error", "Internal error", 500, "https://httpstatuses.io/500");
        var result = Result.Fail(problem);

        // Act & Assert
        var action = () => result.ThrowIfFail();
        action.Should().Throw<ProblemException>()
            .Where(ex => ex.Problem == problem);
    }

    [Fact]
    public void ResultT_TryGetProblem_WithSuccess_ShouldReturnFalse()
    {
        // Arrange
        var result = Result<int>.Succeed(42);

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.Should().BeFalse();
        problem.Should().BeNull();
    }

    [Fact]
    public void ResultT_TryGetProblem_WithFailure_ShouldReturnTrueAndProblem()
    {
        // Arrange
        var expectedProblem = Problem.Create("type", "title", 404, "not found");
        var result = Result<string>.Fail(expectedProblem);

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.Should().BeTrue();
        problem.Should().NotBeNull();
        problem.Should().Be(expectedProblem);
    }

    [Fact]
    public void ResultT_ThrowIfFail_WithSuccess_ShouldReturnFalse()
    {
        // Arrange
        var result = Result<string>.Succeed("test");

        // Act
        var threw = result.ThrowIfFail();

        // Assert
        threw.Should().BeFalse();
    }

    [Fact]
    public void ResultT_ThrowIfFail_WithFailure_ShouldThrowProblemException()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/403", "Forbidden", 403, "Access denied");
        var result = Result<string>.Fail(problem);

        // Act & Assert
        var action = () => result.ThrowIfFail();
        action.Should().Throw<ProblemException>()
            .Where(ex => ex.Problem == problem);
    }

    [Fact]
    public void Result_Try_WithSuccessfulAction_ShouldReturnSuccess()
    {
        // Arrange
        var executed = false;

        // Act
        var result = Result.Try(() => { executed = true; });

        // Assert
        result.IsSuccess.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public void Result_Try_WithException_ShouldReturnFailure()
    {
        // Act
        var result = Result.Try(() => throw new InvalidOperationException("Test error"));

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.Detail.Should().Be("Test error");
        result.Problem.StatusCode.Should().Be(500);
    }

    [Fact]
    public void Result_Try_WithCustomStatusCode_ShouldUseProvidedCode()
    {
        // Act
        var result = Result.Try(
            () => throw new UnauthorizedAccessException("Access denied"),
            HttpStatusCode.Forbidden
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem!.StatusCode.Should().Be(403);
    }

    [Fact]
    public void ResultT_Try_WithSuccessfulFunc_ShouldReturnSuccessWithValue()
    {
        // Act
        var result = Result.Try<int>(() => 42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultT_Try_WithException_ShouldReturnFailure()
    {
        // Act
        var result = Result.Try<string>(() => throw new ArgumentException("Invalid arg"));

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Problem.Should().NotBeNull();
        result.Problem!.Detail.Should().Be("Invalid arg");
    }

    [Fact]
    public async Task Result_TryAsync_WithSuccessfulTask_ShouldReturnSuccess()
    {
        // Arrange
        var executed = false;

        // Act
        var result = await Result.TryAsync(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task Result_TryAsync_WithException_ShouldReturnFailure()
    {
        // Act
        var result = await Result.TryAsync(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Async error");
        });

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.Detail.Should().Be("Async error");
    }

    [Fact]
    public async Task ResultT_TryAsync_WithSuccessfulTask_ShouldReturnSuccessWithValue()
    {
        // Act
        var result = await Result.TryAsync<string>(async () =>
        {
            await Task.Delay(1);
            return "async result";
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("async result");
    }

    [Fact]
    public async Task ResultT_TryAsync_WithException_ShouldReturnFailure()
    {
        // Act
        var result = await Result.TryAsync<int>(async () =>
        {
            await Task.Delay(1);
            throw new DivideByZeroException("Cannot divide");
        });

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Value.Should().Be(0);
        result.Problem.Should().NotBeNull();
        result.Problem!.Detail.Should().Be("Cannot divide");
    }

    [Fact]
    public void Result_From_WithSuccessResult_ShouldReturnSuccess()
    {
        // Act
        var result = Result.From(() => Result.Succeed());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Result_From_WithFailedResult_ShouldReturnFailure()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");

        // Act
        var result = Result.From(() => Result.Fail(problem));

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void Result_From_WithException_ShouldReturnFailure()
    {
        // Act
        Func<Result> func = () => throw new InvalidOperationException("From error");
        var result = Result.From(func);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem!.Detail.Should().Be("From error");
    }

    [Fact]
    public async Task Result_From_WithAsyncTask_ShouldReturnSuccess()
    {
        // Act
        var result = await Result.From(async () =>
        {
            await Task.Delay(1);
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ResultT_From_WithSuccessResult_ShouldReturnSuccessWithValue()
    {
        // Act
        var result = Result<int>.From(() => Result<int>.Succeed(100));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(100);
    }

    [Fact]
    public void Result_FailNotFound_ShouldCreateNotFoundResult()
    {
        // Act
        var result = Result.FailNotFound("Resource not found");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(404);
        result.Problem.Title.Should().Be("Not Found");
        result.Problem.Detail.Should().Be("Resource not found");
    }

    [Fact]
    public void ResultT_FailNotFound_ShouldCreateNotFoundResult()
    {
        // Act
        var result = Result<string>.FailNotFound("User not found");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Problem!.StatusCode.Should().Be(404);
        result.Problem.Title.Should().Be("Not Found");
        result.Problem.Detail.Should().Be("User not found");
    }

    [Fact]
    public void Result_FailValidation_ShouldCreateValidationResult()
    {
        // Act
        var result = Result.FailValidation(
            ("username", "Username is required"),
            ("password", "Password is too short")
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem!.StatusCode.Should().Be(400);
        result.Problem.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        
        var errors = result.Problem.GetValidationErrors();
        errors.Should().NotBeNull();
        errors!["username"].Should().Contain("Username is required");
        errors["password"].Should().Contain("Password is too short");
    }

    [Fact]
    public void ResultT_FailValidation_ShouldCreateValidationResult()
    {
        // Act
        var result = Result<int>.FailValidation(
            ("value", "Value must be positive"),
            ("value", "Value must be less than 100")
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Value.Should().Be(0);
        
        var errors = result.Problem!.GetValidationErrors();
        errors!["value"].Should().HaveCount(2);
        errors["value"].Should().Contain("Value must be positive");
        errors["value"].Should().Contain("Value must be less than 100");
    }

    [Fact]
    public void Result_Fail_WithHttpStatusCode_ShouldCreateFailedResult()
    {
        // Act
        var result = Result.Fail("ServiceUnavailable", "Service is temporarily unavailable", HttpStatusCode.ServiceUnavailable);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem!.StatusCode.Should().Be(503);
        result.Problem.Title.Should().Be("ServiceUnavailable");
    }

    [Fact]
    public void ResultT_Fail_WithHttpStatusCode_ShouldCreateFailedResult()
    {
        // Act
        var result = Result<string>.Fail("GatewayTimeout", "Gateway timeout occurred", HttpStatusCode.GatewayTimeout);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem!.StatusCode.Should().Be(504);
        result.Problem.Title.Should().Be("GatewayTimeout");
    }

    [Fact]
    public void Result_Match_WithSuccess_ShouldCallOnSuccess()
    {
        // Arrange
        var result = Result.Succeed();
        var successCalled = false;
        var failureCalled = false;

        // Act
        var output = result.Match(
            onSuccess: () =>
            {
                successCalled = true;
                return "success";
            },
            onFailure: _ =>
            {
                failureCalled = true;
                return "failure";
            }
        );

        // Assert
        successCalled.Should().BeTrue();
        failureCalled.Should().BeFalse();
        output.Should().Be("success");
    }

    [Fact]
    public void Result_Match_WithFailure_ShouldCallOnFailure()
    {
        // Arrange
        var problem = Problem.Create("title", "detail", 400, "type");
        var result = Result.Fail(problem);
        var successCalled = false;
        var failureCalled = false;

        // Act
        var output = result.Match(
            onSuccess: () =>
            {
                successCalled = true;
                return "success";
            },
            onFailure: p =>
            {
                failureCalled = true;
                return p.Title!;
            }
        );

        // Assert
        successCalled.Should().BeFalse();
        failureCalled.Should().BeTrue();
        output.Should().Be("title");
    }

    [Fact]
    public void ResultT_Match_WithSuccess_ShouldCallOnSuccess()
    {
        // Arrange
        var result = Result<int>.Succeed(42);

        // Act
        var output = result.Match(
            onSuccess: value => value * 2,
            onFailure: _ => -1
        );

        // Assert
        output.Should().Be(84);
    }

    [Fact]
    public void ResultT_Match_WithFailure_ShouldCallOnFailure()
    {
        // Arrange
        var result = Result<string>.Fail("Error", "Something went wrong");

        // Act
        var output = result.Match(
            onSuccess: value => value.Length,
            onFailure: problem => problem.StatusCode
        );

        // Assert
        output.Should().Be(500);
    }
}

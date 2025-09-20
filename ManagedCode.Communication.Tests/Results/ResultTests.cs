using System;
using System.Net;
using Shouldly;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Succeed_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Succeed();

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.IsFailed
            .ShouldBeFalse();
        result.Problem
            .ShouldBeNull();
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
        result.IsSuccess
            .ShouldBeFalse();
        result.IsFailed
            .ShouldBeTrue();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.Title
            .ShouldBe(title);
        result.Problem
            .Detail
            .ShouldBe(detail);
        result.Problem
            .StatusCode
            .ShouldBe(400);
    }

    [Fact]
    public void Fail_WithProblem_ShouldCreateFailedResult()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/400", "Bad Request", 400, "Invalid input");

        // Act
        var result = Result.Fail(problem);

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
        result.IsFailed
            .ShouldBeTrue();
        result.Problem
            .ShouldBe(problem);
    }

    [Fact]
    public void FailValidation_ShouldCreateValidationResult()
    {
        // Act
        var result = Result.FailValidation(("email", "Email is required"), ("age", "Age must be greater than 0"));

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.StatusCode
            .ShouldBe(400);
        result.Problem
            .Title
            .ShouldBe("Validation Failed");

        var validationErrors = result.AssertValidationErrors();
        validationErrors.ShouldNotBeNull();
        validationErrors!["email"]
            .ShouldContain("Email is required");
        validationErrors["age"]
            .ShouldContain("Age must be greater than 0");
    }

    [Fact]
    public void FailNotFound_ShouldCreateNotFoundResult()
    {
        // Act
        var result = Result.FailNotFound("Resource not found");

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.StatusCode
            .ShouldBe(404);
        result.Problem
            .Detail
            .ShouldBe("Resource not found");
    }

    [Fact]
    public void FailUnauthorized_ShouldCreateUnauthorizedResult()
    {
        // Act
        var result = Result.FailUnauthorized("Authentication required");

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.StatusCode
            .ShouldBe(401);
        result.Problem
            .Detail
            .ShouldBe("Authentication required");
    }

    [Fact]
    public void FailForbidden_ShouldCreateForbiddenResult()
    {
        // Act
        var result = Result.FailForbidden("Access denied");

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.StatusCode
            .ShouldBe(403);
        result.Problem
            .Detail
            .ShouldBe("Access denied");
    }

    [Fact]
    public void ThrowIfFail_WithSuccessfulResult_ShouldNotThrow()
    {
        // Arrange
        var result = Result.Succeed();

        // Act & Assert
        Should.NotThrow(() => result.ThrowIfFail());
    }

    [Fact]
    public void ThrowIfFail_WithFailedResult_ShouldThrow()
    {
        // Arrange
        var result = Result.Fail("Operation failed", "Something went wrong", HttpStatusCode.BadRequest);

        // Act & Assert
        var exception = Should.Throw<ProblemException>(() => result.ThrowIfFail());
        exception.Problem.Title.ShouldBe("Operation failed");
    }

    [Fact]
    public void ImplicitOperator_FromBool_True_ShouldCreateSuccessfulResult()
    {
        // Act
        Result result = true;

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
    }

    [Fact]
    public void ImplicitOperator_FromBool_False_ShouldCreateFailedResult()
    {
        // Act
        Result result = false;

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
    }

    [Fact]
    public void ImplicitOperator_ToBool_ShouldReturnIsSuccess()
    {
        // Arrange
        var successResult = Result.Succeed();
        var failResult = Result.Fail("Failed", "Failed");

        // Act & Assert
        ((bool)successResult).ShouldBeTrue();
        ((bool)failResult).ShouldBeFalse();
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
        result.IsSuccess
            .ShouldBeFalse();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.Detail
            .ShouldBe("Test exception");
    }

    [Fact]
    public void Try_WithSuccessfulAction_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var executed = false;

        // Act
        var result = Result.Try(() => executed = true);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        executed.ShouldBeTrue();
    }

    [Fact]
    public void Try_WithExceptionThrowingAction_ShouldCreateFailedResult()
    {
        // Act
        var result = Result.Try(() => throw new InvalidOperationException("Test exception"));

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.Detail
            .ShouldBe("Test exception");
    }

    [Fact]
    public void TryGetProblem_WithSuccessfulResult_ShouldReturnFalse()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.ShouldBeFalse();
        problem.ShouldBeNull();
    }

    [Fact]
    public void TryGetProblem_WithFailedResult_ShouldReturnTrueAndProblem()
    {
        // Arrange
        var expectedProblem = Problem.Create("https://httpstatuses.io/400", "Bad Request", 400, "Invalid input");
        var result = Result.Fail(expectedProblem);

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.ShouldBeTrue();
        problem.ShouldNotBeNull();
        problem.ShouldBe(expectedProblem);
    }

    [Fact]
    public void TryGetProblem_WithValidationResult_ShouldReturnTrueAndValidationProblem()
    {
        // Arrange
        var result = Result.FailValidation(("email", "Email is required"));

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.ShouldBeTrue();
        problem.ShouldNotBeNull();
        problem!.Title.ShouldBe("Validation Failed");
        problem.StatusCode.ShouldBe(400);
        
        var validationErrors = problem.GetValidationErrors();
        validationErrors.ShouldNotBeNull();
        validationErrors!["email"].ShouldContain("Email is required");
    }

    [Fact]
    public void ThrowIfFail_WithProblemException_ShouldPreserveProblemDetails()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/404", "Not Found", 404, "User not found");
        var result = Result.Fail(problem);

        // Act & Assert
        var exception = Should.Throw<ProblemException>(() => result.ThrowIfFail());
        exception.Problem.ShouldBe(problem);
    }
}

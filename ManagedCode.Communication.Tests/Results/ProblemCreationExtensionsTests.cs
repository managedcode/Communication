using System;
using System.Net;
using ManagedCode.Communication.Extensions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Results;

public class ProblemCreationExtensionsTests
{
    [Test]
    public void ToProblem_FromException_ShouldCreateProblemWithDefaultStatusCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Operation not allowed");

        // Act
        var problem = exception.ToProblem();

        // Assert
        problem.ShouldNotBeNull();
        problem.Type.ShouldBe("https://httpstatuses.io/500");
        problem.Title.ShouldBe("InvalidOperationException");
        problem.Detail.ShouldBe("Operation not allowed");
        problem.StatusCode.ShouldBe(500);
        problem.ErrorCode.ShouldBe("System.InvalidOperationException");
    }

    [Test]
    public void ToProblem_FromException_WithCustomStatusCode_ShouldUseProvidedStatusCode()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var problem = exception.ToProblem(400);

        // Assert
        problem.StatusCode.ShouldBe(400);
        problem.Type.ShouldBe("https://httpstatuses.io/400");
    }

    [Test]
    public void ToProblem_FromException_WithHttpStatusCode_ShouldUseProvidedStatusCode()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");

        // Act
        var problem = exception.ToProblem(HttpStatusCode.Forbidden);

        // Assert
        problem.StatusCode.ShouldBe(403);
        problem.Type.ShouldBe("https://httpstatuses.io/403");
        problem.Title.ShouldBe("UnauthorizedAccessException");
        problem.Detail.ShouldBe("Access denied");
    }

    [Test]
    public void ToProblem_FromEnum_WithDefaultParameters_ShouldCreateProblem()
    {
        // Act
        var problem = TestError.InvalidInput.ToProblem();

        // Assert
        problem.ShouldNotBeNull();
        problem.Type.ShouldBe("https://httpstatuses.io/400");
        problem.Title.ShouldBe("InvalidInput");
        problem.Detail.ShouldBe("An error occurred: InvalidInput");
        problem.StatusCode.ShouldBe(400);
        problem.ErrorCode.ShouldBe("InvalidInput");
    }

    [Test]
    public void ToProblem_FromEnum_WithCustomDetail_ShouldUseProvidedDetail()
    {
        // Act
        var problem = TestError.ResourceLocked.ToProblem("The resource is locked by another user");

        // Assert
        problem.Detail.ShouldBe("The resource is locked by another user");
        problem.Title.ShouldBe("ResourceLocked");
    }

    [Test]
    public void ToProblem_FromEnum_WithCustomStatusCode_ShouldUseProvidedStatusCode()
    {
        // Act
        var problem = TestError.ResourceLocked.ToProblem("Resource locked", 423);

        // Assert
        problem.StatusCode.ShouldBe(423);
        problem.Type.ShouldBe("https://httpstatuses.io/423");
        problem.Detail.ShouldBe("Resource locked");
    }

    [Test]
    public void ToProblem_FromEnum_WithHttpStatusCode_ShouldUseProvidedStatusCode()
    {
        // Act
        var problem = TestError.InvalidInput.ToProblem("Invalid input data", HttpStatusCode.UnprocessableEntity);

        // Assert
        problem.StatusCode.ShouldBe(422);
        problem.Type.ShouldBe("https://httpstatuses.io/422");
        problem.Detail.ShouldBe("Invalid input data");
        problem.ErrorCode.ShouldBe("InvalidInput");
    }

    [Test]
    public void ToException_FromProblem_ShouldCreateProblemException()
    {
        // Arrange
        var problem = Problem.Create("Conflict", "Resource conflict detected", 409, "https://httpstatuses.io/409");

        // Act
        var exception = problem.ToException();

        // Assert
        exception.ShouldBeOfType<ProblemException>();
        var problemException = (ProblemException)exception;
        problemException.Problem.ShouldBe(problem);
        problemException.StatusCode.ShouldBe(409);
        problemException.Title.ShouldBe("Conflict");
        problemException.Detail.ShouldBe("Resource conflict detected");
    }

    [Test]
    public void ToProblem_FromExceptionWithData_ShouldIncludeDataInExtensions()
    {
        // Arrange
        var exception = new InvalidOperationException("Operation failed");
        exception.Data["UserId"] = 123;
        exception.Data["CorrelationId"] = "abc-123";

        // Act
        var problem = exception.ToProblem();

        // Assert
        problem.Extensions.ShouldContainKey("exception.UserId");
        problem.Extensions["exception.UserId"].ShouldBe(123);
        problem.Extensions.ShouldContainKey("exception.CorrelationId");
        problem.Extensions["exception.CorrelationId"].ShouldBe("abc-123");
    }

    [Test]
    public void ToException_PreservesAllProblemDetails()
    {
        // Arrange
        var problem = Problem.Validation(("field1", "Error 1"), ("field2", "Error 2"));
        problem.Extensions["customData"] = "customValue";

        // Act
        var exception = problem.ToException();

        // Assert
        var problemException = (ProblemException)exception;
        problemException.IsValidationProblem.ShouldBeTrue();
        problemException.ValidationErrors.ShouldNotBeNull();
        problemException.ValidationErrors!["field1"].ShouldContain("Error 1");
        problemException.ValidationErrors["field2"].ShouldContain("Error 2");
        problemException.Data.Contains($"{nameof(Problem)}.{nameof(problem.Extensions)}.customData").ShouldBeTrue();
    }
}

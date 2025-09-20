using System;
using System.Collections.Generic;
using Shouldly;
using ManagedCode.Communication.Constants;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Results;

public class ProblemToExceptionTests
{
    [Fact]
    public void ToException_FromProblemCreatedFromInvalidOperationException_ShouldReconstructOriginalExceptionType()
    {
        // Arrange
        var originalException = new InvalidOperationException("Operation not allowed");
        originalException.Data["UserId"] = 123;
        originalException.Data["CorrelationId"] = "abc-123";
        
        var problem = Problem.FromException(originalException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.ShouldBeOfType<InvalidOperationException>();
        reconstructedException.Message.ShouldBe("Operation not allowed");
        reconstructedException.Data["UserId"].ShouldBe(123);
        reconstructedException.Data["CorrelationId"].ShouldBe("abc-123");
    }

    [Fact]
    public void ToException_FromProblemCreatedFromArgumentException_ShouldReconstructOriginalExceptionType()
    {
        // Arrange
        var originalException = new ArgumentException("Invalid argument provided", "paramName");
        var problem = Problem.FromException(originalException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.ShouldBeOfType<ArgumentException>();
        reconstructedException.Message.ShouldContain("Invalid argument provided");
    }

    [Fact]
    public void ToException_FromProblemCreatedFromNullReferenceException_ShouldReconstructOriginalExceptionType()
    {
        // Arrange
        var originalException = new NullReferenceException("Object reference not set");
        var problem = Problem.FromException(originalException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.ShouldBeOfType<NullReferenceException>();
        reconstructedException.Message.ShouldBe("Object reference not set");
    }

    [Fact]
    public void ToException_FromProblemCreatedManually_ShouldReturnProblemException()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/400", "Bad Request", 400, "Invalid input");

        // Act
        var exception = problem.ToException();

        // Assert
        exception.ShouldBeOfType<ProblemException>();
        var problemException = (ProblemException)exception;
        problemException.Problem.ShouldBe(problem);
    }

    [Fact]
    public void ToException_FromProblemWithoutOriginalType_ShouldReturnProblemException()
    {
        // Arrange
        var problem = Problem.Create("Server Error", "Something went wrong", 500, "https://httpstatuses.io/500");

        // Act
        var exception = problem.ToException();

        // Assert
        exception.ShouldBeOfType<ProblemException>();
        var problemException = (ProblemException)exception;
        problemException.Problem.ShouldBe(problem);
    }

    [Fact]
    public void ToException_WithCustomExceptionType_ShouldHandleGracefully()
    {
        // Arrange
        var originalException = new CustomTestException("Custom error message");
        originalException.Data["CustomKey"] = "CustomValue";
        var problem = Problem.FromException(originalException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.ShouldBeOfType<CustomTestException>();
        reconstructedException.Message.ShouldBe("Custom error message");
        reconstructedException.Data["CustomKey"].ShouldBe("CustomValue");
    }

    [Fact]
    public void ToException_WithInvalidOriginalType_ShouldFallbackToProblemException()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");
        problem.Extensions[ProblemConstants.ExtensionKeys.OriginalExceptionType] = "NonExistent.Exception.Type";

        // Act
        var exception = problem.ToException();

        // Assert
        exception.ShouldBeOfType<ProblemException>();
    }

    [Fact]
    public void ToException_WithNonExceptionType_ShouldFallbackToProblemException()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");
        problem.Extensions[ProblemConstants.ExtensionKeys.OriginalExceptionType] = typeof(string).FullName;

        // Act
        var exception = problem.ToException();

        // Assert
        exception.ShouldBeOfType<ProblemException>();
    }

    [Fact]
    public void ToException_PreservesAllExceptionData()
    {
        // Arrange
        var originalException = new InvalidOperationException("Test error");
        originalException.Data["StringValue"] = "test";
        originalException.Data["IntValue"] = 42;
        originalException.Data["BoolValue"] = true;
        originalException.Data["DateValue"] = DateTime.UtcNow;
        
        var problem = Problem.FromException(originalException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.Data["StringValue"].ShouldBe("test");
        reconstructedException.Data["IntValue"].ShouldBe(42);
        reconstructedException.Data["BoolValue"].ShouldBe(true);
        reconstructedException.Data["DateValue"].ShouldBeOfType<DateTime>();
    }

    [Fact]
    public void ToException_FromValidationProblem_ShouldReturnProblemException()
    {
        // Arrange
        var problem = Problem.Validation(("email", "Email is required"), ("age", "Age must be positive"));

        // Act
        var exception = problem.ToException();

        // Assert
        exception.ShouldBeOfType<ProblemException>();
        var problemException = (ProblemException)exception;
        problemException.IsValidationProblem.ShouldBeTrue();
        problemException.ValidationErrors.ShouldNotBeNull();
    }
}

// Custom exception for testing
public class CustomTestException : Exception
{
    public CustomTestException(string message) : base(message) { }
}

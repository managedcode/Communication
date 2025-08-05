using System;
using System.Collections.Generic;
using FluentAssertions;
using ManagedCode.Communication.Constants;
using Xunit;

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
        reconstructedException.Should().BeOfType<InvalidOperationException>();
        reconstructedException.Message.Should().Be("Operation not allowed");
        reconstructedException.Data["UserId"].Should().Be(123);
        reconstructedException.Data["CorrelationId"].Should().Be("abc-123");
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
        reconstructedException.Should().BeOfType<ArgumentException>();
        reconstructedException.Message.Should().Contain("Invalid argument provided");
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
        reconstructedException.Should().BeOfType<NullReferenceException>();
        reconstructedException.Message.Should().Be("Object reference not set");
    }

    [Fact]
    public void ToException_FromProblemCreatedManually_ShouldReturnProblemException()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/400", "Bad Request", 400, "Invalid input");

        // Act
        var exception = problem.ToException();

        // Assert
        exception.Should().BeOfType<ProblemException>();
        var problemException = (ProblemException)exception;
        problemException.Problem.Should().Be(problem);
    }

    [Fact]
    public void ToException_FromProblemWithoutOriginalType_ShouldReturnProblemException()
    {
        // Arrange
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/500",
            Title = "Server Error",
            StatusCode = 500,
            Detail = "Something went wrong"
        };

        // Act
        var exception = problem.ToException();

        // Assert
        exception.Should().BeOfType<ProblemException>();
        var problemException = (ProblemException)exception;
        problemException.Problem.Should().BeEquivalentTo(problem);
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
        reconstructedException.Should().BeOfType<CustomTestException>();
        reconstructedException.Message.Should().Be("Custom error message");
        reconstructedException.Data["CustomKey"].Should().Be("CustomValue");
    }

    [Fact]
    public void ToException_WithInvalidOriginalType_ShouldFallbackToProblemException()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");
        problem.Extensions[ProblemExtensionKeys.OriginalExceptionType] = "NonExistent.Exception.Type";

        // Act
        var exception = problem.ToException();

        // Assert
        exception.Should().BeOfType<ProblemException>();
    }

    [Fact]
    public void ToException_WithNonExceptionType_ShouldFallbackToProblemException()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");
        problem.Extensions[ProblemExtensionKeys.OriginalExceptionType] = typeof(string).FullName;

        // Act
        var exception = problem.ToException();

        // Assert
        exception.Should().BeOfType<ProblemException>();
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
        reconstructedException.Data["StringValue"].Should().Be("test");
        reconstructedException.Data["IntValue"].Should().Be(42);
        reconstructedException.Data["BoolValue"].Should().Be(true);
        reconstructedException.Data["DateValue"].Should().BeOfType<DateTime>();
    }

    [Fact]
    public void ToException_FromValidationProblem_ShouldReturnProblemException()
    {
        // Arrange
        var problem = Problem.Validation(("email", "Email is required"), ("age", "Age must be positive"));

        // Act
        var exception = problem.ToException();

        // Assert
        exception.Should().BeOfType<ProblemException>();
        var problemException = (ProblemException)exception;
        problemException.IsValidationProblem.Should().BeTrue();
        problemException.ValidationErrors.Should().NotBeNull();
    }
}

// Custom exception for testing
public class CustomTestException : Exception
{
    public CustomTestException(string message) : base(message) { }
}
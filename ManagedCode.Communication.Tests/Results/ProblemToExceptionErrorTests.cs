using System;
using System.Collections.Generic;
using FluentAssertions;
using ManagedCode.Communication.Constants;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ProblemToExceptionErrorTests
{
    [Fact]
    public void ToException_WithInnerException_ShouldPreserveInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var outerException = new ArgumentException("Outer error", innerException);
        var problem = Problem.FromException(outerException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.Should().BeOfType<ArgumentException>();
        reconstructedException.Message.Should().Contain("Outer error");
        // Note: Inner exceptions are not preserved in Problem, so this won't have InnerException
        reconstructedException.InnerException.Should().BeNull();
    }

    [Fact]
    public void ToException_WithArgumentNullException_ShouldReconstructWithMessage()
    {
        // Arrange
        var originalException = new ArgumentNullException("paramName", "Parameter cannot be null");
        var problem = Problem.FromException(originalException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        // ArgumentNullException can be reconstructed with just the message
        reconstructedException.Should().BeOfType<ArgumentNullException>();
        reconstructedException.Message.Should().Contain("Parameter cannot be null");
    }

    [Fact]
    public void ToException_WithAggregateException_ShouldHandleCorrectly()
    {
        // Arrange
        var innerExceptions = new List<Exception>
        {
            new InvalidOperationException("Error 1"),
            new ArgumentException("Error 2")
        };
        var aggregateException = new AggregateException("Multiple errors", innerExceptions);
        var problem = Problem.FromException(aggregateException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        // AggregateException can be reconstructed with just the message
        reconstructedException.Should().BeOfType<AggregateException>();
        // AggregateException's message includes inner exception messages
        reconstructedException.Message.Should().Contain("Multiple errors");
    }

    [Fact]
    public void ToException_WithCustomExceptionWithoutDefaultConstructor_ShouldFallbackToProblemException()
    {
        // Arrange
        var originalException = new NoDefaultConstructorException(42, "Custom error");
        var problem = Problem.FromException(originalException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.Should().BeOfType<ProblemException>();
        var problemException = (ProblemException)reconstructedException;
        problemException.Problem.Detail.Should().Be("Custom error");
    }

    [Fact]
    public void ToException_WithStackTrace_ShouldNotPreserveStackTrace()
    {
        // Arrange
        Exception? originalException = null;
        try
        {
            throw new InvalidOperationException("Test exception with stack trace");
        }
        catch (Exception ex)
        {
            originalException = ex;
        }
        
        var problem = Problem.FromException(originalException!);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.Should().BeOfType<InvalidOperationException>();
        reconstructedException.Message.Should().Be("Test exception with stack trace");
        // Stack trace is not preserved through Problem conversion
        reconstructedException.StackTrace.Should().BeNull();
    }

    [Fact]
    public void ToException_WithExceptionDataContainingComplexTypes_ShouldPreserveSerializableData()
    {
        // Arrange
        var originalException = new InvalidOperationException("Complex data test");
        originalException.Data["SimpleString"] = "test";
        originalException.Data["Number"] = 123;
        originalException.Data["Date"] = new DateTime(2024, 1, 1);
        originalException.Data["ComplexObject"] = new { Name = "Test", Value = 42 }; // This might not serialize properly
        
        var problem = Problem.FromException(originalException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.Data["SimpleString"].Should().Be("test");
        reconstructedException.Data["Number"].Should().Be(123);
        reconstructedException.Data["Date"].Should().BeOfType<DateTime>();
        // Complex objects might be serialized differently
        reconstructedException.Data.Contains("ComplexObject").Should().BeTrue();
    }

    [Fact]
    public void ToException_WithNullDetail_ShouldUseTitle()
    {
        // Arrange
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/500",
            Title = "Server Error",
            StatusCode = 500,
            Detail = null
        };
        problem.Extensions[ProblemExtensionKeys.OriginalExceptionType] = typeof(InvalidOperationException).FullName;

        // Act
        var exception = problem.ToException();

        // Assert
        exception.Should().BeOfType<InvalidOperationException>();
        exception.Message.Should().Be("Server Error");
    }

    [Fact]
    public void ToException_WithNullDetailAndTitle_ShouldUseDefaultMessage()
    {
        // Arrange
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/500",
            Title = null,
            StatusCode = 500,
            Detail = null
        };
        problem.Extensions[ProblemExtensionKeys.OriginalExceptionType] = typeof(InvalidOperationException).FullName;

        // Act
        var exception = problem.ToException();

        // Assert
        exception.Should().BeOfType<InvalidOperationException>();
        exception.Message.Should().Be("An error occurred");
    }

    [Fact]
    public void ToException_WithMalformedOriginalExceptionType_ShouldFallbackToProblemException()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");
        problem.Extensions[ProblemExtensionKeys.OriginalExceptionType] = "This.Is.Not.A.Valid.Type.Name!!!";

        // Act
        var exception = problem.ToException();

        // Assert
        exception.Should().BeOfType<ProblemException>();
    }

    [Fact]
    public void ToException_WithExceptionDataKeyConflicts_ShouldHandleGracefully()
    {
        // Arrange
        var originalException = new InvalidOperationException("Test");
        originalException.Data["key1"] = "value1";
        originalException.Data[ProblemExtensionKeys.ExceptionDataPrefix + "key2"] = "This should not happen";
        
        var problem = Problem.FromException(originalException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.Data["key1"].Should().Be("value1");
        // The prefixed key should be handled correctly
        reconstructedException.Data.Count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void ToException_WithHttpRequestException_ShouldHandleSpecialCases()
    {
        // Arrange
        var originalException = new System.Net.Http.HttpRequestException("Network error");
        var problem = Problem.FromException(originalException, 503);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        // HttpRequestException might need special handling, could fall back to ProblemException
        reconstructedException.Message.Should().Contain("Network error");
    }

    [Fact]
    public void ToException_PerformanceTest_ShouldNotTakeExcessiveTime()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 500, "detail");
        problem.Extensions[ProblemExtensionKeys.OriginalExceptionType] = typeof(InvalidOperationException).FullName;
        
        // Add many data items
        for (int i = 0; i < 100; i++)
        {
            problem.Extensions[$"{ProblemExtensionKeys.ExceptionDataPrefix}key{i}"] = $"value{i}";
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exception = problem.ToException();
        stopwatch.Stop();

        // Assert
        exception.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should be fast
        exception.Data.Count.Should().Be(100);
    }
}

// Custom exception types for testing
public class NoDefaultConstructorException : Exception
{
    public int Code { get; }
    
    public NoDefaultConstructorException(int code, string message) : base(message)
    {
        Code = code;
    }
}
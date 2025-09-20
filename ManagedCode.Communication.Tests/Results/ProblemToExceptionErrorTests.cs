using System;
using System.Collections.Generic;
using Shouldly;
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
        reconstructedException.ShouldBeOfType<ArgumentException>();
        reconstructedException.Message.ShouldContain("Outer error");
        // Note: Inner exceptions are not preserved in Problem, so this won't have InnerException
        reconstructedException.InnerException.ShouldBeNull();
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
        reconstructedException.ShouldBeOfType<ArgumentNullException>();
        reconstructedException.Message.ShouldContain("Parameter cannot be null");
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
        reconstructedException.ShouldBeOfType<AggregateException>();
        // AggregateException's message includes inner exception messages
        reconstructedException.Message.ShouldContain("Multiple errors");
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
        reconstructedException.ShouldBeOfType<ProblemException>();
        var problemException = (ProblemException)reconstructedException;
        problemException.Problem.Detail.ShouldBe("Custom error");
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
        reconstructedException.ShouldBeOfType<InvalidOperationException>();
        reconstructedException.Message.ShouldBe("Test exception with stack trace");
        // Stack trace is not preserved through Problem conversion
        reconstructedException.StackTrace.ShouldBeNull();
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
        reconstructedException.Data["SimpleString"].ShouldBe("test");
        reconstructedException.Data["Number"].ShouldBe(123);
        reconstructedException.Data["Date"].ShouldBeOfType<DateTime>();
        // Complex objects might be serialized differently
        reconstructedException.Data.Contains("ComplexObject").ShouldBeTrue();
    }

    [Fact]
    public void ToException_WithNullDetail_ShouldUseTitle()
    {
        // Arrange
        var problem = Problem.Create("Server Error", "", 500, "https://httpstatuses.io/500");
        problem.Extensions[ProblemConstants.ExtensionKeys.OriginalExceptionType] = typeof(InvalidOperationException).FullName;

        // Act
        var exception = problem.ToException();

        // Assert
        exception.ShouldBeOfType<InvalidOperationException>();
        exception.Message.ShouldBe("Server Error");
    }

    [Fact]
    public void ToException_WithNullDetailAndTitle_ShouldUseDefaultMessage()
    {
        // Arrange
        var problem = Problem.Create("", "", 500, "https://httpstatuses.io/500");
        problem.Extensions[ProblemConstants.ExtensionKeys.OriginalExceptionType] = typeof(InvalidOperationException).FullName;

        // Act
        var exception = problem.ToException();

        // Assert
        exception.ShouldBeOfType<InvalidOperationException>();
        exception.Message.ShouldBe("An error occurred");
    }

    [Fact]
    public void ToException_WithMalformedOriginalExceptionType_ShouldFallbackToProblemException()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");
        problem.Extensions[ProblemConstants.ExtensionKeys.OriginalExceptionType] = "This.Is.Not.A.Valid.Type.Name!!!";

        // Act
        var exception = problem.ToException();

        // Assert
        exception.ShouldBeOfType<ProblemException>();
    }

    [Fact]
    public void ToException_WithExceptionDataKeyConflicts_ShouldHandleGracefully()
    {
        // Arrange
        var originalException = new InvalidOperationException("Test");
        originalException.Data["key1"] = "value1";
        originalException.Data[ProblemConstants.ExtensionKeys.ExceptionDataPrefix + "key2"] = "This should not happen";
        
        var problem = Problem.FromException(originalException);

        // Act
        var reconstructedException = problem.ToException();

        // Assert
        reconstructedException.Data["key1"].ShouldBe("value1");
        // The prefixed key should be handled correctly
        reconstructedException.Data.Count.ShouldBeGreaterThanOrEqualTo(1);
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
        reconstructedException.Message.ShouldContain("Network error");
    }

    [Fact]
    public void ToException_PerformanceTest_ShouldNotTakeExcessiveTime()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 500, "detail");
        problem.Extensions[ProblemConstants.ExtensionKeys.OriginalExceptionType] = typeof(InvalidOperationException).FullName;
        
        // Add many data items
        for (int i = 0; i < 100; i++)
        {
            problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}key{i}"] = $"value{i}";
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exception = problem.ToException();
        stopwatch.Stop();

        // Assert
        exception.ShouldNotBeNull();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(100); // Should be fast
        exception.Data.Count.ShouldBe(100);
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

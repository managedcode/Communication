using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using ManagedCode.Communication.CollectionResultT;

namespace ManagedCode.Communication.Tests.TestHelpers;

/// <summary>
/// Extension methods for testing Result types without using null-forgiving operators.
/// </summary>
public static class ResultTestExtensions
{
    /// <summary>
    /// Asserts that the result has a problem and returns it for further assertions.
    /// </summary>
    public static Problem AssertProblem(this Result result)
    {
        using (new AssertionScope())
        {
            result.HasProblem.Should().BeTrue("result should have a problem");
            if (result.TryGetProblem(out var problem))
            {
                return problem;
            }
            throw new AssertionFailedException("Result has no problem but HasProblem returned true");
        }
    }
    
    /// <summary>
    /// Asserts that the result has a problem and returns it for further assertions.
    /// </summary>
    public static Problem AssertProblem<T>(this Result<T> result)
    {
        using (new AssertionScope())
        {
            result.HasProblem.Should().BeTrue("result should have a problem");
            if (result.TryGetProblem(out var problem))
            {
                return problem;
            }
            throw new AssertionFailedException("Result has no problem but HasProblem returned true");
        }
    }
    
    /// <summary>
    /// Asserts that the result has a problem with validation errors and returns them.
    /// </summary>
    public static Dictionary<string, List<string>> AssertValidationErrors(this Result result)
    {
        var problem = result.AssertProblem();
        var errors = problem.GetValidationErrors();
        errors.Should().NotBeNull("problem should have validation errors");
        return errors ?? new Dictionary<string, List<string>>();
    }
    
    /// <summary>
    /// Asserts that the result has a problem with validation errors and returns them.
    /// </summary>
    public static Dictionary<string, List<string>> AssertValidationErrors<T>(this Result<T> result)
    {
        var problem = result.AssertProblem();
        var errors = problem.GetValidationErrors();
        errors.Should().NotBeNull("problem should have validation errors");
        return errors ?? new Dictionary<string, List<string>>();
    }
    
    /// <summary>
    /// Asserts problem properties in a fluent way.
    /// </summary>
    public static ProblemAssertions ShouldHaveProblem(this Result result)
    {
        return new ProblemAssertions(result.AssertProblem());
    }
    
    /// <summary>
    /// Asserts problem properties in a fluent way.
    /// </summary>
    public static ProblemAssertions ShouldHaveProblem<T>(this Result<T> result)
    {
        return new ProblemAssertions(result.AssertProblem());
    }
    
    /// <summary>
    /// Asserts that result should not have a problem.
    /// </summary>
    public static void ShouldNotHaveProblem(this Result result)
    {
        result.HasProblem.Should().BeFalse("result should not have a problem");
    }
    
    /// <summary>
    /// Asserts that result should not have a problem.
    /// </summary>
    public static void ShouldNotHaveProblem<T>(this Result<T> result)
    {
        result.HasProblem.Should().BeFalse("result should not have a problem");
    }
    
    // CollectionResult extensions
    
    /// <summary>
    /// Asserts that the collection result has a problem and returns it for further assertions.
    /// </summary>
    public static Problem AssertProblem<T>(this CollectionResult<T> result)
    {
        using (new AssertionScope())
        {
            result.HasProblem.Should().BeTrue("collection result should have a problem");
            if (result.TryGetProblem(out var problem))
            {
                return problem;
            }
            throw new AssertionFailedException("CollectionResult has no problem but HasProblem returned true");
        }
    }
    
    /// <summary>
    /// Asserts that the collection result has a problem with validation errors and returns them.
    /// </summary>
    public static Dictionary<string, List<string>> AssertValidationErrors<T>(this CollectionResult<T> result)
    {
        var problem = result.AssertProblem();
        var errors = problem.GetValidationErrors();
        errors.Should().NotBeNull("problem should have validation errors");
        return errors ?? new Dictionary<string, List<string>>();
    }
    
    /// <summary>
    /// Asserts problem properties in a fluent way for CollectionResult.
    /// </summary>
    public static ProblemAssertions ShouldHaveProblem<T>(this CollectionResult<T> result)
    {
        return new ProblemAssertions(result.AssertProblem());
    }
    
    /// <summary>
    /// Asserts that collection result should not have a problem.
    /// </summary>
    public static void ShouldNotHaveProblem<T>(this CollectionResult<T> result)
    {
        result.HasProblem.Should().BeFalse("collection result should not have a problem");
    }
}

/// <summary>
/// Fluent assertions for Problem.
/// </summary>
public class ProblemAssertions
{
    private readonly Problem _problem;
    
    public ProblemAssertions(Problem problem)
    {
        _problem = problem;
    }
    
    public ProblemAssertions WithTitle(string expectedTitle)
    {
        _problem.Title.Should().Be(expectedTitle);
        return this;
    }
    
    public ProblemAssertions WithDetail(string expectedDetail)
    {
        _problem.Detail.Should().Be(expectedDetail);
        return this;
    }
    
    public ProblemAssertions WithStatusCode(int expectedStatusCode)
    {
        _problem.StatusCode.Should().Be(expectedStatusCode);
        return this;
    }
    
    public ProblemAssertions WithErrorCode(string expectedErrorCode)
    {
        _problem.ErrorCode.Should().Be(expectedErrorCode);
        return this;
    }
    
    public ProblemAssertions WithValidationError(string field, string expectedMessage)
    {
        var errors = _problem.GetValidationErrors();
        errors.Should().NotBeNull();
        errors.Should().ContainKey(field);
        
        if (errors != null && errors.TryGetValue(field, out var fieldErrors))
        {
            fieldErrors.Should().Contain(expectedMessage);
        }
        
        return this;
    }
    
    public ProblemAssertions WithValidationErrors(params (string field, string message)[] expectedErrors)
    {
        var errors = _problem.GetValidationErrors();
        errors.Should().NotBeNull();
        
        if (errors != null)
        {
            foreach (var (field, message) in expectedErrors)
            {
                errors.Should().ContainKey(field);
                if (errors.TryGetValue(field, out var fieldErrors))
                {
                    fieldErrors.Should().Contain(message);
                }
            }
        }
        
        return this;
    }
    
    public Dictionary<string, List<string>> GetValidationErrors()
    {
        var errors = _problem.GetValidationErrors();
        errors.Should().NotBeNull();
        return errors ?? new Dictionary<string, List<string>>();
    }
}
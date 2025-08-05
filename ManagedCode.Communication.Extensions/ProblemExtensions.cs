using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.Extensions;

/// <summary>
/// Extension methods for Problem and ProblemDetails conversion.
/// </summary>
public static class ProblemExtensions
{
    /// <summary>
    /// Converts Problem to ProblemDetails.
    /// </summary>
    public static ProblemDetails ToProblemDetails(this ManagedCode.Communication.Problem problem)
    {
        var problemDetails = new ProblemDetails
        {
            Type = problem.Type,
            Title = problem.Title,
            Status = problem.StatusCode == 0 ? null : problem.StatusCode,
            Detail = problem.Detail,
            Instance = problem.Instance
        };
        
        foreach (var kvp in problem.Extensions)
        {
            problemDetails.Extensions[kvp.Key] = kvp.Value;
        }
        
        return problemDetails;
    }
    
    /// <summary>
    /// Creates a Problem from ProblemDetails.
    /// </summary>
    public static ManagedCode.Communication.Problem FromProblemDetails(ProblemDetails problemDetails)
    {
        var problem = new ManagedCode.Communication.Problem
        {
            Type = problemDetails.Type,
            Title = problemDetails.Title,
            StatusCode = problemDetails.Status ?? 0,
            Detail = problemDetails.Detail,
            Instance = problemDetails.Instance
        };
        
        foreach (var kvp in problemDetails.Extensions)
        {
            problem.Extensions[kvp.Key] = kvp.Value;
        }
        
        return problem;
    }
    
    /// <summary>
    /// Converts Problem to ProblemDetails.
    /// </summary>
    public static ProblemDetails AsProblemDetails(this ManagedCode.Communication.Problem problem)
    {
        return problem.ToProblemDetails();
    }
    
    /// <summary>
    /// Converts ProblemDetails to Problem.
    /// </summary>
    public static ManagedCode.Communication.Problem AsProblem(this ProblemDetails problemDetails)
    {
        return FromProblemDetails(problemDetails);
    }
    
    /// <summary>
    /// Creates a failed Result from ProblemDetails.
    /// </summary>
    public static ManagedCode.Communication.Result ToFailedResult(this ProblemDetails problemDetails)
    {
        return ManagedCode.Communication.Result.Fail(problemDetails.AsProblem());
    }
    
    /// <summary>
    /// Creates a failed Result<T> from ProblemDetails.
    /// </summary>
    public static ManagedCode.Communication.Result<T> ToFailedResult<T>(this ProblemDetails problemDetails)
    {
        return ManagedCode.Communication.Result<T>.Fail(problemDetails.AsProblem());
    }
    
    /// <summary>
    /// Creates a failed Result from Problem.
    /// </summary>
    public static ManagedCode.Communication.Result ToFailedResult(this ManagedCode.Communication.Problem problem)
    {
        return ManagedCode.Communication.Result.Fail(problem);
    }
    
    /// <summary>
    /// Creates a failed Result<T> from Problem.
    /// </summary>
    public static ManagedCode.Communication.Result<T> ToFailedResult<T>(this ManagedCode.Communication.Problem problem)
    {
        return ManagedCode.Communication.Result<T>.Fail(problem);
    }
}
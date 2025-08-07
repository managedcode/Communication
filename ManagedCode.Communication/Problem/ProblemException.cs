using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

/// <summary>
///     Exception that represents a Problem Details failure.
/// </summary>
public class ProblemException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProblemException" /> class with a Problem.
    /// </summary>
    public ProblemException(Problem problem) : base(GetMessage(problem))
    {
        Problem = problem;
        PopulateData(problem);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProblemException" /> class with title.
    /// </summary>
    public ProblemException(string title) 
        : this(Problem.Create(title, title))
    {
    }
    
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProblemException" /> class with title and detail.
    /// </summary>
    public ProblemException(string title, string detail) 
        : this(Problem.Create(title, detail))
    {
    }
    
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProblemException" /> class with title, detail and status code.
    /// </summary>
    public ProblemException(string title, string detail, int statusCode) 
        : this(Problem.Create(title, detail, statusCode))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProblemException" /> class with an inner exception.
    /// </summary>
    public ProblemException(Exception innerException) 
        : this(Problem.Create(innerException))
    {
    }

    private void PopulateData(Problem problem)
    {
        // Add problem data to exception Data collection
        Data[$"{nameof(Problem)}.{nameof(problem.Type)}"] = problem.Type;
        Data[$"{nameof(Problem)}.{nameof(problem.Title)}"] = problem.Title;
        Data[$"{nameof(Problem)}.{nameof(problem.StatusCode)}"] = problem.StatusCode;
        Data[$"{nameof(Problem)}.{nameof(problem.Detail)}"] = problem.Detail;
        Data[$"{nameof(Problem)}.{nameof(problem.Instance)}"] = problem.Instance;

        // Add extensions to Data
        foreach (var extension in problem.Extensions)
        {
            Data[$"{nameof(Problem)}.{nameof(problem.Extensions)}.{extension.Key}"] = extension.Value;
        }

        // Handle error code if present
        if (problem.ErrorCode != null)
        {
            Data[$"{nameof(Problem)}.{nameof(problem.ErrorCode)}"] = problem.ErrorCode;
        }

        // Handle validation errors if present
        var validationErrors = problem.GetValidationErrors();
        if (validationErrors != null)
        {
            foreach (var error in validationErrors)
            {
                Data[$"{nameof(Problem)}.ValidationError.{error.Key}"] = string.Join("; ", error.Value);
            }
        }
    }

    /// <summary>
    ///     Gets the Problem that caused this exception.
    /// </summary>
    public Problem Problem { get; }

    /// <summary>
    ///     Gets the HTTP status code from the problem.
    /// </summary>
    public int StatusCode => Problem.StatusCode;

    /// <summary>
    ///     Gets the problem type.
    /// </summary>
    public string? Type => string.IsNullOrEmpty(Problem.Type) || Problem.Type == "https://httpstatuses.io/0" ? null : Problem.Type;

    /// <summary>
    ///     Gets the problem title.
    /// </summary>
    public string? Title => string.IsNullOrEmpty(Problem.Title) ? null : Problem.Title;

    /// <summary>
    ///     Gets the problem detail.
    /// </summary>
    public string? Detail => string.IsNullOrEmpty(Problem.Detail) ? null : Problem.Detail;

    /// <summary>
    ///     Gets the error code if available.
    /// </summary>
    public string ErrorCode => Problem.ErrorCode;

    /// <summary>
    ///     Gets the validation errors if this is a validation problem.
    /// </summary>
    public Dictionary<string, List<string>>? ValidationErrors => Problem.GetValidationErrors();

    /// <summary>
    ///     Checks if this is a validation problem.
    /// </summary>
    public bool IsValidationProblem => Problem.Type == "https://tools.ietf.org/html/rfc7231#section-6.5.1";

    private static string GetMessage(Problem problem)
    {
        // Create a detailed message based on problem type
        if (problem.Type == "https://tools.ietf.org/html/rfc7231#section-6.5.1")
        {
            // Validation error
            var validationErrors = problem.GetValidationErrors();
            if (validationErrors != null && validationErrors.Count > 0)
            {
                var errors = new List<string>();
                foreach (var error in validationErrors)
                {
                    errors.Add($"{error.Key}: {string.Join(", ", error.Value)}");
                }

                return $"Validation failed: {string.Join("; ", errors)}";
            }
        }

        // Generic message construction
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(problem.Title))
        {
            parts.Add(problem.Title);
        }

        if (!string.IsNullOrEmpty(problem.Detail))
        {
            parts.Add(problem.Detail);
        }

        if (problem.StatusCode > 0)
        {
            parts.Add($"(HTTP {problem.StatusCode})");
        }

        if (!string.IsNullOrEmpty(problem.ErrorCode))
        {
            parts.Add($"[{problem.ErrorCode}]");
        }

        return parts.Count > 0 ? string.Join(" - ", parts) : "An error occurred";
    }

    /// <summary>
    ///     Creates a ProblemException from a Problem.
    /// </summary>
    public static ProblemException FromProblem(Problem problem)
    {
        return new ProblemException(problem);
    }
}
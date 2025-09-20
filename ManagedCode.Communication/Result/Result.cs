using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

/// <summary>
///     Represents a result of an operation.
/// </summary>
[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; Problem: {Problem?.Title}")]
public partial struct Result : IResult, IResultFactory<Result>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Result" /> struct.
    /// </summary>
    private Result(bool isSuccess, Problem? problem = null)
    {
        IsSuccess = isSuccess;
        Problem = problem;
    }

    /// <summary>
    ///     Creates a successful Result.
    /// </summary>
    internal static Result CreateSuccess()
    {
        return new Result(true, null);
    }

    /// <summary>
    ///     Creates a failed Result with the specified problem.
    /// </summary>
    internal static Result CreateFailed(Problem problem)
    {
        return new Result(false, problem);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("isSuccess")]
    [JsonPropertyOrder(1)]
    [MemberNotNullWhen(false, nameof(Problem))]
    public bool IsSuccess { get; private init; }

    /// <summary>
    ///     Gets a value indicating whether the operation failed.
    /// </summary>
    [JsonIgnore]
    public bool IsFailed => !IsSuccess;

    [JsonInclude]
    [JsonPropertyName("problem")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private Problem? _problem;

    /// <summary>
    ///     Gets or sets the problem that occurred during the operation.
    /// </summary>
    [JsonIgnore]
    public Problem? Problem
    {
        get
        {
            if (_problem is null && !IsSuccess)
                _problem = Problem.GenericError();

            return _problem;
        }
        private init => _problem = value;
    }


    /// <summary>
    ///     Gets a value indicating whether the result has a problem.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Problem))]
    public bool HasProblem => !IsSuccess;

    /// <summary>
    ///     Gets a value indicating whether the result is valid (successful and has no problems).
    /// </summary>
    [JsonIgnore]
    public bool IsValid => IsSuccess && !HasProblem;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, List<string>>? InvalidObject => Problem?.GetValidationErrors();


    /// <summary>
    ///     Get the Problem assigned to the result without falling back to a generic error if no problem is assigned.
    ///     Useful if a different default problem is desired.
    /// </summary>
    internal Problem? GetProblemNoFallback() => _problem;

    /// <summary>
    ///     Throws an exception if the result indicates a failure.
    /// </summary>
    public bool ThrowIfFail()
    {
        var problem = Problem;
        if (problem is not null)
        {
            throw problem;
        }

        return false;
    }

    /// <summary>
    ///     Tries to get the problem from the result.
    /// </summary>
    /// <param name="problem">When this method returns, contains the problem if the result has a problem; otherwise, null.</param>
    /// <returns>true if the result has a problem; otherwise, false.</returns>
    [MemberNotNullWhen(true, nameof(Problem))]
    public bool TryGetProblem([MaybeNullWhen(false)] out Problem problem)
    {
        problem = Problem;
        return problem is not null;
    }


    #region IResultInvalid Implementation

    [JsonIgnore]
    public bool IsInvalid => Problem?.Type == "https://tools.ietf.org/html/rfc7231#section-6.5.1";

    [JsonIgnore]
    public bool IsNotInvalid => !IsInvalid;

    public bool InvalidField(string fieldName)
    {
        return !IsSuccess && Problem.InvalidField(fieldName);
    }

    public string InvalidFieldError(string fieldName)
    {
        return IsSuccess
            ? string.Empty
            : Problem.InvalidFieldError(fieldName);
    }

    [Obsolete("Use Problem.AddValidationError instead")]
    public void AddInvalidMessage(string message)
    {
        if (!IsSuccess)
            Problem.AddValidationError(message);
    }

    [Obsolete("Use Problem.AddValidationError instead")]
    public void AddInvalidMessage(string key, string value)
    {
        if (!IsSuccess)
            Problem.AddValidationError(key, value);
    }


    #endregion
}

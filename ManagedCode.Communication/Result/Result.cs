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
    [MemberNotNullWhen(true, nameof(Problem))]
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
        // The fallback is deliberately not written back into _problem. Assigning a field from a getter is
        // silently discarded whenever the struct is read through a defensive copy (a readonly field, an `in`
        // parameter, a boxed value), so it only ever looked like a cache. Every Result the library produces
        // for a failure carries its Problem already; the fallback exists solely for default(Result) and for
        // hand-written payloads that claim failure without one.
        get => _problem ?? (IsSuccess ? null : Problem.GenericError());
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

    /// <summary>
    ///     Validation errors by field, or <c>null</c> when there are none.
    /// </summary>
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

    /// <summary>
    ///     Whether the failure is a validation failure.
    /// </summary>
    [JsonIgnore]
    public bool IsInvalid => Problem?.Type == ProblemConstants.Types.ValidationFailed;

    /// <summary>
    ///     Whether the result is anything other than a validation failure.
    /// </summary>
    [JsonIgnore]
    public bool IsNotInvalid => !IsInvalid;

    /// <summary>
    ///     Whether the named field has a validation error.
    /// </summary>
    public bool InvalidField(string fieldName)
    {
        return !IsSuccess && Problem.InvalidField(fieldName);
    }

    /// <summary>
    ///     The validation messages for a field, joined by commas; empty when the field has none.
    /// </summary>
    public string InvalidFieldError(string fieldName)
    {
        return IsSuccess
            ? string.Empty
            : Problem.InvalidFieldError(fieldName);
    }


    #endregion
}

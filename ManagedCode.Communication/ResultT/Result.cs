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
///     Represents a result from an operation that can either succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; Problem: {Problem?.Title}")]
public partial struct Result<T> : IResult<T>, IResultFactory<Result<T>>, IResultValueFactory<Result<T>, T>
{
    /// <summary>
    ///     Initializes a new instance of the Result struct.
    /// </summary>
    private Result(bool isSuccess, T? value, Problem? problem = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Problem = problem;
    }

    /// <summary>
    ///     Initializes a new instance of the Result struct with an exception.
    /// </summary>
    private Result(Exception exception) : this(false, default, Problem.Create(exception))
    {

    }

    /// <summary>
    ///     Creates a successful Result with the specified value.
    /// </summary>
    internal static Result<T> CreateSuccess(T value)
    {
        return new Result<T>(true, value, null);
    }

    /// <summary>
    ///     Creates a failed Result with the specified problem and optional value.
    /// </summary>
    internal static Result<T> CreateFailed(Problem problem, T? value = default)
    {
        return new Result<T>(false, value, problem);
    }


    /// <summary>
    ///     Throws an exception if the result is a failure.
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


    /// <summary>
    ///     Gets a value indicating whether the result is a success.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("isSuccess")]
    [JsonPropertyOrder(1)]
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Problem))]
    public bool IsSuccess { get; private init; }

    /// <summary>
    ///     Gets a value indicating whether the result is empty.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsEmpty => Value is null;

    /// <summary>
    ///     Gets a value indicating whether the result is a failure.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailed => !IsSuccess;

    /// <summary>
    ///     Gets or sets the value of the result.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Value { get; set; }

    [JsonInclude]
    [JsonPropertyName("problem")]
    [JsonPropertyOrder(3)]
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
    ///     Get the Problem assigned to the result without falling back to a generic error if no problem is assigned.
    ///     Useful if a different default problem is desired.
    /// </summary>
    internal Problem? GetProblemNoFallback() => _problem;

    /// <summary>
    ///     Gets a value indicating whether the result is valid (successful and has no problems).
    /// </summary>
    [JsonIgnore]
    public bool IsValid => IsSuccess && !HasProblem;

    /// <summary>
    ///     Gets a value indicating whether the result is invalid.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsInvalid => Problem?.Type == "https://tools.ietf.org/html/rfc7231#section-6.5.1";

    [JsonIgnore]
    public bool IsNotInvalid => !IsInvalid;


    /// <summary>
    ///     Adds an invalid message to the result.
    /// </summary>
    [Obsolete("Use Problem.AddValidationError instead")]
    public void AddInvalidMessage(string message)
    {
        if (!IsSuccess)
            Problem.AddValidationError(message);
    }

    /// <summary>
    ///     Adds an invalid message with a specific key to the result.
    /// </summary>
    [Obsolete("Use Problem.AddValidationError instead")]
    public void AddInvalidMessage(string key, string value)
    {
        if (!IsSuccess)
            Problem.AddValidationError(key, value);
    }

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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, List<string>>? InvalidObject => Problem?.GetValidationErrors();
}

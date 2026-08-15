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
[JsonConverter(typeof(ResultTJsonConverterFactory))]
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
    [MemberNotNullWhen(true, nameof(Problem))]
    public bool IsFailed => !IsSuccess;

    /// <summary>
    ///     The value carried by the result.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <c>init</c> rather than <c>set</c>: a settable value let callers put a payload on a failed result,
    ///         which contradicts the nullable annotations declaring that a failure carries none. Serializers can
    ///         still populate it.
    ///     </para>
    ///     <para>
    ///         Always written to JSON. It used to be omitted when it equalled <c>default</c>, which meant
    ///         <c>Result&lt;int&gt;.Succeed(0)</c> and <c>Result&lt;bool&gt;.Succeed(false)</c> serialized with no
    ///         <c>value</c> member at all — indistinguishable, to a non-.NET client, from a result that carried
    ///         nothing.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(2)]
    public T? Value { get; init; }

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
        // The fallback is deliberately not written back into _problem. Assigning a field from a getter is
        // silently discarded whenever the struct is read through a defensive copy (a readonly field, an `in`
        // parameter, a boxed value), so it only ever looked like a cache. Every Result the library produces
        // for a failure carries its Problem already; the fallback exists solely for default(Result<T>) and for
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

    /// <summary>
    ///     Validation errors by field, or <c>null</c> when there are none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, List<string>>? InvalidObject => Problem?.GetValidationErrors();
}

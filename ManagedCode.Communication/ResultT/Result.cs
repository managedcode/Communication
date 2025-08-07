using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication;

/// <summary>
///     Represents a result from an operation that can either succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; Problem: {Problem?.Title}")]
public partial struct Result<T> : IResult<T>
{
    /// <summary>
    ///     Initializes a new instance of the Result struct.
    /// </summary>
    internal Result(bool isSuccess, T? value, Problem? problem = null)
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
    ///     Creates a Result with the specified success status, value and optional problem.
    /// </summary>
    internal static Result<T> Create(bool isSuccess, T? value, Problem? problem = null)
    {
        return new Result<T>(isSuccess, value, problem);
    }


    /// <summary>
    ///     Throws an exception if the result is a failure.
    /// </summary>
    public bool ThrowIfFail()
    {
        if (HasProblem)
        {
            throw Problem;
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
        return HasProblem;
    }


    /// <summary>
    ///     Gets a value indicating whether the result is a success.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the result is empty.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsEmpty => Value is null;

    /// <summary>
    ///     Gets a value indicating whether the result is a failure.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailed => !IsSuccess || HasProblem;

    /// <summary>
    ///     Gets or sets the value of the result.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Value { get; set; }


    /// <summary>
    ///     Gets or sets the problem that occurred during the operation.
    /// </summary>
    [JsonPropertyName("problem")]
    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Problem? Problem { get; set; }

    /// <summary>
    ///     Gets a value indicating whether the result has a problem.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Problem))]
    public bool HasProblem => Problem is not null;

    /// <summary>
    ///     Gets a value indicating whether the result is invalid.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsInvalid => Problem?.Type == "https://tools.ietf.org/html/rfc7231#section-6.5.1";

    public bool IsNotInvalid => !IsInvalid;


    /// <summary>
    ///     Adds an invalid message to the result.
    /// </summary>
    public void AddInvalidMessage(string message)
    {
        if (Problem == null)
        {
            Problem = Problem.Validation((ProblemConstants.ValidationFields.General, message));
        }
        else
        {
            Problem.Extensions[ProblemConstants.ExtensionKeys.Errors] ??= new Dictionary<string, List<string>>();
            if (Problem.Extensions[ProblemConstants.ExtensionKeys.Errors] is Dictionary<string, List<string>> errors)
            {
                if (!errors.ContainsKey(ProblemConstants.ValidationFields.General))
                {
                    errors[ProblemConstants.ValidationFields.General] = new List<string>();
                }

                errors[ProblemConstants.ValidationFields.General]
                    .Add(message);
            }
        }
    }

    /// <summary>
    ///     Adds an invalid message with a specific key to the result.
    /// </summary>
    public void AddInvalidMessage(string key, string value)
    {
        if (Problem == null)
        {
            Problem = Problem.Validation((key, value));
        }
        else
        {
            Problem.Extensions[ProblemConstants.ExtensionKeys.Errors] ??= new Dictionary<string, List<string>>();
            if (Problem.Extensions[ProblemConstants.ExtensionKeys.Errors] is Dictionary<string, List<string>> errors)
            {
                if (!errors.ContainsKey(key))
                {
                    errors[key] = new List<string>();
                }

                errors[key]
                    .Add(value);
            }
        }
    }

    public bool InvalidField(string fieldName)
    {
        var errors = Problem?.GetValidationErrors();
        return errors?.ContainsKey(fieldName) ?? false;
    }

    public string InvalidFieldError(string fieldName)
    {
        var errors = Problem?.GetValidationErrors();
        return errors?.TryGetValue(fieldName, out var fieldErrors) == true ? string.Join(", ", fieldErrors) : string.Empty;
    }

    public Dictionary<string, List<string>>? InvalidObject => Problem?.GetValidationErrors();
}
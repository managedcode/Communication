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
    internal Result(Exception exception) : this(false, default, Problem.FromException(exception))
    {
    }


    /// <summary>
    ///     Throws an exception if the result is a failure.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public bool ThrowIfFail()
    {
        if (IsSuccess && !HasProblem)
        {
            return false;
        }

        if (Problem != null)
        {
            throw new ProblemException(Problem);
        }

        if (IsFailed)
        {
            throw new Exception("Operation failed");
        }

        return false;
    }


    /// <summary>
    ///     Gets a value indicating whether the result is a success.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; set; }

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
    public bool HasProblem => Problem != null;

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
            Problem = Problem.Validation(("_general", message));
        }
        else
        {
            Problem.Extensions[ProblemExtensionKeys.Errors] ??= new Dictionary<string, List<string>>();
            if (Problem.Extensions[ProblemExtensionKeys.Errors] is Dictionary<string, List<string>> errors)
            {
                if (!errors.ContainsKey("_general"))
                {
                    errors["_general"] = new List<string>();
                }

                errors["_general"]
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
            Problem.Extensions[ProblemExtensionKeys.Errors] ??= new Dictionary<string, List<string>>();
            if (Problem.Extensions[ProblemExtensionKeys.Errors] is Dictionary<string, List<string>> errors)
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

    public string? InvalidFieldError(string fieldName)
    {
        var errors = Problem?.GetValidationErrors();
        return errors?.TryGetValue(fieldName, out var fieldErrors) == true ? string.Join(", ", fieldErrors) : null;
    }

    public Dictionary<string, List<string>>? InvalidObject => Problem?.GetValidationErrors();
}
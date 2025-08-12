using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication;

/// <summary>
///     Represents a result of an operation.
/// </summary>
[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; Problem: {Problem?.Title}")]
public partial struct Result : IResult
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Result" /> struct.
    /// </summary>
    internal Result(bool isSuccess, Problem? problem = null)
    {
        IsSuccess = isSuccess;
        Problem = problem;
    }

    /// <summary>
    ///     Creates a Result with the specified success status and optional problem.
    /// </summary>
    internal static Result Create(bool isSuccess, Problem? problem = null)
    {
        return new Result(isSuccess, problem);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    [JsonPropertyName("isSuccess")]
    [JsonPropertyOrder(1)]
    [MemberNotNullWhen(false, nameof(Problem))]
    public bool IsSuccess { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the operation failed.
    /// </summary>
    [JsonIgnore]
    public bool IsFailed => !IsSuccess || HasProblem;

    /// <summary>
    ///     Gets or sets the problem that occurred during the operation.
    /// </summary>
    [JsonPropertyName("problem")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Problem? Problem { get; set; }


    /// <summary>
    ///     Gets a value indicating whether the result has a problem.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Problem))]
    public bool HasProblem => Problem != null;


    /// <summary>
    ///     Throws an exception if the result indicates a failure.
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


    #region IResultInvalid Implementation

    public bool IsInvalid => Problem?.Type == "https://tools.ietf.org/html/rfc7231#section-6.5.1";

    public bool IsNotInvalid => !IsInvalid;

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

    #endregion
}

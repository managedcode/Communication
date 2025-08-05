using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication;

/// <summary>
/// Represents a result of an operation.
/// </summary>
[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; Problem: {Problem?.Title}")]
public partial struct Result : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> struct.
    /// </summary>
    internal Result(bool isSuccess, Problem? problem = null)
    {
        IsSuccess = isSuccess;
        Problem = problem;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    [JsonPropertyName("isSuccess")]
    [JsonPropertyOrder(1)]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [JsonIgnore]
    public bool IsFailed => !IsSuccess || HasProblem;

    /// <summary>
    /// Gets or sets the problem that occurred during the operation.
    /// </summary>
    [JsonPropertyName("problem")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Problem? Problem { get; set; }

    
    /// <summary>
    /// Gets a value indicating whether the result has a problem.
    /// </summary>
    [JsonIgnore]
    public bool HasProblem => Problem != null;


    /// <summary>
    /// Throws an exception if the result indicates a failure.
    /// </summary>
    public bool ThrowIfFail()
    {
        if (IsSuccess && !HasProblem)
            return false;
        
        if (Problem != null)
        {
            throw new ProblemException(Problem);
        }
        
        if (IsFailed)
            throw new Exception("Operation failed");
            
        return false;
    }


    #region IResultInvalid Implementation

    public bool IsInvalid => Problem?.Type == "https://tools.ietf.org/html/rfc7231#section-6.5.1";

    public bool IsNotInvalid => !IsInvalid;

    public bool InvalidField(string fieldName)
    {
        var errors = Problem?.GetValidationErrors();
        return errors?.ContainsKey(fieldName) ?? false;
    }

    public string? InvalidFieldError(string fieldName)
    {
        var errors = Problem?.GetValidationErrors();
        return errors?.TryGetValue(fieldName, out var fieldErrors) == true 
            ? string.Join(", ", fieldErrors) 
            : null;
    }
    
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
                    errors["_general"] = new List<string>();
                errors["_general"].Add(message);
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
            Problem.Extensions[ProblemExtensionKeys.Errors] ??= new Dictionary<string, List<string>>();
            if (Problem.Extensions[ProblemExtensionKeys.Errors] is Dictionary<string, List<string>> errors)
            {
                if (!errors.ContainsKey(key))
                    errors[key] = new List<string>();
                errors[key].Add(value);
            }
        }
    }

    #endregion
}
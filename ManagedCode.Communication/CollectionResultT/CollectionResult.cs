using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.CollectionResultT;

[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; Count: {Collection?.Length ?? 0}; Problem: {Problem?.Title}")]
public partial struct CollectionResult<T> : IResult, IResultProblem
{
    internal CollectionResult(bool isSuccess, IEnumerable<T>? collection, int pageNumber, int pageSize, int totalItems, Problem? problem) : this(
        isSuccess, collection?.ToArray(), pageNumber, pageSize, totalItems, problem)
    {
    }

    internal CollectionResult(bool isSuccess, T[]? collection, int pageNumber, int pageSize, int totalItems, Problem? problem = null)
    {
        IsSuccess = isSuccess;
        Collection = collection ?? [];
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        Problem = problem;
    }

    [JsonPropertyName("isSuccess")]
    [JsonPropertyOrder(1)]
    [MemberNotNullWhen(true, nameof(Collection))]
    public bool IsSuccess { get; set; }

    [JsonIgnore]
    public bool IsFailed => !IsSuccess || HasProblem;

    [JsonPropertyName("collection")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T[] Collection { get; set; } = [];

    [JsonPropertyName("pageNumber")]
    [JsonPropertyOrder(3)]
    public int PageNumber { get; set; }

    [JsonPropertyName("pageSize")]
    [JsonPropertyOrder(4)]
    public int PageSize { get; set; }

    [JsonPropertyName("totalItems")]
    [JsonPropertyOrder(5)]
    public int TotalItems { get; set; }

    [JsonPropertyName("totalPages")]
    [JsonPropertyOrder(6)]
    public int TotalPages { get; set; }

    [JsonPropertyName("problem")]
    [JsonPropertyOrder(7)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Problem? Problem { get; set; }


    [JsonIgnore]
    public bool IsEmpty => Collection is null || Collection.Length == 0;

    [JsonIgnore]
    public bool HasItems => Collection?.Length > 0;

    [JsonIgnore]
    public bool HasProblem => Problem != null;

    #region IResultProblem Implementation

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

    #endregion


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
        return errors?.TryGetValue(fieldName, out var fieldErrors) == true ? string.Join(", ", fieldErrors) : null;
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
                {
                    errors["_general"] = new List<string>();
                }

                errors["_general"]
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

    #endregion

    #region Static Factory Methods

    /// <summary>
    ///     Creates an empty collection result.
    /// </summary>
    public static CollectionResult<T> Empty()
    {
        return new CollectionResult<T>(true, Array.Empty<T>(), 1, 0, 0);
    }

    /// <summary>
    ///     Creates an empty collection result with paging information.
    /// </summary>
    public static CollectionResult<T> Empty(int pageNumber, int pageSize)
    {
        return new CollectionResult<T>(true, Array.Empty<T>(), pageNumber, pageSize, 0);
    }

    #endregion
}
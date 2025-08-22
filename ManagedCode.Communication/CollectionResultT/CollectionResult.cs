using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.CollectionResultT;

[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; Count: {Collection?.Length ?? 0}; Problem: {Problem?.Title}")]
public partial struct CollectionResult<T> : IResult
{
    private CollectionResult(bool isSuccess, IEnumerable<T>? collection, int pageNumber, int pageSize, int totalItems, Problem? problem) : this(
        isSuccess, collection?.ToArray(), pageNumber, pageSize, totalItems, problem)
    {
    }

    private CollectionResult(bool isSuccess, T[]? collection, int pageNumber, int pageSize, int totalItems, Problem? problem = null)
    {
        IsSuccess = isSuccess;
        Collection = collection ?? [];
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 0;
        Problem = problem;
    }

    internal static CollectionResult<T> CreateSuccess(T[]? collection, int pageNumber, int pageSize, int totalItems)
    {
        return new CollectionResult<T>(true, collection, pageNumber, pageSize, totalItems, null);
    }

    internal static CollectionResult<T> CreateFailed(Problem problem, T[]? collection = null)
    {
        return new CollectionResult<T>(false, collection, 0, 0, 0, problem);
    }

    [JsonInclude]
    [JsonPropertyName("isSuccess")]
    [JsonPropertyOrder(1)]
    [MemberNotNullWhen(true, nameof(Collection))]
    [MemberNotNullWhen(false, nameof(Problem))]
    public bool IsSuccess { get; set; }

    [JsonIgnore]
    public bool IsFailed => !IsSuccess;

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

    [JsonInclude]
    [JsonPropertyName("problem")]
    [JsonPropertyOrder(7)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private Problem? _problem;

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

    [JsonIgnore]
    public bool IsEmpty => Collection is null || Collection.Length == 0;

    [JsonIgnore]
    public bool HasItems => Collection?.Length > 0;

    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Problem))]
    public bool HasProblem => !IsSuccess;

    #region IResultProblem Implementation

    /// <summary>
    ///     Get the Problem assigned to the result without falling back to a generic error if no problem is assigned.
    ///     Useful if a different default problem is desired.
    /// </summary>
    internal Problem? GetProblemNoFallback() => _problem;

    public bool ThrowIfFail()
    {
        var problem = Problem;
        if (problem is not null)
        {
            throw problem;
        }

        return false;
    }

    [MemberNotNullWhen(true, nameof(Problem))]
    public bool TryGetProblem([MaybeNullWhen(false)] out Problem problem)
    {
        problem = Problem;
        return problem is not null;
    }

    #endregion

    #region IResultInvalid Implementation

    public bool IsInvalid => Problem?.Type == "https://tools.ietf.org/html/rfc7231#section-6.5.1";

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

    #region Static Factory Methods

    /// <summary>
    ///     Creates an empty collection result.
    /// </summary>
    public static CollectionResult<T> Empty()
    {
        return CreateSuccess([], 0, 0, 0);
    }

    #endregion
}

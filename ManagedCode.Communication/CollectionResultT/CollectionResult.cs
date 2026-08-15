using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication.CollectionResultT;

/// <summary>
///     A result carrying a page of items, with the paging metadata that describes it.
/// </summary>
[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; Count: {Collection?.Length ?? 0}; Problem: {Problem?.Title}")]
public partial struct CollectionResult<T> : IResultCollection<T>, ICollectionResultFactory<CollectionResult<T>, T>
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

    /// <summary>
    ///     Whether the operation succeeded.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("isSuccess")]
    [JsonPropertyOrder(1)]
    [MemberNotNullWhen(true, nameof(Collection))]
    [MemberNotNullWhen(false, nameof(Problem))]
    public bool IsSuccess { get; init; }

    /// <summary>
    ///     Whether the operation failed.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Problem))]
    public bool IsFailed => !IsSuccess;

    /// <summary>
    ///     The items on this page. Empty when the result failed.
    /// </summary>
    [JsonPropertyName("collection")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T[] Collection { get; init; } = [];

    /// <summary>
    ///     Gets the collection as a Value property, for <see cref="IResult{T}" /> compatibility.
    /// </summary>
    [JsonIgnore]
    public T[]? Value => Collection;

    /// <summary>
    ///     1-based index of this page.
    /// </summary>
    [JsonPropertyName("pageNumber")]
    [JsonPropertyOrder(3)]
    public int PageNumber { get; init; }

    /// <summary>
    ///     Maximum number of items per page.
    /// </summary>
    [JsonPropertyName("pageSize")]
    [JsonPropertyOrder(4)]
    public int PageSize { get; init; }

    /// <summary>
    ///     Total number of items across all pages.
    /// </summary>
    [JsonPropertyName("totalItems")]
    [JsonPropertyOrder(5)]
    public int TotalItems { get; init; }

    /// <summary>
    ///     Total number of pages.
    /// </summary>
    [JsonPropertyName("totalPages")]
    [JsonPropertyOrder(6)]
    public int TotalPages { get; init; }

    [JsonInclude]
    [JsonPropertyName("problem")]
    [JsonPropertyOrder(7)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private Problem? _problem;

    /// <summary>
    ///     The failure, or <c>null</c> when the result succeeded.
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
    ///     Whether this page carries no items.
    /// </summary>
    [JsonIgnore]
    public bool IsEmpty => Collection is null || Collection.Length == 0;

    /// <summary>
    ///     Whether this page carries at least one item.
    /// </summary>
    [JsonIgnore]
    public bool HasItems => Collection?.Length > 0;

    /// <summary>
    ///     Gets a value indicating whether the result has a non-empty value, for <see cref="IResult{T}" /> compatibility.
    /// </summary>
    [JsonIgnore]
    public bool HasValue => !IsEmpty;

    /// <summary>
    ///     Whether the result carries a failure.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Problem))]
    public bool HasProblem => !IsSuccess;

    /// <summary>
    ///     Gets a value indicating whether the result is valid (successful and has no problems).
    /// </summary>
    [JsonIgnore]
    public bool IsValid => IsSuccess && !HasProblem;

    #region IResultProblem Implementation

    /// <summary>
    ///     Get the Problem assigned to the result without falling back to a generic error if no problem is assigned.
    ///     Useful if a different default problem is desired.
    /// </summary>
    internal Problem? GetProblemNoFallback() => _problem;

    /// <summary>
    ///     Throws the carried problem as an exception when the result failed.
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
    ///     Gets the failure when there is one.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Problem))]
    public bool TryGetProblem([MaybeNullWhen(false)] out Problem problem)
    {
        problem = Problem;
        return problem is not null;
    }

    #endregion

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
    ///     Validation errors by field, or <c>null</c> when there are none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, List<string>>? InvalidObject => Problem?.GetValidationErrors();

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

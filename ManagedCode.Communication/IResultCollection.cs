using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a result that contains a collection of items with pagination support and comprehensive validation properties.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public interface IResultCollection<out T> : IResult<T[]>
{
    /// <summary>
    ///     Gets the collection of items.
    /// </summary>
    /// <value>The collection of items, or empty array if the result does not contain items.</value>
    [JsonPropertyName("collection")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    T[] Collection { get; }

    /// <summary>
    ///     Gets a value indicating whether the collection has any items.
    /// </summary>
    /// <value>true if the collection has items; otherwise, false.</value>
    [JsonIgnore]
    bool HasItems { get; }

    /// <summary>
    ///     Gets the current page number (1-based).
    /// </summary>
    /// <value>The current page number.</value>
    [JsonPropertyName("pageNumber")]
    [JsonPropertyOrder(3)]
    int PageNumber { get; }

    /// <summary>
    ///     Gets the number of items per page.
    /// </summary>
    /// <value>The page size.</value>
    [JsonPropertyName("pageSize")]
    [JsonPropertyOrder(4)]
    int PageSize { get; }

    /// <summary>
    ///     Gets the total number of items across all pages.
    /// </summary>
    /// <value>The total item count.</value>
    [JsonPropertyName("totalItems")]
    [JsonPropertyOrder(5)]
    int TotalItems { get; }

    /// <summary>
    ///     Gets the total number of pages.
    /// </summary>
    /// <value>The total page count.</value>
    [JsonPropertyName("totalPages")]
    [JsonPropertyOrder(6)]
    int TotalPages { get; }

    /// <summary>
    ///     Gets a value indicating whether there is a previous page.
    /// </summary>
    /// <value>true if there is a previous page; otherwise, false.</value>
    [JsonIgnore]
    bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    ///     Gets a value indicating whether there is a next page.
    /// </summary>
    /// <value>true if there is a next page; otherwise, false.</value>
    [JsonIgnore]
    bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    ///     Gets the number of items in the current page.
    /// </summary>
    /// <value>The count of items in the current collection.</value>
    [JsonIgnore]
    int Count => Collection.Length;

    /// <summary>
    ///     Gets a value indicating whether this is the first page.
    /// </summary>
    /// <value>true if this is the first page; otherwise, false.</value>
    [JsonIgnore]
    bool IsFirstPage => PageNumber <= 1;

    /// <summary>
    ///     Gets a value indicating whether this is the last page.
    /// </summary>
    /// <value>true if this is the last page; otherwise, false.</value>
    [JsonIgnore]
    bool IsLastPage => PageNumber >= TotalPages;
}
using System;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.Communication.Commands;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    /// <summary>
    /// Creates a successful collection result using pagination metadata from <paramref name="request"/>.
    /// </summary>
    /// <param name="values">Values contained in the page.</param>
    /// <param name="request">Pagination request describing the current page.</param>
    /// <param name="totalItems">Total number of items across all pages.</param>
    /// <param name="options">Optional normalization options.</param>
    public static CollectionResult<T> Succeed(IEnumerable<T> values, PaginationRequest request, int totalItems, PaginationOptions? options = null)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var array = values as T[] ?? values.ToArray();
        var normalized = request.Normalize(options).ClampToTotal(totalItems);
        return CreateSuccess(array, normalized.PageNumber, normalized.PageSize, totalItems);
    }

    /// <summary>
    /// Creates a successful collection result using pagination metadata from <paramref name="request"/>.
    /// </summary>
    /// <param name="values">Values contained in the page.</param>
    /// <param name="request">Pagination request describing the current page.</param>
    /// <param name="totalItems">Total number of items across all pages.</param>
    /// <param name="options">Optional normalization options.</param>
    public static CollectionResult<T> Succeed(T[] values, PaginationRequest request, int totalItems, PaginationOptions? options = null)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var normalized = request.Normalize(options).ClampToTotal(totalItems);
        return CreateSuccess(values, normalized.PageNumber, normalized.PageSize, totalItems);
    }
}

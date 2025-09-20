using System;

namespace ManagedCode.Communication.Commands;

/// <summary>
/// Provides reusable constraints for <see cref="PaginationRequest"/> normalization.
/// </summary>
public sealed record PaginationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaginationOptions"/> record.
    /// </summary>
    /// <param name="defaultPageSize">Default page size used when a request omits a take value.</param>
    /// <param name="maxPageSize">Upper bound applied to the requested page size. Use <c>null</c> to disable the cap.</param>
    /// <param name="minPageSize">Minimum page size allowed after normalization.</param>
    public PaginationOptions(int defaultPageSize = 50, int? maxPageSize = 1000, int minPageSize = 1)
    {
        if (defaultPageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultPageSize), "Default page size must be greater than zero.");
        }

        if (minPageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minPageSize), "Minimum page size must be greater than zero.");
        }

        if (maxPageSize is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPageSize), "Maximum page size must be greater than zero when provided.");
        }

        if (maxPageSize is not null && defaultPageSize > maxPageSize)
        {
            throw new ArgumentException("Default page size cannot exceed the maximum page size.", nameof(defaultPageSize));
        }

        if (maxPageSize is not null && minPageSize > maxPageSize)
        {
            throw new ArgumentException("Minimum page size cannot exceed the maximum page size.", nameof(minPageSize));
        }

        DefaultPageSize = defaultPageSize;
        MaxPageSize = maxPageSize;
        MinimumPageSize = minPageSize;
    }

    /// <summary>
    /// Gets the default page size used when the request omits a <c>Take</c> value or specifies zero.
    /// </summary>
    public int DefaultPageSize { get; }

    /// <summary>
    /// Gets the maximum page size allowed. A <c>null</c> value disables the ceiling.
    /// </summary>
    public int? MaxPageSize { get; }

    /// <summary>
    /// Gets the minimum page size enforced during normalization.
    /// </summary>
    public int MinimumPageSize { get; }

    /// <summary>
    /// Gets the default options profile used when callers do not supply constraints.
    /// </summary>
    public static PaginationOptions Default { get; } = new();
}

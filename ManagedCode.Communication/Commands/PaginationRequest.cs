using System;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

/// <summary>
/// Represents pagination parameters in a command-friendly structure.
/// </summary>
public sealed record PaginationRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaginationRequest"/> record.
    /// </summary>
    /// <param name="skip">Number of items to skip from the start of the collection.</param>
    /// <param name="take">Number of items to take from the collection.</param>
    [JsonConstructor]
    public PaginationRequest(int skip, int take)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be greater than or equal to zero.");
        }

        if (take < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than or equal to zero.");
        }

        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Gets the number of items to skip.
    /// </summary>
    public int Skip { get; init; }

    /// <summary>
    /// Gets the number of items to take.
    /// </summary>
    public int Take { get; init; }

    /// <summary>
    /// Gets the calculated page number (1-based) for the request. Defaults to the first page when <see cref="Take"/> is zero.
    /// </summary>
    public int PageNumber => Take <= 0 ? 1 : (Skip / Take) + 1;

    /// <summary>
    /// Gets the requested page size.
    /// </summary>
    public int PageSize => Take;

    /// <summary>
    /// Gets the offset equivalent of <see cref="Skip"/>.
    /// </summary>
    public int Offset => Skip;

    /// <summary>
    /// Gets the limit equivalent of <see cref="Take"/>.
    /// </summary>
    public int Limit => Take;

    /// <summary>
    /// Gets a value indicating whether the request has an explicit page size.
    /// </summary>
    public bool HasExplicitPageSize => Take > 0;

    /// <summary>
    /// Creates a new <see cref="PaginationRequest"/> applying the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="skip">Requested skip.</param>
    /// <param name="take">Requested take.</param>
    /// <param name="options">Normalization options; defaults to <see cref="PaginationOptions.Default"/>.</param>
    public static PaginationRequest Create(int skip, int take, PaginationOptions? options = null)
    {
        var nonNegativeSkip = Math.Max(0, skip);
        var nonNegativeTake = Math.Max(0, take);
        return new PaginationRequest(nonNegativeSkip, nonNegativeTake).Normalize(options);
    }

    /// <summary>
    /// Creates a new <see cref="PaginationRequest"/> from 1-based page values applying the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Requested page size.</param>
    /// <param name="options">Normalization options; defaults to <see cref="PaginationOptions.Default"/>.</param>
    public static PaginationRequest FromPage(int pageNumber, int pageSize, PaginationOptions? options = null)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to one.");
        }

        if (pageSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to zero.");
        }

        var baseRequest = new PaginationRequest(0, pageSize).Normalize(options);
        var pageSkip = pageNumber == 1 ? 0 : (pageNumber - 1) * baseRequest.Take;
        var request = new PaginationRequest(pageSkip, baseRequest.Take);
        return options is null ? request : request.Normalize(options);
    }

    /// <summary>
    /// Normalizes the request according to the supplied <paramref name="options"/>, applying defaults, bounds, and clamping.
    /// </summary>
    /// <param name="options">Options that control normalization behaviour.</param>
    /// <returns>A new <see cref="PaginationRequest"/> instance guaranteed to respect the supplied constraints.</returns>
    public PaginationRequest Normalize(PaginationOptions? options = null)
    {
        var settings = options ?? PaginationOptions.Default;

        var normalizedTake = Take <= 0 ? settings.DefaultPageSize : Take;
        normalizedTake = Math.Max(settings.MinimumPageSize, normalizedTake);

        if (settings.MaxPageSize is { } max)
        {
            normalizedTake = Math.Min(normalizedTake, max);
        }

        var normalizedSkip = Math.Max(0, Skip);

        return this with { Skip = normalizedSkip, Take = normalizedTake };
    }

    /// <summary>
    /// Ensures the request does not address items beyond <paramref name="totalItems"/>.
    /// </summary>
    /// <param name="totalItems">Total number of items available.</param>
    /// <returns>A new request whose <see cref="Skip"/> never exceeds <paramref name="totalItems"/>.</returns>
    public PaginationRequest ClampToTotal(int totalItems)
    {
        if (totalItems < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalItems), "Total items must be greater than or equal to zero.");
        }

        if (totalItems == 0)
        {
            return this with { Skip = 0, Take = 0 };
        }

        var maxSkip = Math.Max(0, totalItems - 1);
        var clampedSkip = Math.Min(Skip, maxSkip);
        var remaining = Math.Max(0, totalItems - clampedSkip);
        var adjustedTake = Math.Min(Take, remaining);
        return this with { Skip = clampedSkip, Take = adjustedTake };
    }

    /// <summary>
    /// Calculates the inclusive start and exclusive end indices for the request, after optional normalization.
    /// </summary>
    /// <param name="totalItems">Total available items, used for clamping. Provide <c>null</c> to skip clamping.</param>
    /// <param name="options">Optional normalization settings.</param>
    public (int start, int length) ToSlice(int? totalItems = null, PaginationOptions? options = null)
    {
        var normalized = Normalize(options);
        var request = totalItems is null ? normalized : normalized.ClampToTotal(totalItems.Value);
        var length = totalItems is null ? request.Take : Math.Min(request.Take, Math.Max(0, totalItems.Value - request.Skip));
        return (request.Skip, length);
    }
}

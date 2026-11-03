using System;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

/// <summary>
/// Represents pagination parameters in a command-friendly structure.
/// </summary>
public sealed record PaginationRequest
{
    private const string UnrepresentablePageOffset = "The page offset exceeds the supported range.";
    private const string InvalidTotalItems = "Total items must be greater than or equal to zero.";

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
    [JsonPropertyName(CommunicationJsonNames.Skip)]
    public int Skip { get; init; }

    /// <summary>
    /// Gets the number of items to take.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.Take)]
    public int Take { get; init; }

    /// <summary>
    /// Gets the calculated page number (1-based) for the request. Defaults to the first page when <see cref="Take"/> is zero.
    /// </summary>
    /// <exception cref="OverflowException">The calculated page index exceeds the supported range.</exception>
    [JsonIgnore]
    public int PageNumber => Take <= 0 ? 1 : checked(Skip / Take + 1);

    /// <summary>
    /// Gets the requested page size.
    /// </summary>
    [JsonIgnore]
    public int PageSize => Take;

    /// <summary>
    /// Gets the offset equivalent of <see cref="Skip"/>.
    /// </summary>
    [JsonIgnore]
    public int Offset => Skip;

    /// <summary>
    /// Gets the limit equivalent of <see cref="Take"/>.
    /// </summary>
    [JsonIgnore]
    public int Limit => Take;

    /// <summary>
    /// Gets a value indicating whether the request has an explicit page size.
    /// </summary>
    [JsonIgnore]
    public bool HasExplicitPageSize => Take > 0;

    /// <summary>
    /// Creates a new <see cref="PaginationRequest"/> applying the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="skip">Requested skip.</param>
    /// <param name="take">Requested take.</param>
    /// <param name="options">Normalization options; defaults to <see cref="PaginationOptions.Default"/>.</param>
    public static PaginationRequest Create(int skip, int take, PaginationOptions? options = null)
    {
        var (normalizedSkip, normalizedTake) = NormalizeBounds(skip, take, options);
        return new PaginationRequest(normalizedSkip, normalizedTake);
    }

    /// <summary>
    /// Creates a new <see cref="PaginationRequest"/> from 1-based page values applying the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Requested page size.</param>
    /// <param name="options">Normalization options; defaults to <see cref="PaginationOptions.Default"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The page number, page size or resulting offset is outside the supported range.</exception>
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

        var (_, take) = NormalizeBounds(0, pageSize, options);
        var skip = (long)(pageNumber - 1) * take;
        if (skip > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), UnrepresentablePageOffset);
        }

        return new PaginationRequest((int)skip, take);
    }

    /// <summary>
    /// Normalizes the request according to the supplied <paramref name="options"/>, applying defaults, bounds, and clamping.
    /// </summary>
    /// <param name="options">Options that control normalization behaviour.</param>
    /// <returns>A new <see cref="PaginationRequest"/> instance guaranteed to respect the supplied constraints.</returns>
    public PaginationRequest Normalize(PaginationOptions? options = null)
    {
        var (skip, take) = NormalizeBounds(Skip, Take, options);
        return this with { Skip = skip, Take = take };
    }

    private static (int Skip, int Take) NormalizeBounds(int skip, int take, PaginationOptions? options)
    {
        var settings = options ?? PaginationOptions.Default;

        var normalizedTake = take <= 0 ? settings.DefaultPageSize : take;
        normalizedTake = Math.Max(settings.MinimumPageSize, normalizedTake);

        if (settings.MaxPageSize is { } max)
        {
            normalizedTake = Math.Min(normalizedTake, max);
        }

        return (Math.Max(0, skip), normalizedTake);
    }

    /// <summary>
    /// Ensures the request does not address items beyond <paramref name="totalItems"/>.
    /// </summary>
    /// <param name="totalItems">Total number of items available.</param>
    /// <returns>A new request whose <see cref="Skip"/> never exceeds <paramref name="totalItems"/>.</returns>
    public PaginationRequest ClampToTotal(int totalItems)
    {
        var (skip, take) = ClampBounds(Skip, Take, totalItems);
        return this with { Skip = skip, Take = take };
    }

    private static (int Skip, int Take) ClampBounds(int skip, int take, int totalItems)
    {
        if (totalItems < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalItems), InvalidTotalItems);
        }

        if (totalItems == 0)
        {
            return (0, 0);
        }

        var clampedSkip = Math.Min(skip, totalItems - 1);
        var remaining = Math.Max(0, totalItems - clampedSkip);
        return (clampedSkip, Math.Min(take, remaining));
    }

    /// <summary>
    /// Calculates the inclusive start and exclusive end indices for the request, after optional normalization.
    /// </summary>
    /// <param name="totalItems">Total available items, used for clamping. Provide <c>null</c> to skip clamping.</param>
    /// <param name="options">Optional normalization settings.</param>
    public (int start, int length) ToSlice(int? totalItems = null, PaginationOptions? options = null)
    {
        var (skip, take) = NormalizeBounds(Skip, Take, options);
        return totalItems is { } total ? ClampBounds(skip, take, total) : (skip, take);
    }

    internal (int PageNumber, int PageSize) GetPageMetadata(int totalItems, PaginationOptions? options)
    {
        var (skip, pageSize) = NormalizeBounds(Skip, Take, options);
        var (start, _) = ClampBounds(skip, pageSize, totalItems);
        return totalItems == 0 ? (1, 0) : (start / pageSize + 1, pageSize);
    }
}

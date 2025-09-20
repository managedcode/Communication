using System;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

public sealed record PaginationRequest
{
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

    public int Skip { get; init; }

    public int Take { get; init; }

    public int PageNumber => Take <= 0 ? 1 : (Skip / Take) + 1;

    public int PageSize => Take;

    public int Offset => Skip;

    public int Limit => Take;

    public static PaginationRequest FromPage(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to one.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to one.");
        }

        var skip = (pageNumber - 1) * pageSize;
        return new PaginationRequest(skip, pageSize);
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ManagedCode.Communication.CQRS;

namespace ManagedCode.Communication.AspNetCore.Filters;

/// <summary>
///     Minimal API endpoint filter that turns an <c>IAsyncEnumerable&lt;CqrsStreamChunk&lt;,&gt;&gt;</c> returned by a
///     route handler into a Server-Sent Events response.
/// </summary>
public sealed class CqrsResultEndpointFilter : IEndpointFilter
{
    private readonly CqrsStreamServerOptions _options;

    /// <summary>
    ///     Creates the filter with <see cref="CqrsStreamServerOptions.Default" />.
    /// </summary>
    public CqrsResultEndpointFilter()
        : this(CqrsStreamServerOptions.Default)
    {
    }

    /// <summary>
    ///     Creates the filter with explicit options.
    /// </summary>
    public CqrsResultEndpointFilter(CqrsStreamServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var result = await next(context).ConfigureAwait(false);

        if (result is null)
        {
            return null;
        }

        return CqrsStreamResultFactory.TryCreateServerSentEventsResult(result, _options, out var converted)
            ? converted
            : result;
    }
}

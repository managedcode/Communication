using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Communication.CQRS.AspNetCore.Filters;

/// <summary>
///     Endpoint filter that converts streamed CQRS chunks into Server-Sent Events responses.
/// </summary>
public sealed class CqrsResultEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var result = await next(context).ConfigureAwait(false);

        if (result is null)
        {
            return null;
        }

        return CqrsStreamResultFactory.TryCreateServerSentEventsResult(result, out var converted)
            ? converted
            : result;
    }
}

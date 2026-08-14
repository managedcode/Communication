using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ManagedCode.Communication.CQRS.AspNetCore.Filters;

/// <summary>
///     MVC result filter that turns an <c>IAsyncEnumerable&lt;CqrsStreamChunk&lt;,&gt;&gt;</c> returned by an action
///     into a Server-Sent Events response.
/// </summary>
public sealed class CqrsResultActionFilter : IAsyncResultFilter
{
    private readonly CqrsStreamServerOptions _options;

    /// <summary>
    ///     Creates the filter with <see cref="CqrsStreamServerOptions.Default" />.
    /// </summary>
    public CqrsResultActionFilter()
        : this(CqrsStreamServerOptions.Default)
    {
    }

    /// <summary>
    ///     Creates the filter with application-configured options. This is the constructor MVC activates.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public CqrsResultActionFilter(IOptions<CqrsStreamServerOptions> options)
        : this(options?.Value ?? CqrsStreamServerOptions.Default)
    {
    }

    private CqrsResultActionFilter(CqrsStreamServerOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Result is ObjectResult { Value: not null } objectResult &&
            CqrsStreamResultFactory.TryCreateServerSentEventsActionResult(objectResult.Value, _options, out var converted))
        {
            context.Result = converted;
        }

        await next().ConfigureAwait(false);
    }
}

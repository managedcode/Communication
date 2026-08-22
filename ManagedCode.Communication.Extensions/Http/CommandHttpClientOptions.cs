using System;
using System.Net.Http;
using ManagedCode.Communication.Commands.Execution;

namespace ManagedCode.Communication.Extensions.Http;

/// <summary>
///     Configures native command reliability for an <see cref="IHttpClientFactory"/> client.
/// </summary>
public sealed class CommandHttpClientOptions
{
    /// <summary>Creates safe HTTP defaults backed by command retry and circuit breaking.</summary>
    public CommandHttpClientOptions()
    {
        Execution.Retry.Enabled = true;
        Execution.Timeout.Enabled = false;
        Execution.Idempotency.Enabled = false;
        Execution.CircuitBreaker.Enabled = true;
        Execution.CircuitBreaker.PartitionKeySelector = static command =>
            command.CorrelationId ?? command.CommandType;
        Execution.RateLimiter.Enabled = false;
    }

    /// <summary>Native command execution settings used by the HTTP handler.</summary>
    public CommandExecutionOptions Execution { get; } = new();

    /// <summary>
    ///     Selects requests that may be replayed. The default accepts only content-free GET, HEAD, OPTIONS, and TRACE
    ///     requests. Requests with content are always passed through once.
    /// </summary>
    public Func<HttpRequestMessage, bool> ShouldHandle { get; set; } = static request =>
        request.Method == HttpMethod.Get
        || request.Method == HttpMethod.Head
        || request.Method == HttpMethod.Options
        || request.Method == HttpMethod.Trace;
}

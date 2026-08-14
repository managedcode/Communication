using System;
using System.Collections.Generic;
using System.Threading;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     Exercises the MVC side of the CQRS transport. Every action mirrors one stream from
///     <see cref="CqrsTestStreams" /> so MVC and Minimal API are tested against identical handler behaviour.
/// </summary>
[ApiController]
[Route("api/cqrs/mvc")]
public sealed class CqrsTestController : ControllerBase
{
    [HttpGet("completed")]
    public IAsyncEnumerable<Chunk> Completed()
    {
        return CqrsTestStreams.CompletedAsync();
    }

    [HttpGet("completed-without-sequences")]
    public IAsyncEnumerable<Chunk> CompletedWithoutSequences()
    {
        return CqrsTestStreams.CompletedWithoutSequencesAsync();
    }

    [HttpGet("handler-failed")]
    public IAsyncEnumerable<Chunk> HandlerFailed()
    {
        return CqrsTestStreams.FailedByHandlerAsync();
    }

    [HttpGet("stream-throws")]
    public IAsyncEnumerable<Chunk> StreamThrows()
    {
        return CqrsTestStreams.ThrowsAfterProgressAsync();
    }

    [HttpGet("stream-throws-immediately")]
    public IAsyncEnumerable<Chunk> StreamThrowsImmediately()
    {
        return CqrsTestStreams.ThrowsImmediatelyAsync();
    }

    [HttpGet("throws-before-stream")]
    public IAsyncEnumerable<Chunk> ThrowsBeforeStream()
    {
        throw new InvalidOperationException("Action crashed before returning a stream");
    }

    [HttpGet("no-terminal")]
    public IAsyncEnumerable<Chunk> NoTerminal()
    {
        return CqrsTestStreams.WithoutTerminalChunkAsync();
    }

    [HttpGet("null-chunk")]
    public IAsyncEnumerable<Chunk> NullChunk()
    {
        return CqrsTestStreams.WithNullChunkAsync();
    }

    [HttpGet("empty")]
    public IAsyncEnumerable<Chunk> EmptyStream()
    {
        return CqrsTestStreams.EmptyAsync();
    }

    /// <remarks>
    ///     Deliberately far longer than any test consumes. A short stream would let a cancellation test pass by
    ///     simply running to completion, which is the failure mode it exists to catch.
    /// </remarks>
    [HttpGet("long-running")]
    public IAsyncEnumerable<Chunk> LongRunning(CancellationToken cancellationToken)
    {
        return CqrsTestStreams.LongRunningAsync(cancellationToken, tickCount: 10_000);
    }

    [HttpGet("non-chunk-stream")]
    public IAsyncEnumerable<int> NonChunkStream()
    {
        return CqrsTestStreams.NonChunkAsync();
    }

    [HttpGet("plain-object")]
    public FinalResult PlainObject()
    {
        return new FinalResult("not-a-stream");
    }

    [HttpPost("submit")]
    public IAsyncEnumerable<Chunk> Submit([FromBody] SubmitCommand command)
    {
        return CqrsTestStreams.CompletedAsync(command);
    }
}

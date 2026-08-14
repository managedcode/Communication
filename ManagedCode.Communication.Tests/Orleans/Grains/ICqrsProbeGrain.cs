using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Orleans;

namespace ManagedCode.Communication.Tests.Orleans.Grains;

/// <summary>Progress payload carried across a grain boundary.</summary>
[GenerateSerializer]
public sealed record OrleansProgress([property: Id(0)] string State);

/// <summary>Terminal payload carried across a grain boundary.</summary>
[GenerateSerializer]
public sealed record OrleansReport([property: Id(0)] string Status);

/// <summary>
///     Verifies that a CQRS chunk can cross an Orleans grain boundary, both as a return value and as an async stream.
/// </summary>
public interface ICqrsProbeGrain : IGrainWithGuidKey
{
    Task<CqrsStreamChunk<OrleansProgress, OrleansReport>> EchoChunkAsync(
        CqrsStreamChunk<OrleansProgress, OrleansReport> chunk);

    IAsyncEnumerable<CqrsStreamChunk<OrleansProgress, OrleansReport>> StreamAsync();
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     One Server-Sent Events frame, exactly as it appeared on the wire.
/// </summary>
public sealed record SseFrame(string? EventType, string? Id, string Data)
{
    public Chunk DeserializeChunk()
    {
        return JsonSerializer.Deserialize<Chunk>(Data, CqrsStreamSerialization.Default)
               ?? throw new InvalidOperationException($"Frame '{EventType}' did not contain a chunk: {Data}");
    }
}

/// <summary>
///     Reads a Server-Sent Events response as raw frames.
/// </summary>
/// <remarks>
///     Deliberately does not use <c>SseParser</c>: these tests exist to verify the bytes the server produces
///     (<c>event:</c> and <c>id:</c> fields included), and a typed parser would hide exactly the fields under test.
/// </remarks>
public static class SseTestReader
{
    public static async Task<IReadOnlyList<SseFrame>> ReadFramesAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ParseFrames(body);
    }

    public static async Task<IReadOnlyList<Chunk>> ReadChunksAsync(HttpResponseMessage response)
    {
        var frames = await ReadFramesAsync(response).ConfigureAwait(false);

        var chunks = new List<Chunk>(frames.Count);
        foreach (var frame in frames)
        {
            chunks.Add(frame.DeserializeChunk());
        }

        return chunks;
    }

    public static IReadOnlyList<SseFrame> ParseFrames(string body)
    {
        ArgumentNullException.ThrowIfNull(body);

        var frames = new List<SseFrame>();

        foreach (var block in body.Replace("\r\n", "\n", StringComparison.Ordinal).Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            string? eventType = null;
            string? id = null;
            var data = new List<string>();

            foreach (var line in block.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith(':'))
                {
                    // Comment / keep-alive line.
                    continue;
                }

                var separator = line.IndexOf(':');
                if (separator < 0)
                {
                    continue;
                }

                var field = line[..separator];
                var value = line[(separator + 1)..].TrimStart(' ');

                switch (field)
                {
                    case "event":
                        eventType = value;
                        break;
                    case "id":
                        id = value;
                        break;
                    case "data":
                        data.Add(value);
                        break;
                }
            }

            if (data.Count > 0 || eventType is not null)
            {
                frames.Add(new SseFrame(eventType, id, string.Join('\n', data)));
            }
        }

        return frames;
    }
}

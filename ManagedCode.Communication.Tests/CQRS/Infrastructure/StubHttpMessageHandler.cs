using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     An <see cref="HttpMessageHandler" /> that returns a canned response and records the request it received,
///     so client-side behaviour can be tested without a server.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    /// <summary>The last request that reached the handler.</summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <summary>The body of the last request, read before the response was produced.</summary>
    public string? LastRequestBody { get; private set; }

    /// <summary>How many requests the handler has served.</summary>
    public int RequestCount { get; private set; }

    /// <summary>Responds with an SSE body built from <paramref name="chunks" />.</summary>
    public static StubHttpMessageHandler RespondingWithSse(params Chunk[] chunks)
    {
        var payload = BuildSsePayload(chunks);
        return RespondingWithRawSse(payload);
    }

    /// <summary>Responds with a verbatim SSE body, so malformed frames can be exercised.</summary>
    public static StubHttpMessageHandler RespondingWithRawSse(string payload, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/event-stream")
        }));
    }

    /// <summary>Responds with a non-success status and the given body.</summary>
    public static StubHttpMessageHandler RespondingWith(HttpStatusCode statusCode, string body, string contentType)
    {
        return new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, contentType)
        }));
    }

    /// <summary>Renders chunks as an SSE body, mirroring what the server transport writes.</summary>
    public static string BuildSsePayload(IEnumerable<Chunk> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var builder = new StringBuilder();
        foreach (var chunk in chunks)
        {
            builder.Append("event: ").Append(chunk.EventType).Append('\n');

            if (chunk.EventId is not null || chunk.Sequence is not null)
            {
                builder.Append("id: ").Append(chunk.EventId ?? chunk.Sequence!.Value.ToString()).Append('\n');
            }

            builder.Append("data: ")
                .Append(JsonSerializer.Serialize(chunk, CqrsStreamSerialization.Default))
                .Append("\n\n");
        }

        return builder.ToString();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        LastRequest = request;

        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        return await _handler(request, cancellationToken).ConfigureAwait(false);
    }
}

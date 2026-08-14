using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;

namespace ManagedCode.Communication.CQRS.Extensions.Http;

/// <summary>
///     HttpClient helpers for streaming CQRS chunk responses via Server-Sent Events.
/// </summary>
public static class CqrsHttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    ///     Sends a request built by <paramref name="requestFactory"/> and exposes streamed CQRS chunks from
    ///     <see cref="System.Net.ServerSentEvents.SseItem"/> payloads.
    /// </summary>
    /// <typeparam name="TProgress">The progress payload type.</typeparam>
    /// <typeparam name="TResult">The final successful result payload type.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="requestFactory">Creates a request for each retry or retry-like invocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestFactory);

        return SendForCqrsStreamCoreAsync<TProgress, TResult>(client, requestFactory, cancellationToken);
    }

    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);

        return SendForCqrsStreamAsync<TProgress, TResult>(
            client,
            () => new HttpRequestMessage(method, requestUri),
            cancellationToken);
    }

    /// <summary>
    ///     Sends a JSON POST request and reads CQRS stream chunks from the SSE response body.
    /// </summary>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        HttpMethod method,
        TRequest requestBody,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        return SendForCqrsStreamAsync<TProgress, TResult>(
            client,
            () => new HttpRequestMessage(method, requestUri)
            {
                Content = JsonContent.Create(requestBody)
            },
            cancellationToken);
    }

    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> GetForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return SendForCqrsStreamAsync<TProgress, TResult>(
            client,
            HttpMethod.Get,
            requestUri,
            cancellationToken);
    }

    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return SendForCqrsStreamAsync<TProgress, TResult>(client, HttpMethod.Post, requestUri, cancellationToken);
    }

    /// <summary>
    ///     Sends a JSON POST request and reads CQRS stream chunks from the SSE response body.
    /// </summary>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        TRequest requestBody,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        return SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
            client,
            requestUri,
            HttpMethod.Post,
            requestBody,
            cancellationToken);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamCoreAsync<TProgress, TResult>(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = requestFactory();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<TProgress, TResult>>(data, JsonOptions) ??
                                  throw new JsonException("The response did not contain a valid CQRS stream chunk."));

        await foreach (var item in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return item.Data;
        }
    }
}

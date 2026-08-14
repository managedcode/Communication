using System.Net.Http;

namespace ManagedCode.Communication.AspNetCore.Extensions.Http;

/// <summary>
///     Backward/monolithic package facade for CQRS streaming HttpClient helpers.
/// </summary>
public static class CqrsHttpClientExtensions
{
    /// <summary>
    ///     Sends a request built by <paramref name="requestFactory"/> and exposes streamed CQRS chunks from
    ///     <see cref="System.Net.ServerSentEvents.SseItem"/> payloads.
    /// </summary>
    public static System.Collections.Generic.IAsyncEnumerable<
        ManagedCode.Communication.CQRS.CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        System.Func<HttpRequestMessage> requestFactory,
        System.Threading.CancellationToken cancellationToken = default)
    {
        return ManagedCode.Communication.CQRS.Extensions.Http.CqrsHttpClientExtensions.SendForCqrsStreamAsync<
            TProgress, TResult>(client, requestFactory, cancellationToken);
    }

    /// <summary>
    ///     Sends a request with a specific method and parses CQRS stream chunks from SSE response.
    /// </summary>
    public static System.Collections.Generic.IAsyncEnumerable<
        ManagedCode.Communication.CQRS.CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        HttpMethod method,
        string requestUri,
        System.Threading.CancellationToken cancellationToken = default)
    {
        return ManagedCode.Communication.CQRS.Extensions.Http.CqrsHttpClientExtensions.SendForCqrsStreamAsync<
            TProgress, TResult>(client, method, requestUri, cancellationToken);
    }

    /// <summary>
    ///     Sends a JSON request and reads CQRS stream chunks from SSE response body.
    /// </summary>
    public static System.Collections.Generic.IAsyncEnumerable<
        ManagedCode.Communication.CQRS.CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        HttpMethod method,
        TRequest requestBody,
        System.Threading.CancellationToken cancellationToken = default)
        where TRequest : class
    {
        return ManagedCode.Communication.CQRS.Extensions.Http.CqrsHttpClientExtensions.SendForCqrsStreamAsync<
            TProgress, TResult, TRequest>(client, requestUri, method, requestBody, cancellationToken);
    }

    /// <summary>
    ///     Sends GET and parses CQRS stream chunks from SSE response body.
    /// </summary>
    public static System.Collections.Generic.IAsyncEnumerable<
        ManagedCode.Communication.CQRS.CqrsStreamChunk<TProgress, TResult>> GetForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        System.Threading.CancellationToken cancellationToken = default)
    {
        return ManagedCode.Communication.CQRS.Extensions.Http.CqrsHttpClientExtensions.GetForCqrsStreamAsync<
            TProgress, TResult>(client, requestUri, cancellationToken);
    }

    /// <summary>
    ///     Sends POST without request body and parses CQRS stream chunks from SSE response body.
    /// </summary>
    public static System.Collections.Generic.IAsyncEnumerable<
        ManagedCode.Communication.CQRS.CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        System.Threading.CancellationToken cancellationToken = default)
    {
        return ManagedCode.Communication.CQRS.Extensions.Http.CqrsHttpClientExtensions.PostForCqrsStreamAsync<
            TProgress, TResult>(client, requestUri, cancellationToken);
    }

    /// <summary>
    ///     Sends JSON POST and parses CQRS stream chunks from SSE response body.
    /// </summary>
    public static System.Collections.Generic.IAsyncEnumerable<
        ManagedCode.Communication.CQRS.CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        TRequest requestBody,
        System.Threading.CancellationToken cancellationToken = default)
        where TRequest : class
    {
        return ManagedCode.Communication.CQRS.Extensions.Http.CqrsHttpClientExtensions.PostForCqrsStreamAsync<
            TProgress, TResult, TRequest>(client, requestUri, requestBody, cancellationToken);
    }
}

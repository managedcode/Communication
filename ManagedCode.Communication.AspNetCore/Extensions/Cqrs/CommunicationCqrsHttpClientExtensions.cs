using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.Extensions.Http;
using CoreCqrsHttpClientExtensions = ManagedCode.Communication.CQRS.Extensions.Http.CqrsHttpClientExtensions;

namespace ManagedCode.Communication.AspNetCore.Extensions.Http;

/// <summary>
///     Facade over <see cref="CoreCqrsHttpClientExtensions" /> for applications that depend only on the monolithic
///     <c>ManagedCode.Communication.AspNetCore</c> package. Client-only applications should reference
///     <c>ManagedCode.Communication.CQRS</c> directly — it carries no ASP.NET Core dependency.
/// </summary>
public static class CqrsHttpClientExtensions
{
    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync{TProgress,TResult}(HttpClient,Func{HttpRequestMessage},CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync<TProgress, TResult>(
            client, requestFactory, options, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync{TProgress,TResult}(HttpClient,Func{HttpRequestMessage},CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        return CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync<TProgress, TResult>(
            client, requestFactory, null, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync{TProgress,TResult}(HttpClient,HttpMethod,string,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        HttpMethod method,
        string requestUri,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync<TProgress, TResult>(
            client, method, requestUri, options, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync{TProgress,TResult}(HttpClient,HttpMethod,string,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken)
    {
        return CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync<TProgress, TResult>(
            client, method, requestUri, null, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync{TProgress,TResult,TRequest}(HttpClient,string,HttpMethod,TRequest,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        HttpMethod method,
        TRequest requestBody,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        return CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
            client, requestUri, method, requestBody, options, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync{TProgress,TResult,TRequest}(HttpClient,string,HttpMethod,TRequest,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        HttpMethod method,
        TRequest requestBody,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        return CoreCqrsHttpClientExtensions.SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
            client, requestUri, method, requestBody, null, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.GetForCqrsStreamAsync{TProgress,TResult}(HttpClient,string,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> GetForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return CoreCqrsHttpClientExtensions.GetForCqrsStreamAsync<TProgress, TResult>(
            client, requestUri, options, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.GetForCqrsStreamAsync{TProgress,TResult}(HttpClient,string,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> GetForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
    {
        return CoreCqrsHttpClientExtensions.GetForCqrsStreamAsync<TProgress, TResult>(
            client, requestUri, null, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.PostForCqrsStreamAsync{TProgress,TResult}(HttpClient,string,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return CoreCqrsHttpClientExtensions.PostForCqrsStreamAsync<TProgress, TResult>(
            client, requestUri, options, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.PostForCqrsStreamAsync{TProgress,TResult}(HttpClient,string,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
    {
        return CoreCqrsHttpClientExtensions.PostForCqrsStreamAsync<TProgress, TResult>(
            client, requestUri, null, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.PostForCqrsStreamAsync{TProgress,TResult,TRequest}(HttpClient,string,TRequest,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        TRequest requestBody,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        return CoreCqrsHttpClientExtensions.PostForCqrsStreamAsync<TProgress, TResult, TRequest>(
            client, requestUri, requestBody, options, cancellationToken);
    }

    /// <inheritdoc cref="CoreCqrsHttpClientExtensions.PostForCqrsStreamAsync{TProgress,TResult,TRequest}(HttpClient,string,TRequest,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        TRequest requestBody,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        return CoreCqrsHttpClientExtensions.PostForCqrsStreamAsync<TProgress, TResult, TRequest>(
            client, requestUri, requestBody, null, cancellationToken);
    }
}

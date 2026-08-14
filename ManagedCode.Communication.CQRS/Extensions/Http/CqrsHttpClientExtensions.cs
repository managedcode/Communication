using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.CQRS.Extensions.Http;

/// <summary>
///     <see cref="HttpClient" /> helpers that read a CQRS command stream from a Server-Sent Events response.
/// </summary>
/// <remarks>
///     These helpers never throw for transport or protocol failures: a non-success status code, a dropped connection
///     or an undecodable frame all surface as a terminal <see cref="CqrsStreamChunkKind.Failed" /> chunk, mirroring
///     what the server-side transport does. Only cancellation propagates as an
///     <see cref="OperationCanceledException" />. Use <see cref="CqrsStreamClientOptions" /> to opt out.
/// </remarks>
public static class CqrsHttpClientExtensions
{
    private const string EventStreamMediaType = "text/event-stream";

    /// <summary>
    ///     Sends a request built by <paramref name="requestFactory" /> and reads CQRS chunks from the SSE response.
    /// </summary>
    /// <typeparam name="TProgress">Progress payload type.</typeparam>
    /// <typeparam name="TResult">Final (terminal) payload type.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="requestFactory">Builds the request. Invoked once, when enumeration starts.</param>
    /// <param name="options">Reader behaviour; <see cref="CqrsStreamClientOptions.Default" /> when omitted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestFactory);

        var effectiveOptions = options ?? CqrsStreamClientOptions.Default;

        return CqrsStreamNormalizer.NormalizeAsync(
            ReadCqrsStreamAsync<TProgress, TResult>(client, requestFactory, effectiveOptions, cancellationToken),
            effectiveOptions.AssignSequenceNumbers,
            effectiveOptions.EnsureTerminalChunk,
            cancellationToken);
    }

    /// <inheritdoc cref="SendForCqrsStreamAsync{TProgress,TResult}(HttpClient,Func{HttpRequestMessage},CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        return SendForCqrsStreamAsync<TProgress, TResult>(client, requestFactory, null, cancellationToken);
    }

    /// <summary>
    ///     Sends a bodyless request with the given method and reads CQRS chunks from the SSE response.
    /// </summary>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        HttpMethod method,
        string requestUri,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);

        return SendForCqrsStreamAsync<TProgress, TResult>(
            client,
            () => new HttpRequestMessage(method, requestUri),
            options,
            cancellationToken);
    }

    /// <inheritdoc cref="SendForCqrsStreamAsync{TProgress,TResult}(HttpClient,HttpMethod,string,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        HttpMethod method,
        string requestUri,
        CancellationToken cancellationToken)
    {
        return SendForCqrsStreamAsync<TProgress, TResult>(client, method, requestUri, null, cancellationToken);
    }

    /// <summary>
    ///     Sends a JSON request with the given method and reads CQRS chunks from the SSE response.
    /// </summary>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        HttpMethod method,
        TRequest requestBody,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);

        var jsonOptions = (options ?? CqrsStreamClientOptions.Default).ResolveJsonOptions();

        return SendForCqrsStreamAsync<TProgress, TResult>(
            client,
            () => new HttpRequestMessage(method, requestUri)
            {
                Content = JsonContent.Create(requestBody, options: jsonOptions)
            },
            options,
            cancellationToken);
    }

    /// <inheritdoc cref="SendForCqrsStreamAsync{TProgress,TResult,TRequest}(HttpClient,string,HttpMethod,TRequest,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        HttpMethod method,
        TRequest requestBody,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        return SendForCqrsStreamAsync<TProgress, TResult, TRequest>(client, requestUri, method, requestBody, null, cancellationToken);
    }

    /// <summary>
    ///     Sends a GET request and reads CQRS chunks from the SSE response.
    /// </summary>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> GetForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return SendForCqrsStreamAsync<TProgress, TResult>(client, HttpMethod.Get, requestUri, options, cancellationToken);
    }

    /// <inheritdoc cref="GetForCqrsStreamAsync{TProgress,TResult}(HttpClient,string,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> GetForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
    {
        return SendForCqrsStreamAsync<TProgress, TResult>(client, HttpMethod.Get, requestUri, null, cancellationToken);
    }

    /// <summary>
    ///     Sends a bodyless POST request and reads CQRS chunks from the SSE response.
    /// </summary>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return SendForCqrsStreamAsync<TProgress, TResult>(client, HttpMethod.Post, requestUri, options, cancellationToken);
    }

    /// <inheritdoc cref="PostForCqrsStreamAsync{TProgress,TResult}(HttpClient,string,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult>(
        this HttpClient client,
        string requestUri,
        CancellationToken cancellationToken)
    {
        return SendForCqrsStreamAsync<TProgress, TResult>(client, HttpMethod.Post, requestUri, null, cancellationToken);
    }

    /// <summary>
    ///     Sends a JSON POST request and reads CQRS chunks from the SSE response.
    /// </summary>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        TRequest requestBody,
        CqrsStreamClientOptions? options = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        return SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
            client,
            requestUri,
            HttpMethod.Post,
            requestBody,
            options,
            cancellationToken);
    }

    /// <inheritdoc cref="PostForCqrsStreamAsync{TProgress,TResult,TRequest}(HttpClient,string,TRequest,CqrsStreamClientOptions,CancellationToken)" />
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> PostForCqrsStreamAsync<TProgress, TResult, TRequest>(
        this HttpClient client,
        string requestUri,
        TRequest requestBody,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        return SendForCqrsStreamAsync<TProgress, TResult, TRequest>(
            client,
            requestUri,
            HttpMethod.Post,
            requestBody,
            null,
            cancellationToken);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> ReadCqrsStreamAsync<TProgress, TResult>(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        CqrsStreamClientOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = requestFactory();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(EventStreamMediaType));

        using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            yield return await CreateFailureChunkAsync<TProgress, TResult>(response, options, cancellationToken)
                .ConfigureAwait(false);

            yield break;
        }

        var jsonOptions = options.ResolveJsonOptions();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var parser = SseParser.Create(stream);

        await foreach (var item in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
        {
            // Keep-alive and heartbeat frames carry no payload; they are not protocol errors.
            if (string.IsNullOrWhiteSpace(item.Data))
            {
                continue;
            }

            CqrsStreamChunk<TProgress, TResult>? chunk;
            string? decodeError;

            try
            {
                chunk = JsonSerializer.Deserialize<CqrsStreamChunk<TProgress, TResult>>(item.Data, jsonOptions);
                decodeError = chunk is null
                    ? $"Frame '{item.EventType}' decoded to a null CQRS stream chunk."
                    : null;
            }
            catch (JsonException exception)
            {
                chunk = null;
                decodeError = $"Frame '{item.EventType}' is not a valid CQRS stream chunk: {exception.Message}";
            }

            if (decodeError is null)
            {
                yield return chunk!;
                continue;
            }

            switch (options.MalformedChunkBehavior)
            {
                case CqrsMalformedChunkBehavior.Skip:
                    continue;

                case CqrsMalformedChunkBehavior.Throw:
                    // Wrapped so stream normalization lets the caller's opt-in exception through untouched.
                    throw new CqrsStreamPassthroughException(new JsonException(decodeError));

                default:
                    yield return CqrsStreamChunk<TProgress, TResult>.Failed(
                        CqrsStreamProblems.Malformed(decodeError),
                        "The server sent a frame that could not be decoded.");

                    yield break;
            }
        }
    }

    private static async Task<CqrsStreamChunk<TProgress, TResult>> CreateFailureChunkAsync<TProgress, TResult>(
        HttpResponseMessage response,
        CqrsStreamClientOptions options,
        CancellationToken cancellationToken)
    {
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var problem = ParseProblemFromResponse(response, responseBody, options.ResolveJsonOptions())
                      ?? Problem.Create(
                          response.StatusCode,
                          string.IsNullOrWhiteSpace(responseBody)
                              ? "Request returned " + response.StatusCode
                              : responseBody);

        return CqrsStreamChunk<TProgress, TResult>.Failed(
            problem,
            "Request returned a non-success status code.");
    }

    private static Problem? ParseProblemFromResponse(
        HttpResponseMessage response,
        string responseBody,
        JsonSerializerOptions jsonOptions)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        Problem? parsedProblem;
        try
        {
            parsedProblem = JsonSerializer.Deserialize<Problem>(responseBody, jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }

        if (parsedProblem is null)
        {
            return null;
        }

        if (parsedProblem.StatusCode == 0)
        {
            parsedProblem.StatusCode = (int)response.StatusCode;
        }

        if (string.IsNullOrWhiteSpace(parsedProblem.Title))
        {
            parsedProblem.Title = response.ReasonPhrase ?? response.StatusCode.ToString();
        }

        if (string.IsNullOrWhiteSpace(parsedProblem.Type) ||
            string.Equals(parsedProblem.Type, ProblemConstants.Types.AboutBlank, StringComparison.Ordinal))
        {
            parsedProblem.Type = response.StatusCode.ToString();
        }

        return parsedProblem;
    }
}

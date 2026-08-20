using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Telemetry;

namespace ManagedCode.Communication.Extensions.Http;

/// <summary>
///     Helpers that execute <see cref="HttpClient"/> requests and transform the responses into
///     <see cref="ManagedCode.Communication.Result"/> instances.
/// </summary>
public static class ResultHttpClientExtensions
{
    /// <summary>
    ///     Sends a request built by <paramref name="requestFactory"/> and converts the HTTP response into a
    ///     <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="T">The JSON payload type that the endpoint returns in case of success.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> used to send the request.</param>
    /// <param name="requestFactory">Factory that creates a fresh <see cref="HttpRequestMessage"/> for each attempt.</param>
    /// <param name="cancellationToken">Token that cancels the request execution.</param>
    public static async Task<Result<T>> SendForResultAsync<T>(
        this HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestFactory);

        try
        {
            return await SendCoreAsync(
                    client,
                    requestFactory,
                    static response => response.FromJsonToResult<T>(),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            return Result<T>.Fail(TransportProblem(exception, HttpStatusCode.ServiceUnavailable));
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<T>.Fail(TransportProblem(exception, HttpStatusCode.GatewayTimeout));
        }
    }

    /// <summary>
    ///     Sends a request and lets <paramref name="readSuccessAsync"/> project a successful HTTP response while the
    ///     library continues to own transport failures and RFC problem responses.
    /// </summary>
    /// <typeparam name="T">The projected success value.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> used to send the request.</param>
    /// <param name="requestFactory">Factory that creates a fresh request for each attempt.</param>
    /// <param name="readSuccessAsync">Reads a successful response into the projected value.</param>
    /// <param name="cancellationToken">Token that cancels request execution and response reading.</param>
    public static async Task<Result<T>> SendForResultAsync<T>(
        this HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        Func<HttpResponseMessage, CancellationToken, Task<T?>> readSuccessAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestFactory);
        ArgumentNullException.ThrowIfNull(readSuccessAsync);

        try
        {
            return await SendCoreAsync(
                    client,
                    requestFactory,
                    response => ReadProjectedResultAsync(response, readSuccessAsync, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            return Result<T>.Fail(TransportProblem(exception, HttpStatusCode.ServiceUnavailable));
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<T>.Fail(TransportProblem(exception, HttpStatusCode.GatewayTimeout));
        }
    }

    /// <summary>
    ///     Sends a request built by <paramref name="requestFactory"/> and converts the HTTP response into a
    ///     <see cref="Result"/> without a payload.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> used to send the request.</param>
    /// <param name="requestFactory">Factory that creates a fresh <see cref="HttpRequestMessage"/> for each attempt.</param>
    /// <param name="cancellationToken">Token that cancels the request execution.</param>
    public static async Task<Result> SendForResultAsync(
        this HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestFactory);

        try
        {
            return await SendCoreAsync(
                    client,
                    requestFactory,
                    static response => response.FromRequestToResult(),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            return Result.Fail(TransportProblem(exception, HttpStatusCode.ServiceUnavailable));
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Fail(TransportProblem(exception, HttpStatusCode.GatewayTimeout));
        }
    }

    /// <summary>
    ///     Performs a GET request for <paramref name="requestUri"/> and converts the response into a
    ///     <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="T">The JSON payload type that the endpoint returns in case of success.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> used to send the request.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="cancellationToken">Token that cancels the request execution.</param>
    public static Task<Result<T>> GetAsResultAsync<T>(
        this HttpClient client,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(requestUri);

        return client.SendForResultAsync<T>(
            () => new HttpRequestMessage(HttpMethod.Get, requestUri),
            cancellationToken);
    }

    /// <summary>
    ///     Performs a GET request for <paramref name="requestUri"/> and converts the response into a non generic
    ///     <see cref="Result"/>.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> used to send the request.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="cancellationToken">Token that cancels the request execution.</param>
    public static Task<Result> GetAsResultAsync(
        this HttpClient client,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(requestUri);

        return client.SendForResultAsync(
            () => new HttpRequestMessage(HttpMethod.Get, requestUri),
            cancellationToken);
    }

    private static async Task<TResponse> SendCoreAsync<TResponse>(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        Func<HttpResponseMessage, Task<TResponse>> convert,
        CancellationToken cancellationToken)
    {
        using var request = requestFactory();
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await convert(response).ConfigureAwait(false);
    }

    private static async Task<Result<T>> ReadProjectedResultAsync<T>(
        HttpResponseMessage response,
        Func<HttpResponseMessage, CancellationToken, Task<T?>> readSuccessAsync,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var failure = await response.FromRequestToResult(cancellationToken).ConfigureAwait(false);
            return Result<T>.Fail(failure.Problem ?? Problem.GenericError());
        }

        try
        {
            var value = await readSuccessAsync(response, cancellationToken).ConfigureAwait(false);
            return value is null
                ? InvalidProjectedResponse<T>(response, new JsonException("The projected response payload was null."))
                : Result<T>.Succeed(value);
        }
        catch (JsonException exception)
        {
            return InvalidProjectedResponse<T>(response, exception);
        }
        catch (NotSupportedException exception)
        {
            return InvalidProjectedResponse<T>(response, exception);
        }
    }

    private static Result<T> InvalidProjectedResponse<T>(HttpResponseMessage response, Exception exception)
    {
        var problem = Problem.Create(
            "Invalid response body",
            $"The successful response could not be projected to {typeof(T).Name}: {exception.Message}",
            (int)response.StatusCode);
        CommunicationDiagnostics.ReportFailure(CommunicationLogger.GetLogger(), problem, exception);
        return Result<T>.Fail(problem);
    }

    private static Problem TransportProblem(Exception exception, HttpStatusCode statusCode)
    {
        var problem = Problem.Create(
            "HTTP transport failure",
            exception.Message,
            statusCode);
        CommunicationDiagnostics.ReportFailure(CommunicationLogger.GetLogger(), problem, exception);
        return problem;
    }
}

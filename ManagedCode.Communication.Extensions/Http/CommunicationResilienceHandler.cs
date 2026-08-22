using System;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Constants;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Extensions.Http;

internal sealed class CommunicationResilienceHandler : DelegatingHandler
{
    private readonly CommandExecutionRuntime _runtime;
    private readonly Func<HttpRequestMessage, bool> _shouldHandle;

    public CommunicationResilienceHandler(CommandHttpClientOptions options, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options.ShouldHandle);

        _shouldHandle = options.ShouldHandle;
        _runtime = new CommandExecutionRuntime(options.Execution, logger: logger);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Content is not null || !_shouldHandle(request))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var command = Command.Create(HttpCommandExecutionConstants.CommandType);
        command.CorrelationId = ResolveAuthority(request.RequestUri);

        HttpResponseMessage? lastResponse = null;
        ExceptionDispatchInfo? lastException = null;
        var result = await CommandExecutor.ExecuteResultAsync<Command, HttpResponseMessage>(
                command,
                async (_, token) =>
                {
                    lastResponse?.Dispose();
                    lastResponse = null;
                    lastException = null;

                    using var attempt = CloneRequest(request);
                    try
                    {
                        var response = await base.SendAsync(attempt, token).ConfigureAwait(false);
                        response.RequestMessage = request;
                        lastResponse = response;
                        if (response.IsSuccessStatusCode)
                        {
                            return Result<HttpResponseMessage>.Succeed(response);
                        }

                        return Result<HttpResponseMessage>.Fail(CreateProblem(response));
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException
                                                       || !cancellationToken.IsCancellationRequested)
                    {
                        lastException = ExceptionDispatchInfo.Capture(exception);
                        throw;
                    }
                },
                _runtime,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        if (lastResponse is not null)
        {
            return lastResponse;
        }

        lastException?.Throw();
        throw new HttpRequestException(
            result.Problem?.Detail ?? HttpCommandExecutionConstants.ExecutionFailedDetail,
            null,
            HttpStatusCode.ServiceUnavailable);
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage source)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri)
        {
            Version = source.Version,
            VersionPolicy = source.VersionPolicy
        };

        foreach (var header in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in source.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return clone;
    }

    private static Problem CreateProblem(HttpResponseMessage response)
    {
        var problem = Problem.Create(
            response.ReasonPhrase ?? HttpCommandExecutionConstants.FailureTitle,
            HttpCommandExecutionConstants.FailureDetail,
            response.StatusCode);
        PromoteRetryAfter(response, problem);
        return problem;
    }

    private static void PromoteRetryAfter(HttpResponseMessage response, Problem problem)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta && delta >= TimeSpan.Zero)
        {
            problem.Extensions[ProblemConstants.ExtensionKeys.RetryAfter] = delta;
            return;
        }

        if (retryAfter?.Date is { } date)
        {
            var remaining = date - DateTimeOffset.UtcNow;
            problem.Extensions[ProblemConstants.ExtensionKeys.RetryAfter] =
                remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    private static string ResolveAuthority(Uri? requestUri)
    {
        return requestUri is { IsAbsoluteUri: true }
            ? requestUri.GetLeftPart(UriPartial.Authority)
            : HttpCommandExecutionConstants.UnknownAuthority;
    }
}

internal static class HttpCommandExecutionConstants
{
    public const string CommandType = "http.client.send";
    public const string UnknownAuthority = "unknown-authority";
    public const string FailureTitle = "HTTP request failed";
    public const string FailureDetail = "The HTTP dependency returned a non-success status code.";
    public const string ExecutionFailedDetail = "HTTP command execution failed before a response was received.";
}

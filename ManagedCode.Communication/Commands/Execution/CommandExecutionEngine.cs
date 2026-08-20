using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands.Extensions;
using ManagedCode.Communication.Results;
using ManagedCode.Communication.Telemetry;

namespace ManagedCode.Communication.Commands.Execution;

internal static class CommandExecutionEngine
{
    public static async ValueTask<TResult> ExecuteAsync<TResult>(
        ICommand command,
        Func<ICommand, CancellationToken, ValueTask<TResult>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken)
        where TResult : struct, IResult, IResultFactory<TResult>
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(runtime);

        Validate(runtime.Options);

        var startedAt = runtime.TimeProvider.GetTimestamp();
        using var activity = CommunicationTelemetry.StartCommandExecution(command);
        using var timeoutSource = CreateTimeoutSource(command, runtime);
        using var linkedSource = timeoutSource is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        var executionToken = linkedSource?.Token ?? cancellationToken;

        try
        {
            TResult result;
            if (runtime.Options.Idempotency.Enabled && runtime.IdempotencyStore is not null)
            {
                if (command.CommandId == Guid.Empty)
                {
                    var invalidCommand = TResult.Fail(Problem.Create(
                        "Invalid command identifier",
                        "Idempotent command execution requires a non-empty CommandId.",
                        HttpStatusCode.BadRequest));
                    CommunicationTelemetry.RecordCommandCompleted(
                        command,
                        invalidCommand,
                        runtime.TimeProvider.GetElapsedTime(startedAt));
                    return invalidCommand;
                }

                result = await runtime.IdempotencyStore.ExecuteIdempotentAsync(
                        command.CommandId.ToString("D"),
                        token => ExecuteRetriesAsync(command, handler, runtime, activity, token).AsTask(),
                        executionToken)
                    .ConfigureAwait(false);
            }
            else
            {
                result = await ExecuteRetriesAsync(command, handler, runtime, activity, executionToken)
                    .ConfigureAwait(false);
            }

            CommunicationTelemetry.RecordCommandCompleted(command, result, runtime.TimeProvider.GetElapsedTime(startedAt));
            return result;
        }
        catch (OperationCanceledException) when (timeoutSource?.IsCancellationRequested == true
                                                 && !cancellationToken.IsCancellationRequested)
        {
            var timeout = ResolveTimeout(command, runtime.Options.Timeout);
            var problem = Problem.Create(
                "Command timed out",
                $"The command exceeded its {timeout.TotalMilliseconds:0} ms execution timeout.",
                HttpStatusCode.RequestTimeout);
            problem.Extensions["timeoutMilliseconds"] = timeout.TotalMilliseconds;
            CommunicationTelemetry.RecordCommandTimeout(command, problem, activity);
            var result = TResult.Fail(problem);
            CommunicationTelemetry.RecordCommandCompleted(
                command,
                result,
                runtime.TimeProvider.GetElapsedTime(startedAt));
            return result;
        }
        finally
        {
            if (command.Metadata is not null)
            {
                command.Metadata.ExecutionTime = runtime.TimeProvider.GetElapsedTime(startedAt);
            }
        }
    }

    private static async ValueTask<TResult> ExecuteRetriesAsync<TResult>(
        ICommand command,
        Func<ICommand, CancellationToken, ValueTask<TResult>> handler,
        CommandExecutionRuntime runtime,
        Activity? activity,
        CancellationToken cancellationToken)
        where TResult : struct, IResult, IResultFactory<TResult>
    {
        var retry = runtime.Options.Retry;
        var maxRetries = retry.Enabled ? ResolveMaxRetries(command, retry.MaxRetries) : 0;

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var attemptStartedAt = runtime.TimeProvider.GetTimestamp();
            TResult result;
            Exception? exception = null;

            try
            {
                result = await ExecuteAttemptAsync(command, handler, runtime, attempt, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception caught)
            {
                exception = caught;
                var problem = Problem.Create(caught, HttpStatusCode.InternalServerError);
                CommunicationDiagnostics.ReportFailure(runtime.Logger, problem, caught);
                result = TResult.Fail(problem);
            }

            CommunicationTelemetry.RecordCommandAttempt(
                command,
                attempt,
                result,
                runtime.TimeProvider.GetElapsedTime(attemptStartedAt));

            if (result.IsSuccess)
            {
                return result;
            }

            var failure = result.Problem!;
            var shouldRetry = exception is null
                ? retry.ShouldRetry(failure)
                : retry.ShouldRetryException(exception);
            var retryNumber = attempt;

            if (!shouldRetry || retryNumber > maxRetries)
            {
                if (shouldRetry && retryNumber > maxRetries)
                {
                    failure.Extensions["retryAttempts"] = attempt;
                    failure.Extensions["retriesExhausted"] = true;
                    CommunicationTelemetry.RecordRetriesExhausted(command, attempt, failure, activity);

                    if (retry.OnRetriesExhausted is not null)
                    {
                        await retry.OnRetriesExhausted(
                                new CommandRetryEvent(command, attempt, retryNumber, TimeSpan.Zero, failure, exception),
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                return result;
            }

            var delay = CalculateDelay(retry, retryNumber, failure);
            if (command.Metadata is not null)
            {
                command.Metadata.RetryCount = retryNumber;
            }

            CommunicationTelemetry.RecordCommandRetry(command, attempt, delay, failure, activity);
            if (retry.OnRetry is not null)
            {
                await retry.OnRetry(
                        new CommandRetryEvent(command, attempt, retryNumber, delay, failure, exception),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, runtime.TimeProvider, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask<TResult> ExecuteAttemptAsync<TResult>(
        ICommand command,
        Func<ICommand, CancellationToken, ValueTask<TResult>> handler,
        CommandExecutionRuntime runtime,
        int attempt,
        CancellationToken cancellationToken)
        where TResult : struct, IResult, IResultFactory<TResult>
    {
        if (!runtime.Options.RateLimiter.Enabled || runtime.RateLimiter is null)
        {
            return await handler(command, cancellationToken).ConfigureAwait(false);
        }

        var queuedAt = runtime.TimeProvider.GetTimestamp();
        await using var lease = await runtime.RateLimiter.AcquireAsync(command, cancellationToken).ConfigureAwait(false);
        var queueDuration = runtime.TimeProvider.GetElapsedTime(queuedAt);

        if (lease.WasQueued)
        {
            CommunicationTelemetry.RecordRateLimitQueued(command, queueDuration);
            if (runtime.Options.RateLimiter.OnQueued is not null)
            {
                await runtime.Options.RateLimiter.OnQueued(
                        new CommandRateLimitEvent(command, queueDuration, lease.Problem),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (!lease.IsAcquired)
        {
            var problem = lease.Problem ?? Problem.Create(
                "Command rate limit exceeded",
                "The command could not acquire a rate-limit permit.",
                HttpStatusCode.TooManyRequests);

            // A custom or distributed limiter may keep protocol metadata on the lease instead of duplicating it
            // on the Problem. Promote it here so retry decisions (especially Retry-After) behave consistently for
            // every ICommandRateLimiter implementation. An adapter-supplied Problem remains authoritative.
            foreach (var pair in lease.Metadata)
            {
                if (!problem.Extensions.ContainsKey(pair.Key))
                {
                    problem.Extensions[pair.Key] = pair.Value;
                }
            }

            problem.Extensions["attempt"] = attempt;
            CommunicationTelemetry.RecordRateLimitRejected(command, problem);

            if (runtime.Options.RateLimiter.OnRejected is not null)
            {
                await runtime.Options.RateLimiter.OnRejected(
                        new CommandRateLimitEvent(command, queueDuration, problem),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return TResult.Fail(problem);
        }

        return await handler(command, cancellationToken).ConfigureAwait(false);
    }

    private static CancellationTokenSource? CreateTimeoutSource(ICommand command, CommandExecutionRuntime runtime)
    {
        if (!runtime.Options.Timeout.Enabled)
        {
            return null;
        }

        var timeout = ResolveTimeout(command, runtime.Options.Timeout);
        return timeout == Timeout.InfiniteTimeSpan
            ? null
            : new CancellationTokenSource(timeout, runtime.TimeProvider);
    }

    private static TimeSpan ResolveTimeout(ICommand command, TimeoutOptions timeoutOptions)
    {
        if (timeoutOptions.Timeout is not null)
        {
            return timeoutOptions.Timeout.Value;
        }

        return command.Metadata?.TimeoutSeconds is > 0
            ? TimeSpan.FromSeconds(command.Metadata.TimeoutSeconds)
            : Timeout.InfiniteTimeSpan;
    }

    private static int ResolveMaxRetries(ICommand command, int configuredMaxRetries)
    {
        return command.Metadata is null
            ? configuredMaxRetries
            : Math.Max(0, Math.Min(configuredMaxRetries, command.Metadata.MaxRetries));
    }

    private static TimeSpan CalculateDelay(RetryOptions options, int retryNumber, Problem problem)
    {
        if (problem.Extensions.TryGetValue("retryAfter", out var retryAfter))
        {
            if (retryAfter is TimeSpan retryAfterTimeSpan)
            {
                if (retryAfterTimeSpan <= TimeSpan.Zero)
                {
                    return TimeSpan.Zero;
                }

                return retryAfterTimeSpan > options.MaxDelay ? options.MaxDelay : retryAfterTimeSpan;
            }

            if (retryAfter is double retryAfterSeconds)
            {
                if (!double.IsFinite(retryAfterSeconds) || retryAfterSeconds <= 0D)
                {
                    return TimeSpan.Zero;
                }

                return TimeSpan.FromSeconds(Math.Min(retryAfterSeconds, options.MaxDelay.TotalSeconds));
            }
        }

        var multiplier = options.BackoffType switch
        {
            RetryBackoffType.Constant => 1D,
            RetryBackoffType.Linear => retryNumber,
            RetryBackoffType.Exponential => Math.Pow(2, retryNumber - 1),
            _ => 1D
        };
        var milliseconds = Math.Min(options.Delay.TotalMilliseconds * multiplier, options.MaxDelay.TotalMilliseconds);

        if (options.UseJitter && milliseconds > 0)
        {
            milliseconds *= 0.8D + Math.Clamp(options.Randomizer(), 0D, 1D) * 0.4D;
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static void Validate(CommandExecutionOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(options.Retry.MaxRetries);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.Retry.Delay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.Retry.MaxDelay, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(options.Retry.ShouldRetry);
        ArgumentNullException.ThrowIfNull(options.Retry.ShouldRetryException);
        ArgumentNullException.ThrowIfNull(options.Retry.Randomizer);

        if (options.Timeout.Enabled && options.Timeout.Timeout is { } timeout
            && timeout != Timeout.InfiniteTimeSpan
            && timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options.Timeout.Timeout));
        }
    }
}

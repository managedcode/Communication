using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Constants;
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

        var startedAt = runtime.TimeProvider.GetTimestamp();
        using var activity = CommunicationTelemetry.StartCommandExecution(command);
        if (TryGetLifetimeProblem(command, out var lifetimeProblem))
        {
            var expired = TResult.Fail(lifetimeProblem!);
            CommunicationTelemetry.RecordCommandCompleted(command, expired, runtime.TimeProvider.GetElapsedTime(startedAt));
            return expired;
        }

        CommunicationTelemetry.RecordActiveExecution(command, 1);
        var totalTimeout = Timeout.InfiniteTimeSpan;
        CancellationTokenSource? timeoutSource = null;
        CancellationTokenSource? linkedSource = null;

        try
        {
            totalTimeout = ResolveTotalTimeout(command, runtime.OptionsSnapshot.Timeout);
            ValidateResolvedTimeout(totalTimeout, CommandExecutionConstants.TotalTimeoutKind);
            timeoutSource = CreateTimeoutSource(totalTimeout, runtime.TimeProvider);
            linkedSource = timeoutSource is null
                ? null
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
            var executionToken = linkedSource?.Token ?? cancellationToken;
            TResult result;
            if (runtime.OptionsSnapshot.Idempotency.Enabled && runtime.IdempotencyStore is not null)
            {
                if (command.CommandId == Guid.Empty)
                {
                    var invalidCommand = TResult.Fail(Problem.Create(
                        ProblemConstants.CommandExecutionTitles.InvalidCommandIdentifier,
                        ProblemConstants.CommandExecutionMessages.IdempotencyRequiresCommandId,
                        HttpStatusCode.BadRequest));
                    CommunicationTelemetry.RecordCommandCompleted(
                        command,
                        invalidCommand,
                        runtime.TimeProvider.GetElapsedTime(startedAt));
                    return invalidCommand;
                }

                result = await ExecuteIdempotentlyAsync(
                        command,
                        handler,
                        runtime,
                        activity,
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
            var problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandTimedOut,
                string.Format(CommandExecutionConstants.TotalTimeoutDetailFormat, totalTimeout.TotalMilliseconds),
                HttpStatusCode.RequestTimeout);
            problem.Extensions[ProblemConstants.ExtensionKeys.TimeoutMilliseconds] = totalTimeout.TotalMilliseconds;
            problem.Extensions[ProblemConstants.ExtensionKeys.TimeoutKind] = CommandTimeoutKind.Total.ToString();
            CommunicationTelemetry.RecordCommandTimeout(command, problem, activity);
            await InvokeTimeoutCallbackWithoutMaskingAsync(
                    runtime,
                    new CommandTimeoutEvent(command, CommandTimeoutKind.Total, totalTimeout, null, problem),
                    cancellationToken)
                .ConfigureAwait(false);
            var result = TResult.Fail(problem);
            CommunicationTelemetry.RecordCommandCompleted(
                command,
                result,
                runtime.TimeProvider.GetElapsedTime(startedAt));
            return result;
        }
        catch (Exception caught) when (caught is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            var problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandExecutionInfrastructureFailure,
                ProblemConstants.CommandExecutionMessages.InfrastructureFailure,
                HttpStatusCode.InternalServerError);
            CommunicationDiagnostics.ReportInfrastructureFailure(runtime.Logger, problem, caught);
            var result = TResult.Fail(problem);
            CommunicationTelemetry.RecordCommandCompleted(
                command,
                result,
                runtime.TimeProvider.GetElapsedTime(startedAt));
            return result;
        }
        finally
        {
            linkedSource?.Dispose();
            timeoutSource?.Dispose();
            CommunicationTelemetry.RecordActiveExecution(command, -1);
            if (command.Metadata is not null)
            {
                command.Metadata.ExecutionTime = runtime.TimeProvider.GetElapsedTime(startedAt);
            }
        }
    }

    private static bool TryGetLifetimeProblem(ICommand command, out Problem? problem)
    {
        if (command.Metadata?.TimeToLiveSeconds is not { } timeToLiveSeconds)
        {
            problem = null;
            return false;
        }

        if (timeToLiveSeconds <= 0)
        {
            problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.InvalidCommandLifetime,
                ProblemConstants.CommandExecutionMessages.InvalidTimeToLive,
                HttpStatusCode.BadRequest);
            return true;
        }

        DateTime expiresAtUtc;
        try
        {
            expiresAtUtc = command.Timestamp.AddSeconds(timeToLiveSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.InvalidCommandLifetime,
                ProblemConstants.CommandExecutionMessages.UnsupportedTimeToLiveRange,
                HttpStatusCode.BadRequest);
            return true;
        }

        if (expiresAtUtc <= DateTime.UtcNow)
        {
            problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandExpired,
                ProblemConstants.CommandExecutionMessages.CommandExpired,
                HttpStatusCode.Gone);
            problem.Extensions[ProblemConstants.ExtensionKeys.ExpiredAtUtc] = expiresAtUtc;
            return true;
        }

        problem = null;
        return false;
    }

    private static async ValueTask<TResult> ExecuteIdempotentlyAsync<TResult>(
        ICommand command,
        Func<ICommand, CancellationToken, ValueTask<TResult>> handler,
        CommandExecutionRuntime runtime,
        Activity? activity,
        CancellationToken cancellationToken)
        where TResult : struct, IResult, IResultFactory<TResult>
    {
        var store = runtime.IdempotencyStore!;
        var options = runtime.OptionsSnapshot.Idempotency;
        if (!TryCreateIdempotencyDescriptor<TResult>(command, options, out var descriptor, out var configurationProblem))
        {
            return TResult.Fail(configurationProblem!);
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CommandIdempotencyAcquireResult<TResult> acquisition;
            try
            {
                acquisition = await store.TryAcquireAsync<TResult>(descriptor!, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                CommunicationTelemetry.RecordIdempotencyEvent(
                    command,
                    CommandExecutionConstants.IdempotencyStoreErrorOutcome);
                throw;
            }

            switch (acquisition.State)
            {
                case CommandIdempotencyAcquireState.Completed:
                    CommunicationTelemetry.RecordIdempotencyEvent(command, CommandExecutionConstants.IdempotencyHitOutcome);
                    return acquisition.HasOutcome
                        ? acquisition.Outcome!
                        : TResult.Fail(acquisition.Problem ?? Problem.Create(
                            ProblemConstants.CommandExecutionTitles.CorruptIdempotencyOutcome,
                            ProblemConstants.CommandExecutionMessages.MissingCachedOutcome,
                            HttpStatusCode.InternalServerError));

                case CommandIdempotencyAcquireState.Conflict:
                    CommunicationTelemetry.RecordIdempotencyEvent(
                        command,
                        CommandExecutionConstants.IdempotencyConflictOutcome);
                    return TResult.Fail(acquisition.Problem ?? Problem.Create(
                        ProblemConstants.CommandExecutionTitles.IdempotencyConflict,
                        ProblemConstants.CommandExecutionMessages.UnsafeIdempotencyConflict,
                        HttpStatusCode.Conflict));

                case CommandIdempotencyAcquireState.Indeterminate:
                    CommunicationTelemetry.RecordIdempotencyEvent(
                        command,
                        CommandExecutionConstants.IdempotencyIndeterminateOutcome);
                    return TResult.Fail(acquisition.Problem ?? Problem.Create(
                        ProblemConstants.CommandExecutionTitles.IdempotencyConflict,
                        ProblemConstants.CommandExecutionMessages.UnsafeIdempotencyConflict,
                        HttpStatusCode.Conflict));

                case CommandIdempotencyAcquireState.Running:
                    CommunicationTelemetry.RecordIdempotencyEvent(command, CommandExecutionConstants.IdempotencyWaitOutcome);
                    await Task.Delay(options.DuplicatePollInterval, runtime.TimeProvider, cancellationToken)
                        .ConfigureAwait(false);
                    continue;

                case CommandIdempotencyAcquireState.Acquired when acquisition.Claim is not null:
                    CommunicationTelemetry.RecordIdempotencyEvent(command, CommandExecutionConstants.IdempotencyMissOutcome);
                    return await ExecuteOwnedIdempotentAsync(
                            command,
                            handler,
                            runtime,
                            activity,
                            acquisition.Claim,
                            cancellationToken)
                        .ConfigureAwait(false);

                default:
                    return TResult.Fail(Problem.Create(
                        ProblemConstants.CommandExecutionTitles.InvalidIdempotencyResponse,
                        ProblemConstants.CommandExecutionMessages.InvalidStoreAcquisition,
                        HttpStatusCode.InternalServerError));
            }
        }
    }

    private static async ValueTask<TResult> ExecuteOwnedIdempotentAsync<TResult>(
        ICommand command,
        Func<ICommand, CancellationToken, ValueTask<TResult>> handler,
        CommandExecutionRuntime runtime,
        Activity? activity,
        CommandIdempotencyClaim claim,
        CancellationToken cancellationToken)
        where TResult : struct, IResult, IResultFactory<TResult>
    {
        var handlerInvoked = false;
        using var renewalStop = new CancellationTokenSource();
        using var ownershipLost = new CancellationTokenSource();
        using var ownedExecution = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            ownershipLost.Token);
        var renewalFailure = new TaskCompletionSource<Problem>(TaskCreationOptions.RunContinuationsAsynchronously);
        var renewalTask = RenewClaimUntilStoppedAsync(
            runtime,
            claim,
            renewalStop.Token,
            ownershipLost,
            renewalFailure);

        ValueTask<TResult> TrackedHandler(ICommand current, CancellationToken token)
        {
            handlerInvoked = true;
            return handler(current, token);
        }

        try
        {
            var result = await ExecuteRetriesAsync(command, TrackedHandler, runtime, activity, ownedExecution.Token)
                .ConfigureAwait(false);
            await StopClaimRenewalAsync(renewalStop, renewalTask).ConfigureAwait(false);

            if (ownershipLost.IsCancellationRequested)
            {
                var renewalProblem = await renewalFailure.Task.ConfigureAwait(false);
                await MarkOwnedExecutionUncertainAsync(runtime, claim, handlerInvoked, renewalProblem)
                    .ConfigureAwait(false);
                return TResult.Fail(renewalProblem);
            }

            var shouldCache = runtime.OptionsSnapshot.Idempotency.ShouldCacheOutcome(
                new CommandIdempotencyOutcomeContext(command, result, handlerInvoked));

            var finalized = shouldCache
                ? await TryFinalizeAsync(
                        runtime,
                        token => runtime.IdempotencyStore!.TryCompleteAsync(
                            claim,
                            result,
                            runtime.OptionsSnapshot.Idempotency.OutcomeRetention,
                            token))
                    .ConfigureAwait(false)
                : await TryFinalizeAsync(
                        runtime,
                        token => runtime.IdempotencyStore!.TryReleaseAsync(claim, token))
                    .ConfigureAwait(false);

            if (finalized)
            {
                return result;
            }

            var ownershipProblem = Problem.Create(
                    ProblemConstants.CommandExecutionTitles.IdempotencyOwnershipLost,
                    ProblemConstants.CommandExecutionMessages.IdempotencyOwnershipLost,
                    HttpStatusCode.Conflict);
            await MarkOwnedExecutionUncertainAsync(runtime, claim, handlerInvoked, ownershipProblem)
                .ConfigureAwait(false);
            return TResult.Fail(ownershipProblem);
        }
        catch (OperationCanceledException) when (ownershipLost.IsCancellationRequested
                                                 && !cancellationToken.IsCancellationRequested)
        {
            var ownershipProblem = await renewalFailure.Task.ConfigureAwait(false);
            await MarkOwnedExecutionUncertainAsync(runtime, claim, handlerInvoked, ownershipProblem)
                .ConfigureAwait(false);
            return TResult.Fail(ownershipProblem);
        }
        catch (OperationCanceledException)
        {
            var problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.IndeterminateCommandOutcome,
                ProblemConstants.CommandExecutionMessages.CancelledIndeterminateOutcome,
                HttpStatusCode.Conflict);
            await MarkOwnedExecutionUncertainAsync(runtime, claim, handlerInvoked, problem).ConfigureAwait(false);

            throw;
        }
        catch (Exception)
        {
            var problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.IndeterminateCommandOutcome,
                ProblemConstants.CommandExecutionMessages.InfrastructureIndeterminateOutcome,
                HttpStatusCode.Conflict);
            await MarkOwnedExecutionUncertainAsync(runtime, claim, handlerInvoked, problem).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await StopClaimRenewalAsync(renewalStop, renewalTask).ConfigureAwait(false);
        }
    }

    private static async Task RenewClaimUntilStoppedAsync(
        CommandExecutionRuntime runtime,
        CommandIdempotencyClaim claim,
        CancellationToken stopToken,
        CancellationTokenSource ownershipLost,
        TaskCompletionSource<Problem> renewalFailure)
    {
        var renewalInterval = TimeSpan.FromTicks(Math.Max(1, runtime.OptionsSnapshot.Idempotency.ClaimLease.Ticks / 3));
        while (!stopToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(renewalInterval, runtime.TimeProvider, stopToken).ConfigureAwait(false);
                using var renewalTimeout = new CancellationTokenSource(
                    runtime.OptionsSnapshot.Idempotency.FinalizationTimeout,
                    runtime.TimeProvider);
                using var renewalToken = CancellationTokenSource.CreateLinkedTokenSource(stopToken, renewalTimeout.Token);
                if (await runtime.IdempotencyStore!.TryRenewAsync(
                        claim,
                        runtime.OptionsSnapshot.Idempotency.ClaimLease,
                        renewalToken.Token)
                    .ConfigureAwait(false))
                {
                    continue;
                }

                FailRenewal(ProblemConstants.CommandExecutionMessages.RenewalRejected);
                return;
            }
            catch (OperationCanceledException) when (stopToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception caught)
            {
                var problem = Problem.Create(
                    ProblemConstants.CommandExecutionTitles.IdempotencyClaimRenewalFailed,
                    ProblemConstants.CommandExecutionMessages.RenewalFailed,
                    HttpStatusCode.Conflict);
                CommunicationDiagnostics.ReportInfrastructureFailure(
                    runtime.Logger,
                    problem,
                    caught,
                    CommandExecutionConstants.IdempotencyRenewalPhase);
                renewalFailure.TrySetResult(problem);
                ownershipLost.Cancel();
                return;
            }
        }

        void FailRenewal(string detail)
        {
            var problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.IdempotencyOwnershipLost,
                detail,
                HttpStatusCode.Conflict);
            CommunicationDiagnostics.ReportInfrastructureFailure(
                runtime.Logger,
                problem,
                CommandExecutionConstants.IdempotencyRenewalPhase);
            renewalFailure.TrySetResult(problem);
            ownershipLost.Cancel();
        }
    }

    private static async Task StopClaimRenewalAsync(
        CancellationTokenSource renewalStop,
        Task renewalTask)
    {
        if (!renewalStop.IsCancellationRequested)
        {
            await renewalStop.CancelAsync().ConfigureAwait(false);
        }

        await renewalTask.ConfigureAwait(false);
    }

    private static Task MarkOwnedExecutionUncertainAsync(
        CommandExecutionRuntime runtime,
        CommandIdempotencyClaim claim,
        bool handlerInvoked,
        Problem problem)
    {
        return handlerInvoked
            ? TryFinalizeWithoutMaskingAsync(
                runtime,
                token => runtime.IdempotencyStore!.TryMarkIndeterminateAsync(
                    claim,
                    problem,
                    token),
                problem)
            : TryFinalizeWithoutMaskingAsync(
                runtime,
                token => runtime.IdempotencyStore!.TryReleaseAsync(claim, token),
                Problem.Create(
                    ProblemConstants.CommandExecutionTitles.IdempotencyReleaseFailure,
                    ProblemConstants.CommandExecutionMessages.ClaimReleaseFailed,
                    HttpStatusCode.InternalServerError));
    }

    private static async Task<bool> TryFinalizeAsync(
        CommandExecutionRuntime runtime,
        Func<CancellationToken, Task<bool>> finalizer)
    {
        using var finalizationSource = new CancellationTokenSource(
            runtime.OptionsSnapshot.Idempotency.FinalizationTimeout,
            runtime.TimeProvider);
        return await finalizer(finalizationSource.Token).ConfigureAwait(false);
    }

    private static async Task TryFinalizeWithoutMaskingAsync(
        CommandExecutionRuntime runtime,
        Func<CancellationToken, Task<bool>> finalizer,
        Problem problem)
    {
        try
        {
            if (!await TryFinalizeAsync(runtime, finalizer).ConfigureAwait(false))
            {
                CommunicationDiagnostics.ReportInfrastructureFailure(
                    runtime.Logger,
                    problem,
                    CommandExecutionConstants.IdempotencyFinalizationPhase);
            }
        }
        catch (Exception caught)
        {
            CommunicationDiagnostics.ReportInfrastructureFailure(
                runtime.Logger,
                problem,
                caught,
                CommandExecutionConstants.IdempotencyFinalizationPhase);
        }
    }

    private static bool TryCreateIdempotencyDescriptor<TResult>(
        ICommand command,
        IdempotencyOptions options,
        out CommandIdempotencyDescriptor? descriptor,
        out Problem? problem)
        where TResult : struct, IResult
    {
        if (options.ScopeSelector is null)
        {
            descriptor = null;
            problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.MissingIdempotencyScope,
                ProblemConstants.CommandExecutionMessages.MissingScope,
                HttpStatusCode.InternalServerError);
            return false;
        }

        if (options.FingerprintSelector is null)
        {
            descriptor = null;
            problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.MissingIdempotencyFingerprint,
                ProblemConstants.CommandExecutionMessages.MissingFingerprint,
                HttpStatusCode.InternalServerError);
            return false;
        }

        var scope = options.ScopeSelector(command);
        var fingerprint = options.FingerprintSelector(command);
        if (string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(fingerprint))
        {
            descriptor = null;
            problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.InvalidIdempotencyConfiguration,
                ProblemConstants.CommandExecutionMessages.InvalidScopeOrFingerprint,
                HttpStatusCode.InternalServerError);
            return false;
        }

        var rawKey = string.Join(
            CommandExecutionConstants.IdempotencyKeySeparator,
            scope,
            command.CommandType,
            command.CommandId.ToString(CommandExecutionConstants.CommandIdFormat));
        var storageKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
        descriptor = new CommandIdempotencyDescriptor(
            storageKey,
            command.CommandType,
            fingerprint,
            typeof(TResult).FullName ?? typeof(TResult).Name,
            options.ClaimLease,
            options.OutcomeRetention);
        problem = null;
        return true;
    }

    private static async ValueTask<TResult> ExecuteRetriesAsync<TResult>(
        ICommand command,
        Func<ICommand, CancellationToken, ValueTask<TResult>> handler,
        CommandExecutionRuntime runtime,
        Activity? activity,
        CancellationToken cancellationToken)
        where TResult : struct, IResult, IResultFactory<TResult>
    {
        var retry = runtime.OptionsSnapshot.Retry;
        var retryBudget = retry.Enabled ? ResolveRetryBudget(command, retry.MaxRetries) : 0;
        var initialRetryCount = Math.Max(0, command.Metadata?.RetryCount ?? 0);
        RetryDelayState? delayState = null;

        var attempt = 1;
        while (true)
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
                CommunicationDiagnostics.ReportAttemptFailure(runtime.Logger, command, problem, caught);
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
            if (!retry.Enabled)
            {
                return result;
            }

            var retryNumber = initialRetryCount > int.MaxValue - attempt
                ? int.MaxValue
                : initialRetryCount + attempt;
            var decisionContext = new CommandRetryDecisionContext(
                command,
                attempt,
                retryNumber,
                failure,
                exception);
            var shouldRetry = retry.ShouldRetryAsync is not null
                ? await retry.ShouldRetryAsync(decisionContext, cancellationToken).ConfigureAwait(false)
                : exception is null
                    ? retry.ShouldRetry(failure)
                    : retry.ShouldRetryException(exception);

            if (!shouldRetry || retryNumber > retryBudget || attempt == int.MaxValue)
            {
                if (shouldRetry && (retryNumber > retryBudget || attempt == int.MaxValue))
                {
                    failure.Extensions[ProblemConstants.ExtensionKeys.RetryAttempts] = attempt;
                    failure.Extensions[ProblemConstants.ExtensionKeys.RetriesExhausted] = true;
                    CommunicationTelemetry.RecordRetriesExhausted(command, attempt, failure, activity);

                    await InvokeRetryCallbackWithoutMaskingAsync(
                            runtime,
                            retry.OnRetriesExhausted,
                            new CommandRetryEvent(command, attempt, retryNumber, TimeSpan.Zero, failure, exception),
                            CommandExecutionConstants.RetriesExhaustedCallback)
                        .ConfigureAwait(false);
                }

                return result;
            }

            delayState ??= new RetryDelayState();
            var delayDecision = await CalculateDelayAsync(retry, decisionContext, delayState, cancellationToken)
                .ConfigureAwait(false);
            if (!delayDecision.ShouldRetry)
            {
                failure.Extensions[ProblemConstants.ExtensionKeys.RetryAfterExceedsMaximum] = true;
                return result;
            }

            var delay = delayDecision.Delay;
            if (command.Metadata is not null)
            {
                command.Metadata.RetryCount = retryNumber;
            }

            CommunicationTelemetry.RecordCommandRetry(command, attempt, delay, failure, activity);
            await InvokeRetryCallbackWithoutMaskingAsync(
                    runtime,
                    retry.OnRetry,
                    new CommandRetryEvent(command, attempt, retryNumber, delay, failure, exception),
                    CommandExecutionConstants.RetryCallback)
                .ConfigureAwait(false);

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, runtime.TimeProvider, cancellationToken).ConfigureAwait(false);
            }

            attempt++;
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
        var attemptTimeout = ResolveAttemptTimeout(command, runtime.OptionsSnapshot.Timeout);
        ValidateResolvedTimeout(attemptTimeout, CommandExecutionConstants.AttemptTimeoutKind);
        if (attemptTimeout == Timeout.InfiniteTimeSpan)
        {
            return await ExecuteCircuitProtectedAttemptAsync(command, handler, runtime, attempt, cancellationToken)
                .ConfigureAwait(false);
        }

        using var attemptSource = new CancellationTokenSource(attemptTimeout, runtime.TimeProvider);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, attemptSource.Token);
        try
        {
            return await ExecuteCircuitProtectedAttemptAsync(command, handler, runtime, attempt, linkedSource.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (attemptSource.IsCancellationRequested
                                                 && !cancellationToken.IsCancellationRequested)
        {
            var problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandAttemptTimedOut,
                string.Format(
                    CommandExecutionConstants.AttemptTimeoutDetailFormat,
                    attempt,
                    attemptTimeout.TotalMilliseconds),
                HttpStatusCode.RequestTimeout);
            problem.Extensions[ProblemConstants.ExtensionKeys.TimeoutMilliseconds] = attemptTimeout.TotalMilliseconds;
            problem.Extensions[ProblemConstants.ExtensionKeys.TimeoutKind] = CommandTimeoutKind.Attempt.ToString();
            problem.Extensions[ProblemConstants.ExtensionKeys.Attempt] = attempt;
            CommunicationTelemetry.RecordCommandTimeout(command, problem);
            await InvokeTimeoutCallbackWithoutMaskingAsync(
                    runtime,
                    new CommandTimeoutEvent(command, CommandTimeoutKind.Attempt, attemptTimeout, attempt, problem),
                    cancellationToken)
                .ConfigureAwait(false);
            return TResult.Fail(problem);
        }
    }

    private static async ValueTask<TResult> ExecuteCircuitProtectedAttemptAsync<TResult>(
        ICommand command,
        Func<ICommand, CancellationToken, ValueTask<TResult>> handler,
        CommandExecutionRuntime runtime,
        int attempt,
        CancellationToken cancellationToken)
        where TResult : struct, IResult, IResultFactory<TResult>
    {
        if (!runtime.OptionsSnapshot.CircuitBreaker.Enabled || runtime.CircuitBreaker is null)
        {
            return await ExecuteRateLimitedAttemptAsync(command, handler, runtime, attempt, cancellationToken)
                .ConfigureAwait(false);
        }

        var lease = await runtime.CircuitBreaker.AcquireAsync(command, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAllowed)
        {
            var problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandCircuitIsOpen,
                string.Format(CommandExecutionConstants.CircuitOpenDetailFormat, lease.PartitionKey, lease.State),
                HttpStatusCode.ServiceUnavailable);
            problem.Extensions[ProblemConstants.ExtensionKeys.CircuitState] = lease.State.ToString();
            problem.Extensions[ProblemConstants.ExtensionKeys.CircuitPartition] = lease.PartitionKey;
            if (lease.RetryAfter > TimeSpan.Zero)
            {
                problem.Extensions[ProblemConstants.ExtensionKeys.RetryAfter] = lease.RetryAfter;
            }

            CommunicationTelemetry.RecordCircuitRejected(command, lease);
            return TResult.Fail(problem);
        }

        try
        {
            var result = await ExecuteRateLimitedAttemptAsync(command, handler, runtime, attempt, cancellationToken)
                .ConfigureAwait(false);
            await runtime.CircuitBreaker.RecordAsync(command, lease, result, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException caught)
        {
            var cancelled = TResult.Fail(Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandAttemptCancelled,
                caught.Message,
                HttpStatusCode.RequestTimeout));
            await runtime.CircuitBreaker.RecordAsync(
                    command,
                    lease,
                    cancelled,
                    caught,
                    CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }
        catch (Exception caught)
        {
            var failed = TResult.Fail(Problem.Create(caught, HttpStatusCode.InternalServerError));
            await runtime.CircuitBreaker.RecordAsync(
                    command,
                    lease,
                    failed,
                    caught,
                    CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }
    }

    private static async ValueTask<TResult> ExecuteRateLimitedAttemptAsync<TResult>(
        ICommand command,
        Func<ICommand, CancellationToken, ValueTask<TResult>> handler,
        CommandExecutionRuntime runtime,
        int attempt,
        CancellationToken cancellationToken)
        where TResult : struct, IResult, IResultFactory<TResult>
    {
        if (!runtime.OptionsSnapshot.RateLimiter.Enabled || runtime.RateLimiter is null)
        {
            return await handler(command, cancellationToken).ConfigureAwait(false);
        }

        var queuedAt = runtime.TimeProvider.GetTimestamp();
        CommunicationTelemetry.RecordActiveRateLimitQueue(command, 1);
        CommandRateLimitLease lease;
        try
        {
            lease = await runtime.RateLimiter.AcquireAsync(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CommunicationTelemetry.RecordActiveRateLimitQueue(command, -1);
        }
        var queueDuration = runtime.TimeProvider.GetElapsedTime(queuedAt);
        try
        {
            if (lease.WasQueued)
            {
                CommunicationTelemetry.RecordRateLimitQueued(command, queueDuration);
                await InvokeRateLimitCallbackWithoutMaskingAsync(
                        runtime,
                        runtime.OptionsSnapshot.RateLimiter.OnQueued,
                        new CommandRateLimitEvent(command, queueDuration, lease.Problem),
                        CommandExecutionConstants.RateLimitQueuedCallback)
                    .ConfigureAwait(false);
            }

            if (!lease.IsAcquired)
            {
                var problem = lease.Problem ?? Problem.Create(
                    ProblemConstants.CommandExecutionTitles.CommandRateLimitExceeded,
                    ProblemConstants.CommandExecutionMessages.RateLimitExceeded,
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

                problem.Extensions[ProblemConstants.ExtensionKeys.Attempt] = attempt;
                CommunicationTelemetry.RecordRateLimitRejected(command, problem);

                await InvokeRateLimitCallbackWithoutMaskingAsync(
                        runtime,
                        runtime.OptionsSnapshot.RateLimiter.OnRejected,
                        new CommandRateLimitEvent(command, queueDuration, problem),
                        CommandExecutionConstants.RateLimitRejectedCallback)
                    .ConfigureAwait(false);

                return TResult.Fail(problem);
            }

            return await handler(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                await lease.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception caught)
            {
                var problem = Problem.Create(
                    ProblemConstants.CommandExecutionTitles.CommandRateLimitCleanupFailure,
                    ProblemConstants.CommandExecutionMessages.RateLimitCleanupFailed,
                    HttpStatusCode.InternalServerError);
                CommunicationDiagnostics.ReportInfrastructureFailure(
                    runtime.Logger,
                    problem,
                    caught,
                    CommandExecutionConstants.RateLimitCleanupPhase);
            }
        }
    }

    private static async Task InvokeRateLimitCallbackWithoutMaskingAsync(
        CommandExecutionRuntime runtime,
        Func<CommandRateLimitEvent, CancellationToken, ValueTask>? callback,
        CommandRateLimitEvent rateLimitEvent,
        string callbackName)
    {
        if (callback is null)
        {
            return;
        }

        try
        {
            await callback(rateLimitEvent, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception caught)
        {
            var problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandRateLimitCallbackFailure,
                string.Format(CommandExecutionConstants.CallbackOutcomeDetailFormat, callbackName),
                HttpStatusCode.InternalServerError);
            CommunicationDiagnostics.ReportInfrastructureFailure(
                runtime.Logger,
                problem,
                caught,
                CommandExecutionConstants.RateLimitCallbackPhase);
        }
    }

    private static CancellationTokenSource? CreateTimeoutSource(TimeSpan timeout, TimeProvider timeProvider)
    {
        return timeout == Timeout.InfiniteTimeSpan
            ? null
            : new CancellationTokenSource(timeout, timeProvider);
    }

    private static void ValidateResolvedTimeout(TimeSpan timeout, string timeoutKind)
    {
        if (timeout != Timeout.InfiniteTimeSpan && timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                timeoutKind,
                timeout,
                string.Format(CommandExecutionConstants.InvalidGeneratedTimeoutDetailFormat, timeoutKind));
        }
    }

    private static TimeSpan ResolveTotalTimeout(ICommand command, TimeoutOptions timeoutOptions)
    {
        if (!timeoutOptions.Enabled)
        {
            return Timeout.InfiniteTimeSpan;
        }

        if (timeoutOptions.TotalTimeoutGenerator?.Invoke(command) is { } generated)
        {
            return generated;
        }

        var configured = timeoutOptions.TotalTimeout ?? Timeout.InfiniteTimeSpan;
        if (command.Metadata?.TimeoutSeconds is not > 0)
        {
            return configured;
        }

        var commandTimeout = TimeSpan.FromSeconds(command.Metadata.TimeoutSeconds);
        return configured == Timeout.InfiniteTimeSpan || commandTimeout < configured
            ? commandTimeout
            : configured;
    }

    private static TimeSpan ResolveAttemptTimeout(ICommand command, TimeoutOptions timeoutOptions)
    {
        if (!timeoutOptions.Enabled)
        {
            return Timeout.InfiniteTimeSpan;
        }

        return timeoutOptions.AttemptTimeoutGenerator?.Invoke(command)
               ?? timeoutOptions.AttemptTimeout
               ?? Timeout.InfiniteTimeSpan;
    }

    private static async Task InvokeTimeoutCallbackWithoutMaskingAsync(
        CommandExecutionRuntime runtime,
        CommandTimeoutEvent timeoutEvent,
        CancellationToken cancellationToken)
    {
        if (runtime.OptionsSnapshot.Timeout.OnTimeout is null)
        {
            return;
        }

        try
        {
            await runtime.OptionsSnapshot.Timeout.OnTimeout(timeoutEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception caught)
        {
            var callbackProblem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandTimeoutCallbackFailure,
                ProblemConstants.CommandExecutionMessages.TimeoutCallbackFailed,
                HttpStatusCode.InternalServerError);
            CommunicationDiagnostics.ReportInfrastructureFailure(
                runtime.Logger,
                callbackProblem,
                caught,
                CommandExecutionConstants.TimeoutCallbackPhase);
        }
    }

    private static int ResolveRetryBudget(ICommand command, int configuredMaxRetries)
    {
        return command.Metadata is null
            ? configuredMaxRetries
            : Math.Max(0, Math.Min(configuredMaxRetries, command.Metadata.MaxRetries));
    }

    private static async Task InvokeRetryCallbackWithoutMaskingAsync(
        CommandExecutionRuntime runtime,
        Func<CommandRetryEvent, CancellationToken, ValueTask>? callback,
        CommandRetryEvent retryEvent,
        string callbackName)
    {
        if (callback is null)
        {
            return;
        }

        try
        {
            await callback(retryEvent, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception caught)
        {
            var callbackProblem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandRetryCallbackFailure,
                string.Format(CommandExecutionConstants.CallbackDecisionDetailFormat, callbackName),
                HttpStatusCode.InternalServerError);
            CommunicationDiagnostics.ReportInfrastructureFailure(
                runtime.Logger,
                callbackProblem,
                caught,
                CommandExecutionConstants.RetryCallbackPhase);
        }
    }

    private static async ValueTask<RetryDelayDecision> CalculateDelayAsync(
        RetryOptions options,
        CommandRetryDecisionContext context,
        RetryDelayState state,
        CancellationToken cancellationToken)
    {
        if (options.DelayGenerator is not null)
        {
            var generated = await options.DelayGenerator(context, cancellationToken).ConfigureAwait(false);
            if (generated is { } generatedDelay && generatedDelay >= TimeSpan.Zero)
            {
                return AuthoritativeDelay(generatedDelay, options.MaxRetryAfter);
            }
        }

        if (TryReadRetryAfter(context.Problem, out var retryAfter))
        {
            return AuthoritativeDelay(retryAfter, options.MaxRetryAfter);
        }

        var attempt = Math.Max(0, context.Attempt - 1);
        var delay = BuiltInDelay(options, attempt, state);
        return new RetryDelayDecision(true, delay > options.MaxDelay ? options.MaxDelay : delay);
    }

    private static bool TryReadRetryAfter(Problem problem, out TimeSpan delay)
    {
        if (problem.TryGetExtension<TimeSpan>(ProblemConstants.ExtensionKeys.RetryAfter, out var timeSpan)
            && timeSpan >= TimeSpan.Zero)
        {
            delay = timeSpan;
            return true;
        }

        if (problem.TryGetExtension<double>(ProblemConstants.ExtensionKeys.RetryAfter, out var seconds)
            && double.IsFinite(seconds)
            && seconds >= 0D)
        {
            try
            {
                delay = TimeSpan.FromSeconds(seconds);
                return true;
            }
            catch (OverflowException)
            {
                // Invalid hints fall through to generated backoff.
            }
        }

        delay = default;
        return false;
    }

    private static RetryDelayDecision AuthoritativeDelay(TimeSpan delay, TimeSpan maximum)
    {
        return delay > maximum
            ? new RetryDelayDecision(false, TimeSpan.Zero)
            : new RetryDelayDecision(true, delay);
    }

    private static TimeSpan BuiltInDelay(RetryOptions options, int attempt, RetryDelayState state)
    {
        if (options.Delay == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        try
        {
            if (options.UseJitter && options.BackoffType == RetryBackoffType.Exponential)
            {
                return DecorrelatedJitterBackoffV2(attempt, options.Delay, state, options.Randomizer);
            }

            var multiplier = options.BackoffType switch
            {
                RetryBackoffType.Constant => 1D,
                RetryBackoffType.Linear => attempt + 1D,
                RetryBackoffType.Exponential => Math.Pow(2D, attempt),
                _ => 1D
            };
            var delay = TimeSpan.FromMilliseconds(options.Delay.TotalMilliseconds * multiplier);
            return options.UseJitter ? ApplyJitter(delay, options.Randomizer) : delay;
        }
        catch (OverflowException)
        {
            return TimeSpan.MaxValue;
        }
    }

    private static TimeSpan ApplyJitter(TimeSpan delay, Func<double> randomizer)
    {
        var sample = NormalizeRandom(randomizer());
        var offset = delay.TotalMilliseconds * 0.25D;
        return TimeSpan.FromMilliseconds(delay.TotalMilliseconds + delay.TotalMilliseconds * 0.5D * sample - offset);
    }

    private static TimeSpan DecorrelatedJitterBackoffV2(
        int attempt,
        TimeSpan baseDelay,
        RetryDelayState state,
        Func<double> randomizer)
    {
        const double pFactor = 4D;
        const double scalingFactor = 1D / 1.4D;
        var t = attempt + NormalizeRandom(randomizer());
        var next = Math.Pow(2D, t) * Math.Tanh(Math.Sqrt(pFactor * t));
        if (double.IsInfinity(next))
        {
            state.Previous = next;
            return TimeSpan.MaxValue;
        }

        var intrinsic = next - state.Previous;
        state.Previous = next;
        var ticks = intrinsic * scalingFactor * baseDelay.Ticks;
        if (!double.IsFinite(ticks) || ticks >= TimeSpan.MaxValue.Ticks)
        {
            return TimeSpan.MaxValue;
        }

        return TimeSpan.FromTicks(Math.Max(0L, (long)ticks));
    }

    private static double NormalizeRandom(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 0D, 1D) : 0.5D;

    private sealed class RetryDelayState
    {
        public double Previous { get; set; }
    }

    private readonly record struct RetryDelayDecision(bool ShouldRetry, TimeSpan Delay);
}

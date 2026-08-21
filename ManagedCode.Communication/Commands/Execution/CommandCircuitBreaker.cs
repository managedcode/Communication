using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Telemetry;

namespace ManagedCode.Communication.Commands.Execution;

/// <summary>State of one partition in the command circuit breaker.</summary>
public enum CommandCircuitState
{
    /// <summary>Executions are admitted and sampled.</summary>
    Closed,

    /// <summary>Executions fail fast until the break duration expires.</summary>
    Open,

    /// <summary>One probe execution is admitted to decide whether the dependency recovered.</summary>
    HalfOpen,

    /// <summary>The partition was manually isolated and fails fast until reset.</summary>
    Isolated
}

/// <summary>One admission decision returned by a command circuit breaker.</summary>
public sealed record CommandCircuitBreakerLease(
    string PartitionKey,
    bool IsAllowed,
    bool IsProbe,
    CommandCircuitState State,
    TimeSpan RetryAfter);

/// <summary>State transition callback context.</summary>
public sealed record CommandCircuitBreakerEvent(
    string PartitionKey,
    CommandCircuitState PreviousState,
    CommandCircuitState State,
    TimeSpan BreakDuration,
    Problem? Problem);

/// <summary>Stateful circuit-breaker capability used by native command execution.</summary>
public interface ICommandCircuitBreaker
{
    /// <summary>Attempts to admit one physical command attempt.</summary>
    ValueTask<CommandCircuitBreakerLease> AcquireAsync(
        ICommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Records the admitted attempt outcome.</summary>
    ValueTask RecordAsync(
        ICommand command,
        CommandCircuitBreakerLease lease,
        IResult outcome,
        Exception? exception = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Manual inspection and isolation API for command circuit-breaker partitions.</summary>
public interface ICommandCircuitBreakerStateProvider
{
    /// <summary>Returns the current state, treating an unknown partition as closed.</summary>
    CommandCircuitState GetState(string partitionKey);

    /// <summary>Manually isolates a partition.</summary>
    ValueTask IsolateAsync(string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>Resets a partition to closed and clears its samples.</summary>
    ValueTask ResetAsync(string partitionKey, CancellationToken cancellationToken = default);
}

/// <summary>Partitioned, time-provider-aware circuit breaker for command attempts.</summary>
public sealed class PartitionedCommandCircuitBreaker : ICommandCircuitBreaker, ICommandCircuitBreakerStateProvider
{
    private const int SamplingBucketCount = 10;
    private readonly ConcurrentDictionary<string, PartitionState> _partitions = new(StringComparer.Ordinal);
    private readonly CircuitBreakerOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates a circuit breaker from a validated immutable option snapshot.</summary>
    public PartitionedCommandCircuitBreaker(CircuitBreakerOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = CircuitBreakerOptions.CreateSnapshot(options);
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<CommandCircuitBreakerLease> AcquireAsync(
        ICommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var partitionKey = _options.PartitionKeySelector(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        var partition = _partitions.GetOrAdd(partitionKey, static _ => new PartitionState());
        CommandCircuitBreakerEvent? transition = null;
        CommandCircuitBreakerLease lease;
        var now = _timeProvider.GetTimestamp();

        lock (partition.Sync)
        {
            switch (partition.State)
            {
                case CommandCircuitState.Isolated:
                    lease = new CommandCircuitBreakerLease(
                        partitionKey,
                        false,
                        false,
                        CommandCircuitState.Isolated,
                        _options.BreakDuration);
                    break;

                case CommandCircuitState.Open:
                    {
                        var elapsed = _timeProvider.GetElapsedTime(partition.OpenedAtTimestamp, now);
                        if (elapsed < _options.BreakDuration)
                        {
                            lease = new CommandCircuitBreakerLease(
                                partitionKey,
                                false,
                                false,
                                CommandCircuitState.Open,
                                _options.BreakDuration - elapsed);
                            break;
                        }

                        var previous = partition.State;
                        partition.State = CommandCircuitState.HalfOpen;
                        partition.ProbeInProgress = true;
                        transition = CreateEvent(partitionKey, previous, partition.State, null);
                        lease = new CommandCircuitBreakerLease(
                            partitionKey,
                            true,
                            true,
                            CommandCircuitState.HalfOpen,
                            TimeSpan.Zero);
                        break;
                    }

                case CommandCircuitState.HalfOpen when partition.ProbeInProgress:
                    lease = new CommandCircuitBreakerLease(
                        partitionKey,
                        false,
                        false,
                        CommandCircuitState.HalfOpen,
                        _options.BreakDuration);
                    break;

                case CommandCircuitState.HalfOpen:
                    partition.ProbeInProgress = true;
                    lease = new CommandCircuitBreakerLease(
                        partitionKey,
                        true,
                        true,
                        CommandCircuitState.HalfOpen,
                        TimeSpan.Zero);
                    break;

                default:
                    lease = new CommandCircuitBreakerLease(
                        partitionKey,
                        true,
                        false,
                        CommandCircuitState.Closed,
                        TimeSpan.Zero);
                    break;
            }
        }

        if (transition is not null)
        {
            await InvokeTransitionAsync(transition, cancellationToken).ConfigureAwait(false);
        }

        return lease;
    }

    /// <inheritdoc />
    public async ValueTask RecordAsync(
        ICommand command,
        CommandCircuitBreakerLease lease,
        IResult outcome,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(outcome);
        if (!lease.IsAllowed || !_partitions.TryGetValue(lease.PartitionKey, out var partition))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var isFailure = exception is null
            ? outcome.IsFailed && _options.ShouldBreak(outcome.Problem!)
            : _options.ShouldBreakException(exception);
        var now = _timeProvider.GetTimestamp();
        CommandCircuitBreakerEvent? transition = null;

        lock (partition.Sync)
        {
            if (partition.State == CommandCircuitState.Isolated)
            {
                return;
            }

            if (exception is OperationCanceledException)
            {
                if (lease.IsProbe && partition.State == CommandCircuitState.HalfOpen)
                {
                    var previous = partition.State;
                    Open(partition, now);
                    transition = CreateEvent(lease.PartitionKey, previous, partition.State, outcome.Problem);
                }

                goto FinishRecording;
            }

            if (lease.IsProbe && partition.State == CommandCircuitState.HalfOpen)
            {
                partition.ProbeInProgress = false;
                var previous = partition.State;
                if (isFailure)
                {
                    Open(partition, now);
                }
                else
                {
                    Close(partition);
                }

                transition = CreateEvent(
                    lease.PartitionKey,
                    previous,
                    partition.State,
                    outcome.IsFailed ? outcome.Problem : null);
            }
            else if (!lease.IsProbe && partition.State == CommandCircuitState.Closed)
            {
                Prune(partition, now);
                var currentBucket = partition.CurrentBucket;
                if (currentBucket is null
                    || _timeProvider.GetElapsedTime(currentBucket.StartTimestamp, now) >= GetBucketDuration())
                {
                    currentBucket = new CircuitBucket(now);
                    partition.Buckets.Enqueue(currentBucket);
                    partition.CurrentBucket = currentBucket;
                }

                currentBucket.TotalCount++;
                partition.TotalCount++;
                if (isFailure)
                {
                    currentBucket.FailureCount++;
                    partition.FailureCount++;
                }

                if (partition.TotalCount >= _options.MinimumThroughput)
                {
                    if ((double)partition.FailureCount / partition.TotalCount >= _options.FailureRatio)
                    {
                        var previous = partition.State;
                        Open(partition, now);
                        transition = CreateEvent(
                            lease.PartitionKey,
                            previous,
                            partition.State,
                            outcome.IsFailed ? outcome.Problem : null);
                    }
                }
            }

FinishRecording:;
        }

        if (transition is not null)
        {
            await InvokeTransitionAsync(transition, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public CommandCircuitState GetState(string partitionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        if (!_partitions.TryGetValue(partitionKey, out var partition))
        {
            return CommandCircuitState.Closed;
        }

        lock (partition.Sync)
        {
            return partition.State;
        }
    }

    /// <inheritdoc />
    public async ValueTask IsolateAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        cancellationToken.ThrowIfCancellationRequested();
        var partition = _partitions.GetOrAdd(partitionKey, static _ => new PartitionState());
        CommandCircuitBreakerEvent? transition;
        lock (partition.Sync)
        {
            var previous = partition.State;
            partition.State = CommandCircuitState.Isolated;
            partition.ProbeInProgress = false;
            partition.Buckets.Clear();
            partition.CurrentBucket = null;
            partition.TotalCount = 0;
            partition.FailureCount = 0;
            transition = previous == CommandCircuitState.Isolated
                ? null
                : CreateEvent(partitionKey, previous, partition.State, null);
        }

        if (transition is not null)
        {
            await InvokeTransitionAsync(transition, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask ResetAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_partitions.TryGetValue(partitionKey, out var partition))
        {
            return;
        }

        CommandCircuitBreakerEvent? transition;
        lock (partition.Sync)
        {
            var previous = partition.State;
            Close(partition);
            transition = previous == CommandCircuitState.Closed
                ? null
                : CreateEvent(partitionKey, previous, partition.State, null);
        }

        if (transition is not null)
        {
            await InvokeTransitionAsync(transition, cancellationToken).ConfigureAwait(false);
        }
    }

    private void Prune(PartitionState partition, long now)
    {
        while (partition.Buckets.TryPeek(out var bucket)
               && _timeProvider.GetElapsedTime(bucket.StartTimestamp, now) >= _options.SamplingDuration)
        {
            var expired = partition.Buckets.Dequeue();
            partition.TotalCount -= expired.TotalCount;
            partition.FailureCount -= expired.FailureCount;
            if (ReferenceEquals(partition.CurrentBucket, expired))
            {
                partition.CurrentBucket = null;
            }
        }
    }

    private TimeSpan GetBucketDuration() =>
        TimeSpan.FromTicks(Math.Max(1, _options.SamplingDuration.Ticks / SamplingBucketCount));

    private static void Close(PartitionState partition)
    {
        partition.State = CommandCircuitState.Closed;
        partition.ProbeInProgress = false;
        partition.Buckets.Clear();
        partition.CurrentBucket = null;
        partition.TotalCount = 0;
        partition.FailureCount = 0;
        partition.OpenedAtTimestamp = 0;
    }

    private static void Open(PartitionState partition, long now)
    {
        partition.State = CommandCircuitState.Open;
        partition.ProbeInProgress = false;
        partition.Buckets.Clear();
        partition.CurrentBucket = null;
        partition.TotalCount = 0;
        partition.FailureCount = 0;
        partition.OpenedAtTimestamp = now;
    }

    private CommandCircuitBreakerEvent CreateEvent(
        string partitionKey,
        CommandCircuitState previous,
        CommandCircuitState current,
        Problem? problem) =>
        new(partitionKey, previous, current, _options.BreakDuration, problem);

    private async ValueTask InvokeTransitionAsync(
        CommandCircuitBreakerEvent transition,
        CancellationToken cancellationToken)
    {
        CommunicationTelemetry.RecordCircuitTransition(transition);
        var callback = transition.State switch
        {
            CommandCircuitState.Open or CommandCircuitState.Isolated when _options.OnOpened is not null =>
                _options.OnOpened,
            CommandCircuitState.HalfOpen when _options.OnHalfOpened is not null =>
                _options.OnHalfOpened,
            CommandCircuitState.Closed when _options.OnClosed is not null =>
                _options.OnClosed,
            _ => null
        };

        if (callback is null)
        {
            return;
        }

        try
        {
            await callback(transition, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception caught)
        {
            var problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandCircuitBreakerCallbackFailure,
                ProblemConstants.CommandExecutionMessages.CircuitCallbackFailed,
                HttpStatusCode.InternalServerError);
            CommunicationDiagnostics.ReportInfrastructureFailure(
                null,
                problem,
                caught,
                CommandExecutionConstants.CircuitCallbackPhase);
        }
    }

    private sealed class PartitionState
    {
        public object Sync { get; } = new();
        public Queue<CircuitBucket> Buckets { get; } = new();
        public CircuitBucket? CurrentBucket { get; set; }
        public long TotalCount { get; set; }
        public long FailureCount { get; set; }
        public CommandCircuitState State { get; set; }
        public bool ProbeInProgress { get; set; }
        public long OpenedAtTimestamp { get; set; }
    }

    private sealed class CircuitBucket(long startTimestamp)
    {
        public long StartTimestamp { get; } = startTimestamp;
        public long TotalCount { get; set; }
        public long FailureCount { get; set; }
    }
}

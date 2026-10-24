using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Telemetry;

public sealed partial class TryFailureTelemetryTests
{
    private const int CommandCallerLogger = 3;

    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task CommandExecution_DefaultLogger_ShouldObserveFactoryConfiguredAfterRuntimeConstruction(
        bool useValueTask, bool sampleTraces)
    {
        var previousActivity = Activity.Current;
        Activity.Current = null;
        try
        {
            CommunicationLogger.Configure(NullLoggerFactory.Instance);
            var runtime = new CommandExecutionRuntime();
            using var observation = new Observation(sampleTraces);
            var original = new InvalidOperationException(FailureDetail);
            Result<int> result;
            if (useValueTask)
            {
                ValueTask<Result<int>> pending = CommandExecutor.ExecuteAsync(Command.Create(CommandExecutionTestConstants.DependencyCall),
                    (_, _) => ThrowValueTaskAsync(original), runtime);
                result = await pending;
            }
            else
            {
                Task<Result<int>> pending = CommandExecutor.ExecuteAsync(Command.Create(CommandExecutionTestConstants.DependencyCall),
                    (_, _) => ThrowTaskAsync(original), runtime);
                result = await pending;
            }

            result.IsFailed.ShouldBeTrue();
            result.Problem.StatusCode.ShouldBe((int)System.Net.HttpStatusCode.InternalServerError);
            result.Problem.Detail.ShouldBe(FailureDetail);
            result.Problem.Extensions.Values.OfType<Exception>().ShouldBeEmpty();
            original.StackTrace.ShouldNotBeNullOrWhiteSpace();
            original.StackTrace.ShouldContain(nameof(Throw));
            JsonSerializer.Serialize(result).ShouldNotContain(original.StackTrace!);
            var log = observation.Logs.Single(item => item.Exception is not null);
            log.Exception.ShouldBeSameAs(original);
            log.Level.ShouldBe(LogLevel.Error);
            log.Category.ShouldBe(typeof(Result).FullName);
            if (sampleTraces)
            {
                var span = observation.Spans.ShouldHaveSingleItem();
                log.TraceId.ShouldBe(span.TraceId);
                log.SpanId.ShouldBe(span.SpanId);
                span.Events.Single(item => item.Name == ExceptionEvent).Tags
                    .Single(item => item.Key == StackTraceTag).Value!.ToString()!.ShouldContain(nameof(Throw));
            }
            else
            {
                observation.Spans.ShouldBeEmpty();
            }
            observation.FlushFailures().ShouldBe(1);
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    [Test]
    [Arguments(false, ExplicitNullLogger)]
    [Arguments(true, ExplicitNullLogger)]
    [Arguments(false, DisabledLogger)]
    [Arguments(true, DisabledLogger)]
    [Arguments(false, CommandCallerLogger)]
    [Arguments(true, CommandCallerLogger)]
    public async Task CommandExecution_ExplicitLogger_ShouldPreserveCallerAndOptOutPolicy(bool useValueTask, int loggerSelection)
    {
        using var observation = new Observation();
        ILogger logger = loggerSelection switch
        {
            ExplicitNullLogger => NullLogger.Instance,
            DisabledLogger => observation.DisabledLogger,
            CommandCallerLogger => observation.Logger,
            _ => throw new ArgumentOutOfRangeException(nameof(loggerSelection))
        };
        var runtime = new CommandExecutionRuntime(logger: logger);
        var original = new InvalidOperationException(FailureDetail);
        var result = useValueTask
            ? await CommandExecutor.ExecuteAsync(Command.Create(CommandExecutionTestConstants.DependencyCall),
                (_, _) => ThrowValueTaskAsync(original), runtime)
            : await CommandExecutor.ExecuteAsync(Command.Create(CommandExecutionTestConstants.DependencyCall),
                (_, _) => ThrowTaskAsync(original), runtime);

        result.IsFailed.ShouldBeTrue();
        if (loggerSelection == CommandCallerLogger)
        {
            var log = observation.Logs.Single(item => item.Exception is not null);
            log.Category.ShouldBe(CallerCategory);
            log.Level.ShouldBe(LogLevel.Error);
            log.Exception.ShouldBeSameAs(original);
        }
        else
        {
            observation.Logs.Where(item => item.Exception is not null).ShouldBeEmpty();
        }
        var span = observation.Spans.ShouldHaveSingleItem();
        span.Events.Single(item => item.Name == ExceptionEvent).Tags
            .Single(item => item.Key == StackTraceTag).Value!.ToString()!.ShouldContain(nameof(Throw));
        observation.FlushFailures().ShouldBe(1);
    }

    [Test]
    public async Task CommandExecution_DefaultLogger_ShouldObserveOriginalAttemptAndCallbackWithoutChangingSuccess()
    {
        using var observation = new Observation();
        var original = new IOException(FailureDetail);
        var observerFailure = new InvalidOperationException(CommandExecutionTestConstants.ObserverFailed);
        var options = new CommandExecutionOptions();
        options.Retry.Enabled = true;
        options.Retry.MaxRetries = 1;
        options.Retry.Delay = TimeSpan.Zero;
        options.Retry.UseJitter = false;
        options.Retry.OnRetry = (_, _) =>
        {
            Throw(observerFailure);
            return ValueTask.CompletedTask;
        };
        var runtime = new CommandExecutionRuntime(options);
        var attempts = 0;
        var result = await CommandExecutor.ExecuteAsync(Command.Create(CommandExecutionTestConstants.RetryCallback),
            async (_, _) => ++attempts == 1 ? await ThrowTaskAsync(original) : Value, runtime);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Value);
        attempts.ShouldBe(2);
        var exceptions = observation.Logs.Where(item => item.Exception is not null).ToArray();
        exceptions.Length.ShouldBe(2);
        exceptions.Single(item => ReferenceEquals(item.Exception, original)).Level.ShouldBe(LogLevel.Error);
        exceptions.Single(item => ReferenceEquals(item.Exception, observerFailure)).Level.ShouldBe(LogLevel.Error);
        original.StackTrace.ShouldNotBeNullOrWhiteSpace();
        observerFailure.StackTrace.ShouldNotBeNullOrWhiteSpace();
        var span = observation.Spans.ShouldHaveSingleItem();
        span.Events.Count(item => item.Name == ExceptionEvent).ShouldBe(2);
        observation.FlushFailures().ShouldBe(1);
    }

    [Test]
    public async Task CommandExecution_DefaultLogger_ShouldPreserveCallerCancellationWithoutFailureDiagnostics()
    {
        using var observation = new Observation();
        using var cancellation = new CancellationTokenSource();
        var original = new OperationCanceledException(cancellation.Token);
        var runtime = new CommandExecutionRuntime();
        var observed = await Should.ThrowAsync<OperationCanceledException>(() =>
            CommandExecutor.ExecuteAsync(Command.Create(CommandExecutionTestConstants.DependencyCall), (_, _) =>
            {
                cancellation.Cancel();
                return Task.FromException<int>(original);
            }, runtime, cancellation.Token));

        observed.CancellationToken.ShouldBe(cancellation.Token);
        observation.Logs.ShouldBeEmpty();
        observation.Spans.ShouldHaveSingleItem().Events.Where(item => item.Name == ExceptionEvent).ShouldBeEmpty();
        observation.FlushFailures().ShouldBe(0);
    }
}

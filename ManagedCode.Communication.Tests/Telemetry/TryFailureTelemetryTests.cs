using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Results.Extensions;
using ManagedCode.Communication.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shouldly;

namespace ManagedCode.Communication.Tests.Telemetry;

[NotInParallel]
public sealed class TryFailureTelemetryTests
{
    private const string OperationName = "try-failure-operation";
    private const string CallerCategory = "try-failure-caller";
    private const string FailureDetail = "original failure detail";
    private const string BusinessTitle = "Access denied";
    private const string BusinessDetail = "The requested Company cannot be accessed.";
    private const string BusinessCode = "company.access.denied";
    private const string BusinessInstance = "company-test-instance";
    private const string BusinessExtension = "owner";
    private const string BusinessOwner = "company-test-owner";
    private const string ExceptionEvent = "exception";
    private const string StackTraceTag = "exception.stacktrace";
    private const string SensitiveHttpDetail = "secret-http-payload";
    private const int Value = 42;

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TryAsync_WithExplicitLogger_ShouldExportOriginalExceptionOnce(bool useValueTask)
    {
        using var observation = new Observation();
        using (var activity = CommunicationTelemetry.StartActivity(OperationName))
        {
            activity.ShouldNotBeNull();
            var exception = new InvalidOperationException(FailureDetail);
            Result<int> result;
            if (useValueTask)
            {
                ValueTask<Result<int>> pending = Result.TryAsync((Func<ValueTask<int>>)(() => ThrowValueTaskAsync(exception)), observation.Logger);
                result = await pending;
            }
            else
            {
                Task<Result<int>> pending = Result.TryAsync((Func<Task<int>>)(() => ThrowTaskAsync(exception)), observation.Logger);
                result = await pending;
            }

            result.IsFailed.ShouldBeTrue();
            result.Problem.Detail.ShouldBe(FailureDetail);
            result.Problem.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
            Result.Fail(result.Problem).IsFailed.ShouldBeTrue();
            CollectionResult<int>.Fail(result.Problem).IsFailed.ShouldBeTrue();
            AssertException(observation, activity, exception);
            observation.Logs.Single().Category.ShouldBe(CallerCategory);
            JsonSerializer.Serialize(result).ShouldNotContain(exception.StackTrace!);
            result.Problem.Extensions.Values.OfType<Exception>().ShouldBeEmpty();
        }

        observation.Spans.ShouldHaveSingleItem().Events.Count(item => item.Name == ExceptionEvent).ShouldBe(1);
        observation.FlushFailures().ShouldBe(1);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TryAsync_WithoutPayload_ShouldUseExplicitLogger(bool useValueTask)
    {
        using var observation = new Observation();
        using var activity = CommunicationTelemetry.StartActivity(OperationName);
        activity.ShouldNotBeNull();
        var exception = new InvalidOperationException(FailureDetail);
        var result = useValueTask
            ? await Result.TryAsync((Func<ValueTask>)(() => ThrowVoidValueTaskAsync(exception)), observation.Logger, HttpStatusCode.BadRequest)
            : await Result.TryAsync((Func<Task>)(() => ThrowVoidTaskAsync(exception)), observation.Logger, HttpStatusCode.BadRequest);

        result.IsFailed.ShouldBeTrue();
        result.Problem.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        AssertException(observation, activity, exception);
        observation.FlushFailures().ShouldBe(1);
    }

    [Test]
    public void Try_WithExplicitLogger_ShouldPreserveSuccessAndOriginalExceptions()
    {
        using var observation = new Observation();
        using var activity = CommunicationTelemetry.StartActivity(OperationName);
        activity.ShouldNotBeNull();
        var invoked = false;
        Result.Try(() => { invoked = true; }, observation.Logger).IsSuccess.ShouldBeTrue();
        invoked.ShouldBeTrue();
        Result.Try(() => Value, observation.Logger).Value.ShouldBe(Value);
        observation.Logs.ShouldBeEmpty();
        var exception = new InvalidOperationException(FailureDetail);
        var result = Result.Try((Action)(() => Throw(exception)), observation.Logger);
        result.IsFailed.ShouldBeTrue();
        AssertException(observation, activity, exception);
        observation.FlushFailures().ShouldBe(1);
    }

    [Test]
    public async Task TryAsync_WithNullSuccess_ShouldRetainNonNullSuccessInvariant()
    {
        using var observation = new Observation();
        var result = await Result.TryAsync((Func<Task<string?>>)(() => Task.FromResult<string?>(null)), observation.Logger);
        result.IsFailed.ShouldBeTrue();
        observation.Logs.ShouldHaveSingleItem().Exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Test]
    public async Task TryAsync_WithThrownBusinessProblem_ShouldPreserveFieldsAndEnrichExistingFailure()
    {
        using var observation = new Observation();
        using var activity = CommunicationTelemetry.StartActivity(OperationName);
        activity.ShouldNotBeNull();
        var problem = Problem.Create(BusinessTitle, BusinessDetail, HttpStatusCode.Forbidden);
        problem.ErrorCode = BusinessCode;
        problem.Instance = BusinessInstance;
        problem.Extensions[BusinessExtension] = BusinessOwner;
        Result.Fail(problem).IsFailed.ShouldBeTrue();
        observation.Logs.ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
        var exception = new ProblemException(problem);
        var result = await Result.TryAsync((Func<Task<int>>)(() => ThrowTaskAsync(exception)), observation.Logger);
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldBeSameAs(problem);
        result.Problem.StatusCode.ShouldBe((int)HttpStatusCode.Forbidden);
        result.Problem.ErrorCode.ShouldBe(BusinessCode);
        result.Problem.Title.ShouldBe(BusinessTitle);
        result.Problem.Detail.ShouldBe(BusinessDetail);
        result.Problem.Instance.ShouldBe(BusinessInstance);
        result.Problem.Extensions[BusinessExtension].ShouldBe(BusinessOwner);
        Problem.Create(exception).ShouldBeSameAs(problem);
        Problem.Create(exception, HttpStatusCode.InternalServerError).ShouldBeSameAs(problem);
        Result<int>.Fail(exception, observation.Logger).Problem.ShouldBeSameAs(problem);
        CollectionResult<int>.Fail(exception, observation.Logger).Problem.ShouldBeSameAs(problem);
        observation.Logs.Count.ShouldBe(2);
        var error = observation.Logs.Last();
        error.Level.ShouldBe(LogLevel.Error);
        error.Exception.ShouldBeSameAs(exception);
        activity.Events.Count(item => item.Name == ExceptionEvent).ShouldBe(1);
        activity.Events.Count(item => item.Name == CommunicationTelemetry.CreatedFailureEventName).ShouldBe(1);
        observation.FlushFailures().ShouldBe(1);
    }

    [Test]
    public async Task TryAsync_InsideHttpPrivacyBoundary_ShouldKeepSafeStacksAndOmitMessages()
    {
        using var observation = new Observation();
        using var activity = CommunicationTelemetry.StartActivity(OperationName);
        activity.ShouldNotBeNull();
        using var privacy = HttpFailureDiagnostics.Begin(true);
        var exception = new InvalidOperationException(SensitiveHttpDetail);
        var result = await Result.TryAsync((Func<Task<int>>)(() => ThrowTaskAsync(exception)), observation.Logger);
        result.IsFailed.ShouldBeTrue();
        var log = observation.Logs.ShouldHaveSingleItem();
        log.Exception.ShouldBeNull();
        log.Message.ShouldNotContain(SensitiveHttpDetail);
        var trace = activity.Events.Single(item => item.Name == ExceptionEvent);
        trace.Tags.Single(item => item.Key == StackTraceTag).Value!.ToString()!.ShouldContain(nameof(Throw));
        foreach (var tag in activity.Events.SelectMany(item => item.Tags))
        {
            tag.Value?.ToString()?.ShouldNotContain(SensitiveHttpDetail);
        }
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task From_CompletedFailures_ShouldExportTheAwaitedOriginalCause(bool useValueTask, bool canceled)
    {
        using var observation = new Observation();
        using var activity = CommunicationTelemetry.StartActivity(OperationName);
        activity.ShouldNotBeNull();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Exception exception = canceled
            ? new OperationCanceledException(FailureDetail, cancellation.Token)
            : new InvalidOperationException(FailureDetail);
        var task = ThrowVoidTaskAsync(exception);
        try
        {
            await task;
        }
        catch (Exception)
        {
            // Complete the operation before crossing the conversion boundary under test.
        }

        var result = useValueTask ? await Result.From(new ValueTask(task)) : await Result.From(task);
        result.IsFailed.ShouldBeTrue();
        result.Problem.Detail.ShouldBe(FailureDetail);
        AssertException(observation, activity, exception);
        observation.FlushFailures().ShouldBe(1);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task From_ValueTaskSource_ShouldConsumeCompletedOperationExactlyOnce(bool faulted)
    {
        using var observation = new Observation();
        var source = new CompletedSource(faulted);
        var result = await Result.From(new ValueTask(source, 0));
        source.Consumptions.ShouldBe(1);
        result.IsSuccess.ShouldBe(!faulted);
        if (result.IsFailed)
        {
            observation.Logs.ShouldHaveSingleItem().Exception.ShouldBeSameAs(source.Exception);
        }
    }

    [Test]
    public async Task From_Delegate_ShouldInvokeInCallerSchedulerAndReturnTerminalValue()
    {
        using var observation = new Observation();
        var pair = new ConcurrentExclusiveSchedulerPair();
        var scheduler = pair.ExclusiveScheduler;
        try
        {
            await Task.Factory.StartNew(async () =>
            {
                var invoked = false;
                var pending = Result.From((Func<Task>)(() =>
                {
                    TaskScheduler.Current.ShouldBeSameAs(scheduler);
                    invoked = true;
                    return Task.CompletedTask;
                }));
                invoked.ShouldBeTrue();
                (await pending).IsSuccess.ShouldBeTrue();
                var value = await Result<int>.From((Func<Task<int>>)(() =>
                {
                    TaskScheduler.Current.ShouldBeSameAs(scheduler);
                    return Task.FromResult(Value);
                }));
                value.IsSuccess.ShouldBeTrue();
                value.Value.ShouldBe(Value);
                var existing = Result<int>.Succeed(Value);
                var preserved = await Result<int>.From((Func<Task<Result<int>>>)(() =>
                {
                    TaskScheduler.Current.ShouldBeSameAs(scheduler);
                    return Task.FromResult(existing);
                }));
                preserved.ShouldBe(existing);
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler).Unwrap();
        }
        finally
        {
            pair.Complete();
            await pair.Completion;
        }

        observation.Logs.ShouldBeEmpty();
    }

    [Test]
    public async Task From_PreCanceledDelegate_ShouldNotInvokeAndShouldReportCancellation()
    {
        using var observation = new Observation();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var invoked = false;
        var result = await Result.From((Func<Task>)(() =>
        {
            invoked = true;
            return Task.CompletedTask;
        }), cancellation.Token);
        invoked.ShouldBeFalse();
        result.IsFailed.ShouldBeTrue();
        observation.Logs.ShouldHaveSingleItem().Exception.ShouldBeOfType<OperationCanceledException>()
            .CancellationToken.ShouldBe(cancellation.Token);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TryAsync_WithProblemFactory_ShouldMapOnceAndExportOriginalStack(bool useValueTask)
    {
        using var observation = new Observation();
        using var activity = CommunicationTelemetry.StartActivity(OperationName);
        activity.ShouldNotBeNull();
        var original = new InvalidOperationException(FailureDetail);
        var problem = Problem.Create(BusinessTitle, BusinessDetail, HttpStatusCode.Forbidden);
        problem.ErrorCode = BusinessCode;
        var mappings = 0;
        Problem Map(Exception exception)
        {
            exception.ShouldBeSameAs(original);
            mappings++;
            return problem;
        }

        var result = useValueTask
            ? await Result.TryAsync((Func<ValueTask<int>>)(() => ThrowValueTaskAsync(original)), observation.Logger, problemFactory: Map)
            : await Result.TryAsync((Func<Task<int>>)(() => ThrowTaskAsync(original)), observation.Logger, problemFactory: Map);
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldBeSameAs(problem);
        result.Problem.ErrorCode.ShouldBe(BusinessCode);
        result.Problem.StatusCode.ShouldBe((int)HttpStatusCode.Forbidden);
        mappings.ShouldBe(1);
        AssertException(observation, activity, original);
        observation.FlushFailures().ShouldBe(1);
    }

    [Test]
    public void Try_WithInvalidProblemFactory_ShouldPropagateItsFailure()
    {
        using var observation = new Observation();
        var original = new InvalidOperationException(FailureDetail);
        var mappingFailure = new ArgumentException(BusinessDetail);
        Should.Throw<ArgumentException>(() => Result.Try((Action)(() => Throw(original)), observation.Logger,
            problemFactory: _ => throw mappingFailure)).ShouldBeSameAs(mappingFailure);
        Should.Throw<ArgumentNullException>(() => Result.Try((Action)(() => Throw(original)), observation.Logger,
            problemFactory: _ => null!));
        observation.Logs.ShouldBeEmpty();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TryAsync_Success_ShouldPreserveShapeAndInvokeNoProblemFactory(bool useValueTask)
    {
        using var observation = new Observation();
        Problem Map(Exception exception) => throw exception;
        var result = useValueTask
            ? await Result.TryAsync((Func<ValueTask<int>>)(() => ValueTask.FromResult(Value)), observation.Logger, problemFactory: Map)
            : await Result.TryAsync(async () => { await Task.Yield(); return Value; }, observation.Logger, problemFactory: Map);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Value);
        var completed = useValueTask
            ? await Result.TryAsync((Func<ValueTask>)(() => ValueTask.CompletedTask), observation.Logger, problemFactory: Map)
            : await Result.TryAsync(async () => { await Task.Yield(); }, observation.Logger, problemFactory: Map);
        completed.IsSuccess.ShouldBeTrue();
        observation.Logs.ShouldBeEmpty();
        observation.FlushFailures().ShouldBe(0);
    }

    private static void AssertException(Observation observation, Activity activity, Exception exception)
    {
        var log = observation.Logs.ShouldHaveSingleItem();
        log.Level.ShouldBe(LogLevel.Error);
        log.Exception.ShouldBeSameAs(exception);
        log.TraceId.ShouldBe(activity.TraceId);
        log.SpanId.ShouldBe(activity.SpanId);
        exception.StackTrace.ShouldNotBeNullOrWhiteSpace();
        exception.StackTrace.ShouldContain(nameof(Throw));
        var recorded = activity.Events.Single(item => item.Name == ExceptionEvent);
        recorded.Tags.Single(item => item.Key == StackTraceTag).Value!.ToString()!.ShouldContain(nameof(Throw));
        activity.Events.Count(item => item.Name == CommunicationTelemetry.CreatedFailureEventName).ShouldBe(1);
    }

    private static void Throw(Exception exception) => throw exception;

    private static async Task<int> ThrowTaskAsync(Exception exception)
    {
        await Task.Yield();
        Throw(exception);
        return Value;
    }

    private static async ValueTask<int> ThrowValueTaskAsync(Exception exception)
    {
        await Task.Yield();
        Throw(exception);
        return Value;
    }

    private static async Task ThrowVoidTaskAsync(Exception exception)
    {
        await Task.Yield();
        Throw(exception);
    }

    private static async ValueTask ThrowVoidValueTaskAsync(Exception exception)
    {
        await Task.Yield();
        Throw(exception);
    }

    private sealed class CompletedSource(bool faulted) : IValueTaskSource
    {
        internal int Consumptions { get; private set; }
        internal Exception Exception { get; } = new InvalidOperationException(FailureDetail);
        public ValueTaskSourceStatus GetStatus(short token) => faulted ? ValueTaskSourceStatus.Faulted : ValueTaskSourceStatus.Succeeded;
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => throw new NotSupportedException();
        public void GetResult(short token)
        {
            Consumptions++;
            if (faulted)
            {
                Throw(Exception);
            }
        }
    }

    private sealed class Observation : IDisposable
    {
        private readonly ILoggerFactory _factory;
        private readonly TracerProvider _tracer;
        private readonly MeterProvider _meter;
        private long _failures;

        internal Observation()
        {
            _factory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.AddProcessor(new SimpleLogRecordExportProcessor(new CaptureExporter<LogRecord>(record =>
                    Logs.Add(new LogEntry(record.LogLevel, record.Exception, record.CategoryName, record.TraceId, record.SpanId, record.FormattedMessage ?? string.Empty)))));
            }));
            CommunicationLogger.Configure(_factory);
            Logger = _factory.CreateLogger(CallerCategory);
            _tracer = Sdk.CreateTracerProviderBuilder()
                .AddSource(CommunicationTelemetry.SourceName)
                .AddProcessor(new SimpleActivityExportProcessor(new CaptureExporter<Activity>(Spans.Add)))
                .Build();
            _meter = Sdk.CreateMeterProviderBuilder()
                .AddMeter(CommunicationTelemetry.SourceName)
                .AddReader(new PeriodicExportingMetricReader(new CaptureExporter<Metric>(metric =>
                {
                    if (metric.Name == CommunicationTelemetry.CreatedFailureCounterName)
                    {
                        foreach (ref readonly var point in metric.GetMetricPoints())
                        {
                            _failures += point.GetSumLong();
                        }
                    }
                }), exportIntervalMilliseconds: int.MaxValue))
                .Build();
        }

        internal ILogger Logger { get; }
        internal List<LogEntry> Logs { get; } = [];
        internal List<Activity> Spans { get; } = [];
        internal long FlushFailures()
        {
            _meter.ForceFlush().ShouldBeTrue();
            return _failures;
        }

        public void Dispose()
        {
            CommunicationLogger.Configure(NullLoggerFactory.Instance);
            _meter.Dispose();
            _tracer.Dispose();
            _factory.Dispose();
        }
    }

    private sealed record LogEntry(LogLevel Level, Exception? Exception, string? Category, ActivityTraceId TraceId, ActivitySpanId SpanId, string Message);

    private sealed class CaptureExporter<T>(Action<T> capture) : BaseExporter<T> where T : class
    {
        public override ExportResult Export(in Batch<T> batch)
        {
            foreach (var item in batch)
            {
                capture(item);
            }
            return ExportResult.Success;
        }
    }
}

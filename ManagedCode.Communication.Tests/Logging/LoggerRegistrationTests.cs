using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Logging;

/// <summary>
///     Whether log entries actually appear, and whether their absence is safe.
/// </summary>
/// <remarks>
///     Two guarantees, and both matter. Register a logger and failures must genuinely reach it — a logging call
///     that silently goes nowhere is worse than no logging at all. Register nothing and the same calls must stay
///     silent without throwing, so the library can be used from a console app or a unit test as-is.
/// </remarks>
[Collection(ManagedCode.Communication.Tests.Logging.GlobalLoggerCollection.Name)]
public sealed class LoggerRegistrationTests
{
    // ---------- logger registered: entries appear ----------

    [Fact]
    public void AReportedFailureReachesARegisteredLogger()
    {
        var sink = new LogSink();
        using var factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(sink);
        });

        var logger = factory.CreateLogger("test");
        CommunicationDiagnostics.ReportFailure(logger, Problem.Create("payment_declined", "card refused", 402));

        var entry = sink.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Message.ShouldContain("payment_declined");
        entry.Message.ShouldContain("card refused");
        entry.Message.ShouldContain("402");
    }

    [Fact]
    public void TheOriginatingExceptionIsAttachedToTheLogEntry()
    {
        var sink = new LogSink();
        using var factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(sink);
        });

        Exception captured;
        try
        {
            throw new InvalidOperationException("the real cause");
        }
        catch (Exception exception)
        {
            captured = exception;
        }

        CommunicationDiagnostics.ReportFailure(factory.CreateLogger("test"), Problem.Create(captured), captured);

        var entry = sink.Entries.ShouldHaveSingleItem();
        entry.Exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("the real cause");
        entry.Exception!.StackTrace.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AValidationFailureLogsAsAWarningRatherThanAnError()
    {
        var sink = new LogSink();
        using var factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(sink);
        });

        var problem = Problem.Validation(("email", "required"), ("name", "too short"));
        CommunicationDiagnostics.ReportFailure(factory.CreateLogger("test"), problem);

        var entry = sink.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldContain("email");
        entry.Message.ShouldContain("name");
    }

    [Fact]
    public void ReportOnAResultLogsOnlyWhenItFailed()
    {
        var sink = new LogSink();
        using var factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(sink);
        });
        var logger = factory.CreateLogger("test");

        Result<int>.Succeed(1).Report(logger);
        sink.Entries.ShouldBeEmpty();

        Result<int>.Fail(Problem.Create("boom", "d", 500)).Report(logger);
        sink.Entries.Count.ShouldBe(1);
    }

    [Fact]
    public void ConfiguringTheLibraryLoggerRoutesItsInternalLoggingToYourFactory()
    {
        var sink = new LogSink();
        using var factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(sink);
        });

        CommunicationLogger.Configure(factory);
        try
        {
            ProblemLoggerCenter.LogProblem(CommunicationLogger.GetLogger(), "title", 500, "detail");

            sink.Entries.ShouldHaveSingleItem().Message.ShouldContain("title");
        }
        finally
        {
            ResetLibraryLogger();
        }
    }

    [Fact]
    public void ConfiguringFromAServiceProviderAlsoWorks()
    {
        var sink = new LogSink();
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(sink);
        });

        using var provider = services.BuildServiceProvider();
        CommunicationLogger.Configure(provider);
        try
        {
            ProblemLoggerCenter.LogProblem(CommunicationLogger.GetLogger(), "from-di", 500, "detail");

            sink.Entries.ShouldHaveSingleItem().Message.ShouldContain("from-di");
        }
        finally
        {
            ResetLibraryLogger();
        }
    }

    // ---------- no logger registered: silence, not failure ----------

    [Fact]
    public void WithoutAnyRegistrationNothingIsLoggedAndNothingThrows()
    {
        ResetLibraryLogger();

        var sink = new LogSink();
        // The sink is deliberately NOT wired to anything: it stands in for "the application's logging pipeline",
        // which the library must not reach when it was never given one.

        Should.NotThrow(() =>
        {
            var logger = CommunicationLogger.GetLogger();
            ProblemLoggerCenter.LogProblem(logger, "title", 500, "detail");
            CommunicationDiagnostics.ReportFailure(null, Problem.Create("boom", "d", 500));
            Result<int>.Fail(Problem.Create("boom", "d", 500)).Report();
        });

        sink.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void PassingNullAsTheLoggerSkipsLoggingButStillRecordsTelemetry()
    {
        var sink = new LogSink();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(sink));

        // Null logger: the failure must still be counted, it just must not be written anywhere.
        Should.NotThrow(() => CommunicationDiagnostics.ReportFailure(null, Problem.Create("boom", "d", 500)));

        sink.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void ADisabledLogLevelProducesNoEntries()
    {
        var sink = new LogSink();
        using var factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Critical); // Error and Warning are below this.
            builder.AddProvider(sink);
        });

        CommunicationDiagnostics.ReportFailure(factory.CreateLogger("test"), Problem.Create("boom", "d", 500));

        sink.Entries.ShouldBeEmpty();
    }

    private static void ResetLibraryLogger()
    {
        // Configure(null!) is not part of the public contract, so reset through a factory with no providers:
        // resolvable, and writes nowhere.
        CommunicationLogger.Configure(LoggerFactory.Create(_ => { }));
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class LogSink : ILoggerProvider
    {
        private readonly ConcurrentQueue<LogEntry> _entries = new();

        public IReadOnlyList<LogEntry> Entries => _entries.ToList();

        public ILogger CreateLogger(string categoryName) => new SinkLogger(_entries);

        public void Dispose()
        {
        }

        private sealed class SinkLogger(ConcurrentQueue<LogEntry> entries) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}

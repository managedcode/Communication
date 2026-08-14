using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Orleans.Extensions;
using ManagedCode.Communication.Orleans.Filters;
using ManagedCode.Communication.Tests.Common.TestApp.Grains;
using ManagedCode.Communication.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.TestingHost;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Orleans.Filters;

public sealed class OrleansCommunicationFilterLoggingTests
{
    [Fact]
    public async Task IncomingFilter_ShouldLogOriginalExceptionBeforeReturningFailedResult()
    {
        var cluster = DeployCluster<CommunicationSiloConfigurator>();

        try
        {
            var result = await cluster.Client
                .GetGrain<ITestGrain>(0)
                .TestResultIntInvalidOperationError();

            result.ShouldHaveProblem()
                .WithTitle(nameof(InvalidOperationException))
                .WithDetail("result int invalid operation error")
                .WithStatusCode((int)HttpStatusCode.BadRequest);

            var entry = SingleConvertedExceptionLog<CommunicationIncomingGrainCallFilter>();

            entry.Exception.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("result int invalid operation error");
            entry.Message.ShouldContain(((int)HttpStatusCode.BadRequest).ToString());
        }
        finally
        {
            await DisposeClusterAsync(cluster);
        }
    }

    [Fact]
    public async Task OutgoingFilter_ShouldLogOriginalExceptionBeforeReturningFailedResult()
    {
        var cluster = DeployCluster<LoggingOnlySiloConfigurator>();

        try
        {
            var result = await cluster.Client
                .GetGrain<ITestGrain>(0)
                .TestResultIntInvalidOperationError();

            result.IsFailed.ShouldBeTrue();
            var problem = result.Problem!;
            (problem.Detail ?? string.Empty).ShouldContain("result int invalid operation error");

            var entry = SingleConvertedExceptionLog<CommunicationOutgoingGrainCallFilter>();

            entry.Exception.ShouldNotBeNull();
            entry.Exception.Message.ShouldContain("result int invalid operation error");
            entry.Message.ShouldContain(problem.StatusCode.ToString());
        }
        finally
        {
            await DisposeClusterAsync(cluster);
        }
    }

    private static TestCluster DeployCluster<TSiloConfigurator>()
        where TSiloConfigurator : ISiloConfigurator, new()
    {
        CommunicationOrleansLogSink.Clear();

        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TSiloConfigurator>();
        builder.AddClientBuilderConfigurator<CommunicationClientConfigurator>();

        var cluster = builder.Build();
        cluster.Deploy();
        return cluster;
    }

    private static async Task DisposeClusterAsync(TestCluster cluster)
    {
        await cluster.StopAllSilosAsync();
        cluster.Dispose();
    }

    private static CapturedLogEntry SingleConvertedExceptionLog<TFilter>()
    {
        var matchingEntries = CommunicationOrleansLogSink.Snapshot()
            .Where(entry =>
                entry.EventId.Id == 7001 &&
                entry.Level == LogLevel.Error &&
                entry.Category.EndsWith(typeof(TFilter).Name, StringComparison.Ordinal))
            .ToArray();

        matchingEntries.Length.ShouldBe(1);

        var entry = matchingEntries[0];
        entry.Message.ShouldContain("converted to failed Communication result");
        entry.Message.ShouldContain(nameof(ITestGrain));
        entry.Message.ShouldContain(nameof(ITestGrain.TestResultIntInvalidOperationError));
        return entry;
    }

    private sealed class CommunicationSiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .ConfigureServices(static services =>
                {
                    services.AddSingleton<ILoggerProvider, CapturingLoggerProvider>();
                })
                .UseOrleansCommunication();
        }
    }

    private sealed class LoggingOnlySiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(static services =>
            {
                services.AddSingleton<ILoggerProvider, CapturingLoggerProvider>();
            });
        }
    }

    private sealed class CommunicationClientConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder
                .ConfigureServices(static services =>
                {
                    services.AddSingleton<ILoggerProvider, CapturingLoggerProvider>();
                })
                .UseOrleansCommunication();
        }
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new CapturingLogger(categoryName);
        }

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(string categoryName) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            CommunicationOrleansLogSink.Add(new CapturedLogEntry(
                categoryName,
                logLevel,
                eventId,
                exception,
                formatter(state, exception)));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private static class CommunicationOrleansLogSink
    {
        private static readonly ConcurrentQueue<CapturedLogEntry> Entries = new();

        public static void Add(CapturedLogEntry entry)
        {
            Entries.Enqueue(entry);
        }

        public static CapturedLogEntry[] Snapshot()
        {
            return Entries.ToArray();
        }

        public static void Clear()
        {
            Entries.Clear();
        }
    }

    private sealed record CapturedLogEntry(
        string Category,
        LogLevel Level,
        EventId EventId,
        Exception? Exception,
        string Message);
}

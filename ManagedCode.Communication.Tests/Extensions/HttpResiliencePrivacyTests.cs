using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Extensions.Http;
using ManagedCode.Communication.Extensions.Telemetry;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Shouldly;

namespace ManagedCode.Communication.Tests.Extensions;

[NotInParallel]
public sealed class HttpResiliencePrivacyTests
{
    private const string ClientName = "http-privacy";
    private const string TestCommandType = "http.privacy.test";
    private const string ParentOperation = "http-privacy-parent";
    private const string SecretUser = "private-user";
    private const string SecretPassword = "private-password";
    private const string SecretPath = "private-path";
    private const string SecretQuery = "private-query";
    private const string SecretBody = "private-provider-body";
    private const string SecretReason = "private-reason";
    private const string RequestUrl = "https://" + SecretUser + ":" + SecretPassword + "@example.test/" + SecretPath + "?token=" + SecretQuery;
    private const string SafeAuthority = "https://example.test";
    private const string OrdinaryDetail = "ordinary-command-detail";
    private const string ExpectedFailure = "The HTTP operation unexpectedly succeeded.";
    private const string ExceptionEvent = "exception";
    private const string ExceptionStackTrace = "exception.stacktrace";
    private const string ExceptionType = "exception.type";

    [Test]
    [Arguments(0, false)]
    [Arguments(0, true)]
    [Arguments(1, false)]
    [Arguments(1, true)]
    [Arguments(2, false)]
    [Arguments(2, true)]
    [Arguments(3, false)]
    [Arguments(3, true)]
    public async Task TransportFailures_KeepOriginalRetryInputsAndSafeDiagnosticsAsync(int kind, bool traces)
    {
        var failure = CreateFailure(kind);
        var decisions = new List<Exception>();
        var logs = new ConcurrentBag<LogSnapshot>();
        var spans = new ConcurrentBag<Activity>();
        var handler = new FailureHandler(failure);
        using var host = CreateHost(logs, spans, traces, handler, options =>
        {
            options.Execution.Retry.Enabled = true;
            options.Execution.Retry.MaxRetries = 1;
            options.Execution.Retry.Delay = TimeSpan.Zero;
            options.Execution.Retry.UseJitter = false;
            options.Execution.Retry.ShouldRetry = static _ => false;
            options.Execution.Retry.ShouldRetryException = exception =>
            {
                decisions.Add(exception);
                return true;
            };
        });
        await host.StartAsync();
        using var parent = new Activity(ParentOperation).Start();
        using var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);
        var pending = client.GetAsync(RequestUrl);
        var ordinary = ThrowOrdinaryFailure();
        Result.Fail(ordinary).IsFailed.ShouldBeTrue();
        var observed = await ObserveFailureAsync(pending);

        observed.GetBaseException().ShouldBeSameAs(failure.GetBaseException());
        if (failure is HttpRequestException)
        {
            observed.ShouldBeSameAs(failure);
        }
        handler.Requests.Count.ShouldBe(2);
        handler.Requests.All(uri => uri.AbsoluteUri == new Uri(RequestUrl).AbsoluteUri).ShouldBeTrue();
        decisions.Count.ShouldBe(2);
        decisions.All(exception => ReferenceEquals(exception, failure)).ShouldBeTrue();
        var ordinaryLog = logs.Single(log => ReferenceEquals(log.Exception, ordinary));
        ordinaryLog.Text.ShouldContain(OrdinaryDetail);
        var httpLogs = logs.Where(log => !ReferenceEquals(log, ordinaryLog)).ToArray();
        httpLogs.Any(log => log.Level == LogLevel.Error).ShouldBeTrue();
        httpLogs.All(log => log.Exception is null).ShouldBeTrue();
        httpLogs.All(log => log.TraceId == parent.TraceId).ShouldBeTrue();
        var text = string.Join(Environment.NewLine, httpLogs.Select(log => log.Text));
        text.ShouldContain(failure.GetType().FullName!);
        text.ShouldContain(nameof(FailureHandler));
        AssertPrivacy(text);
        AssertSpans(spans, traces, nameof(FailureHandler));
        await host.StopAsync();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task FailedResponses_PreserveWireDataButExcludeReasonAndUserInfoAsync(bool traces)
    {
        var logs = new ConcurrentBag<LogSnapshot>();
        var spans = new ConcurrentBag<Activity>();
        var handler = new ResponseHandler();
        using var host = CreateHost(logs, spans, traces, handler, static options => options.Execution.Retry.Enabled = false);
        await host.StartAsync();
        using var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);
        using var response = await client.GetAsync(RequestUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.ReasonPhrase.ShouldBe(SecretReason);
        (await response.Content.ReadAsStringAsync()).ShouldBe(SecretBody);
        response.RequestMessage!.RequestUri!.AbsoluteUri.ShouldBe(new Uri(RequestUrl).AbsoluteUri);
        logs.Any(log => log.Level == LogLevel.Warning).ShouldBeTrue();
        AssertPrivacy(string.Join(Environment.NewLine, logs.Select(log => log.Text)));
        AssertSpans(spans, traces);
        await host.StopAsync();
    }

    [Test]
    public async Task AutomaticHttpFailures_WithoutAnActivity_StillExportSafeLogsAsync()
    {
        var logs = new ConcurrentBag<LogSnapshot>();
        using var host = CreateHost(logs, [], false, new ResponseHandler(), static _ => { });
        await host.StartAsync();
        var previous = Activity.Current;
        try
        {
            Activity.Current = null;
            using var diagnostics = HttpFailureDiagnostics.Begin(true);
            try
            {
                throw CreateFailure(3);
            }
            catch (HttpRequestException exception)
            {
                var result = Result.Fail(exception);
                result.Problem!.Detail.ShouldBe(RequestUrl);
                var log = logs.Single();
                log.Level.ShouldBe(LogLevel.Error);
                log.Exception.ShouldBeNull();
                log.TraceId.ShouldBe(default);
                log.Text.ShouldContain(typeof(HttpRequestException).FullName!);
                log.Text.ShouldContain(nameof(AutomaticHttpFailures_WithoutAnActivity_StillExportSafeLogsAsync));
                AssertPrivacy(log.Text);
            }
        }
        finally
        {
            Activity.Current = previous;
        }
        await host.StopAsync();
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    public async Task ResultAdapters_KeepTransportProblemsButExportSafeDiagnosticsAsync(int adapter)
    {
        var logs = new ConcurrentBag<LogSnapshot>();
        var spans = new ConcurrentBag<Activity>();
        using var host = CreateHost(logs, spans, true, new FailureHandler(CreateFailure(3)),
            static options => options.Execution.Retry.Enabled = false);
        await host.StartAsync();
        using var parent = CommunicationTelemetry.StartActivity(ParentOperation);
        using var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);
        var problem = await ReadAdapterProblemAsync(client, adapter);

        problem.Detail.ShouldBe(RequestUrl);
        AssertPrivateDiagnostics(logs, parent!);
        foreach (var span in spans)
        {
            AssertPrivacy(ActivityText(span));
        }
        await host.StopAsync();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ResponseConverters_PreserveProviderContentButExportSafeDiagnosticsAsync(bool generic)
    {
        var logs = new ConcurrentBag<LogSnapshot>();
        using var host = CreateHost(logs, [], true, new ResponseHandler(), static _ => { });
        await host.StartAsync();
        using var parent = CommunicationTelemetry.StartActivity(ParentOperation);
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(SecretBody) };
        var problem = generic
            ? (await response.FromJsonToResult<string>()).Problem!
            : (await response.FromRequestToResult()).Problem!;

        problem.Detail.ShouldBe(SecretBody);
        AssertPrivateDiagnostics(logs, parent!);
        await host.StopAsync();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ProjectionFailures_PreserveCallerProblemButExportSafeDiagnosticsAsync(bool command)
    {
        var logs = new ConcurrentBag<LogSnapshot>();
        using var host = CreateHost(logs, [], true, new ResponseHandler(true), static _ => { });
        await host.StartAsync();
        using var parent = CommunicationTelemetry.StartActivity(ParentOperation);
        using var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);
        Func<HttpRequestMessage> request = static () => new HttpRequestMessage(HttpMethod.Post, RequestUrl);
        Func<HttpResponseMessage, CancellationToken, Task<string?>> projection = static (_, _) => throw new JsonException(SecretBody);
        var result = command
            ? await client.SendForResultAsync(Command.Create(TestCommandType), _ => request(), projection, new CommandExecutionRuntime())
            : await client.SendForResultAsync(request, projection);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Detail!.ShouldContain(SecretBody);
        AssertPrivateDiagnostics(logs, parent!);
        await host.StopAsync();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task HttpCqrsFailures_PreserveTerminalProblemAndConsumerLoggingAsync(bool transport)
    {
        var logs = new ConcurrentBag<LogSnapshot>();
        using var host = CreateHost(logs, [], true,
            transport ? new FailureHandler(CreateFailure(3)) : new ProblemResponseHandler(),
            static options => options.Execution.Retry.Enabled = false);
        await host.StartAsync();
        using var parent = CommunicationTelemetry.StartActivity(ParentOperation);
        using var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);
        var chunks = new List<CqrsStreamChunk<int, string>>();
        var ordinary = ThrowOrdinaryFailure();
        await foreach (var chunk in client.GetForCqrsStreamAsync<int, string>(RequestUrl))
        {
            chunks.Add(chunk);
            Result.Fail(ordinary).IsFailed.ShouldBeTrue();
        }

        var terminal = chunks.Single(chunk => chunk.Kind == CqrsStreamChunkKind.Failed);
        terminal.Problem!.Detail!.ShouldContain(transport ? RequestUrl : SecretBody);
        logs.Any(log => ReferenceEquals(log.Exception, ordinary)).ShouldBeTrue();
        var httpLogs = new ConcurrentBag<LogSnapshot>(logs.Where(log => !ReferenceEquals(log.Exception, ordinary)));
        AssertPrivateDiagnostics(httpLogs, parent!);
        await host.StopAsync();
    }

    private static async Task<Problem> ReadAdapterProblemAsync(HttpClient client, int adapter)
    {
        Func<HttpRequestMessage> request = static () => new HttpRequestMessage(HttpMethod.Post, RequestUrl);
        Func<HttpResponseMessage, CancellationToken, Task<string?>> projection = static (_, _) => Task.FromResult<string?>(SecretBody);
        if (adapter == 0)
        {
            return (await client.SendForResultAsync<string>(request)).Problem!;
        }
        if (adapter == 1)
        {
            return (await client.SendForResultAsync(request, projection)).Problem!;
        }
        if (adapter == 2)
        {
            return (await client.SendForResultAsync(request)).Problem!;
        }
        var command = Command.Create(TestCommandType);
        var runtime = new CommandExecutionRuntime();
        if (adapter == 3)
        {
            return (await client.SendForResultAsync<string, Command>(command, _ => request(), runtime)).Problem!;
        }
        if (adapter == 4)
        {
            return (await client.SendForResultAsync(command, _ => request(), projection, runtime)).Problem!;
        }
        return (await client.SendForResultAsync(command, _ => request(), runtime)).Problem!;
    }

    private static void AssertPrivateDiagnostics(ConcurrentBag<LogSnapshot> logs, Activity activity)
    {
        logs.ShouldNotBeEmpty();
        logs.All(log => log.Exception is null).ShouldBeTrue();
        AssertPrivacy(string.Join(Environment.NewLine, logs.Select(log => log.Text)));
        AssertPrivacy(ActivityText(activity));
    }

    private static string ActivityText(Activity activity) => string.Concat(activity.StatusDescription,
        string.Join(Environment.NewLine, activity.TagObjects.Select(tag => string.Concat(tag.Key, tag.Value))),
        string.Join(Environment.NewLine, activity.Events.SelectMany(item => item.Tags).Select(tag => string.Concat(tag.Key, tag.Value))));

    private static async Task<Exception> ObserveFailureAsync(Task<HttpResponseMessage> pending)
    {
        try
        {
            using var response = await pending;
        }
        catch (Exception exception)
        {
            return exception;
        }
        throw new InvalidOperationException(ExpectedFailure);
    }

    private static Exception CreateFailure(int kind) => kind switch
    {
        0 => new TaskCanceledException(RequestUrl, new IOException(SecretBody), CancellationToken.None),
        1 => new TaskCanceledException(RequestUrl, new IOException(SecretBody), new CancellationToken(true)),
        2 => new TaskCanceledException(RequestUrl, new TimeoutException(SecretBody)),
        _ => new HttpRequestException(HttpRequestError.ConnectionError, RequestUrl,
            new AggregateException(new IOException(SecretBody), new InvalidOperationException(SecretQuery)), HttpStatusCode.BadGateway)
    };

    private static Exception ThrowOrdinaryFailure()
    {
        try
        {
            throw new InvalidOperationException(OrdinaryDetail);
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }

    private static IHost CreateHost(ConcurrentBag<LogSnapshot> logs, ConcurrentBag<Activity> spans, bool traces,
        HttpMessageHandler handler, Action<CommandHttpClientOptions> configure)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.AddProcessor(new SimpleLogRecordExportProcessor(new CaptureExporter<LogRecord>(record =>
            {
                if (record.CategoryName?.StartsWith(CommunicationTelemetry.SourceName, StringComparison.Ordinal) == true)
                {
                    var attributes = string.Join(Environment.NewLine,
                        record.Attributes?.Select(item => string.Concat(item.Key, item.Value)) ?? []);
                    logs.Add(new LogSnapshot(record.LogLevel, record.Exception, record.TraceId,
                        string.Concat(record.FormattedMessage, record.Exception?.ToString(), attributes)));
                }
            })));
        });
        if (traces)
        {
            builder.AddCommunicationTelemetry();
            builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing
                .SetSampler(new AlwaysOnSampler())
                .AddProcessor(new SimpleActivityExportProcessor(new CaptureExporter<Activity>(spans.Add))));
        }
        builder.Services.AddHttpClient(ClientName).RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddCommunicationResilienceHandler(configure);
        var host = builder.Build();
        CommunicationLogger.Configure(host.Services);
        if (!traces)
        {
            host.Services.GetService<TracerProvider>().ShouldBeNull();
        }
        return host;
    }

    private static void AssertSpans(ConcurrentBag<Activity> spans, bool traces, string? sourceMethod = null)
    {
        if (!traces)
        {
            spans.ShouldBeEmpty();
            return;
        }
        var execution = spans.Single(span => span.OperationName == CommunicationTelemetry.CommandExecutionActivityName);
        execution.GetTagItem(CommunicationTelemetry.CommandCorrelationIdTag).ShouldBe(SafeAuthority);
        var text = string.Concat(execution.StatusDescription,
            string.Join(Environment.NewLine, execution.TagObjects.Select(tag => string.Concat(tag.Key, tag.Value))),
            string.Join(Environment.NewLine, execution.Events.SelectMany(item => item.Tags).Select(tag => string.Concat(tag.Key, tag.Value))));
        AssertPrivacy(text);
        if (sourceMethod is not null)
        {
            var exception = execution.Events.First(item => item.Name == ExceptionEvent);
            exception.Tags.Single(tag => tag.Key == ExceptionType).Value.ShouldNotBeNull();
            exception.Tags.Single(tag => tag.Key == ExceptionStackTrace).Value!.ToString()!.ShouldContain(sourceMethod);
        }
    }

    private static void AssertPrivacy(string text)
    {
        foreach (var secret in new[] { SecretUser, SecretPassword, SecretPath, SecretQuery, SecretBody, SecretReason })
        {
            text.ShouldNotContain(secret);
        }
    }

    private sealed record LogSnapshot(LogLevel Level, Exception? Exception, ActivityTraceId TraceId, string Text);

    private sealed class FailureHandler(Exception exception) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class ProblemResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = SecretReason,
                Content = JsonContent.Create(Problem.Create(SecretReason, SecretBody, HttpStatusCode.ServiceUnavailable))
            });
    }

    private sealed class ResponseHandler(bool success = false) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(success ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = SecretReason,
                Content = new StringContent(SecretBody)
            });
    }

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Telemetry;
using Shouldly;

namespace ManagedCode.Communication.Tests;

/// <summary>
///     Everything the library offers, used with nothing registered: no DI container, no logger, no OpenTelemetry.
/// </summary>
/// <remarks>
///     A library that quietly requires setup before it can be used is a trap for anyone trying it in a console
///     app or a unit test. These tests are the standing guarantee that registration is optional — if any code
///     path starts depending on configuration, one of them fails.
/// </remarks>
[NotInParallel]
public class ZeroConfigurationTests
{
    // ---------- core ----------
    [Test]
    public void ResultsAndProblemsWorkWithNothingRegistered()
    {
        var success = Result<int>.Succeed(42);
        var failure = Result<string>.Fail(Problem.Create("nope", "denied", 403));

        success.IsSuccess.ShouldBeTrue();
        success.Value.ShouldBe(42);
        failure.Problem!.StatusCode.ShouldBe(403);
        failure.IsFailed.ShouldBeTrue();
    }

    [Test]
    public void ValidationProblemsWorkWithNothingRegistered()
    {
        var problem = Problem.Validation(("email", "required"));
        problem.AddValidationError("name", "too short");

        problem.GetValidationErrors()!.Keys.OrderBy(k => k).ShouldBe(["email", "name"]);
        Result.Fail(problem).IsInvalid.ShouldBeTrue();
    }

    [Test]
    public void SerializationWorksWithNothingRegistered()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(Result<int>.Succeed(7), options);

        JsonSerializer.Deserialize<Result<int>>(json, options).Value.ShouldBe(7);
    }

    [Test]
    public void ExceptionMappingWorksWithNothingRegistered()
    {
        Result.Fail(new InvalidOperationException("boom"), HttpStatusCode.InternalServerError)
            .Problem!.StatusCode.ShouldBe(500);
    }

    // ---------- logging ----------

    [Test]
    public void TheLoggerResolvesWithoutAnyConfiguration()
    {
        // Never call CommunicationLogger.Configure: the last-resort factory must cover it.
        var logger = CommunicationLogger.GetLogger();

        logger.ShouldNotBeNull();
        Should.NotThrow(() => logger.LogAtWarning());
    }

    [Test]
    public void ReportingAFailureWithoutALoggerOnlyRecordsTelemetry()
    {
        var result = Result<int>.Fail(Problem.Create("boom", "d", 500));

        // Passing null is the documented way to skip logging; it must not be a null-reference waiting to happen.
        Should.NotThrow(() => CommunicationDiagnostics.ReportFailure(null, result.Problem));
        result.Report().IsFailed.ShouldBeTrue();
    }

    [Test]
    public void TrackWorksWithoutALogger()
    {
        var result = CommunicationDiagnostics.Track<int>("op", () => throw new InvalidOperationException("boom"));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Detail.ShouldBe("boom");
    }

    // ---------- telemetry ----------

    [Test]
    public void RecordingTelemetryWithNoListenerIsANoOp()
    {
        // No ActivityListener and no MeterListener are attached here, which is the state of any application
        // that has not opted into OpenTelemetry.
        Should.NotThrow(() =>
        {
            CommunicationTelemetry.RecordFailure(Problem.Create("boom", "d", 500));
            CommunicationTelemetry.RecordFailure(Problem.Create("boom", "d", 500), new InvalidOperationException());
            CommunicationTelemetry.RecordFailure(Result.Fail("x"));
        });
    }

    [Test]
    public void StartingAnActivityIsSafeWhetherOrNotAnythingIsListening()
    {
        // Deliberately does not assert that the activity is null: ActivityListener registration is
        // process-wide, so whether one exists depends on which other tests have run. The guarantee that
        // matters to a caller is that the call site is safe either way — `using` on a null activity is a no-op.
        Should.NotThrow(() =>
        {
            using var activity = CommunicationTelemetry.StartActivity("op");
            CommunicationTelemetry.RecordFailure(Problem.Create("boom", "d", 500), null, activity);
        });
    }

    // ---------- railway ----------

    [Test]
    public void RailwayWorksWithNothingRegistered()
    {
        var result = Result<int>.Succeed(2)
            .Map(value => value * 3)
            .Ensure(value => value > 0, Problem.Create("non_positive", "d", 400))
            .Tap(_ => { })
            .Bind(value => Result<string>.Succeed($"v={value}"));

        result.Value.ShouldBe("v=6");
    }

    [Test]
    public async Task AsyncRailwayWorksWithNothingRegistered()
    {
        var value = await Task.FromResult(Result<int>.Succeed(2))
            .MapAsync(v => Task.FromResult(v + 1))
            .MatchAsync(v => $"ok:{v}", p => $"err:{p.Title}");

        value.ShouldBe("ok:3");
    }

    [Test]
    public void AggregationWorksWithNothingRegistered()
    {
        Result.Merge(Result.Succeed(), Result.Succeed()).IsSuccess.ShouldBeTrue();
        Result.MergeAll(Result.Succeed(), Result.Fail("a"), Result.Fail("b")).IsFailed.ShouldBeTrue();
        Result.Combine(Result<int>.Succeed(1), Result<int>.Succeed(2)).Collection.ShouldBe([1, 2]);
    }

    // ---------- CQRS ----------

    [Test]
    public async Task AuthoringACqrsStreamWorksWithNothingRegistered()
    {
        var chunks = new List<CqrsStreamChunk<string, string>>();

        await foreach (var chunk in CqrsStream.Create<string, string>(async writer =>
        {
            await writer.StartedAsync("started");
            return Result<string>.Succeed("done");
        }))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(2);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L]);
    }

    [Test]
    public async Task NormalizingAStreamWorksWithNothingRegistered()
    {
        var chunks = new List<CqrsStreamChunk<string, string>>();

        await foreach (var chunk in CqrsStream.Normalize(Incomplete()))
        {
            chunks.Add(chunk);
        }

        chunks[^1].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);

        static async IAsyncEnumerable<CqrsStreamChunk<string, string>> Incomplete()
        {
            await Task.Yield();
            yield return CqrsStreamChunk<string, string>.Started();
        }
    }

    [Test]
    public async Task TheCqrsHttpClientWorksWithNothingRegistered()
    {
        using var client = new HttpClient(new UnreachableHandler());

        var chunks = new List<CqrsStreamChunk<string, string>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<string, string>("https://example.com/x"))
        {
            chunks.Add(chunk);
        }

        // A dead endpoint still produces a terminal chunk rather than throwing.
        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
    }

    private sealed class UnreachableHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            System.Threading.CancellationToken cancellationToken)
        {
            throw new HttpRequestException("connection refused");
        }
    }
}

file static class LoggerProbe
{
    public static void LogAtWarning(this Microsoft.Extensions.Logging.ILogger logger)
    {
        ProblemLoggerCenter.LogProblem(logger, "title", 500, "detail");
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.AspNetCore;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     The README's CQRS examples, compiled and executed.
/// </summary>
/// <remarks>
///     Documentation that does not compile is worse than no documentation. Keeping the samples here means a change to
///     the API breaks the build rather than quietly leaving a broken snippet in the README.
/// </remarks>
public class CqrsReadmeSampleTests
{
    public sealed record ImportProgress(int Percent);

    public sealed record ImportReport(int Imported);

    // --- README: "Writing a handler" (push-style) ---
    private static IAsyncEnumerable<CqrsStreamChunk<ImportProgress, ImportReport>> Import(
        CancellationToken cancellationToken)
    {
        return CqrsStream.Create<ImportProgress, ImportReport>(async writer =>
        {
            await writer.StartedAsync(new ImportProgress(0));

            for (var i = 1; i <= 10; i++)
            {
                await DoWorkAsync(writer.CancellationToken);
                await writer.ProgressAsync(new ImportProgress(i * 10));
            }

            return Result<ImportReport>.Succeed(new ImportReport(10));
        }, cancellationToken);
    }

    // --- README: "Writing a handler" (hand-written iterator) ---
    private static async IAsyncEnumerable<CqrsStreamChunk<ImportProgress, ImportReport>> ImportAsync()
    {
        yield return CqrsStreamChunk<ImportProgress, ImportReport>.Started(new ImportProgress(0));
        await Task.Delay(1);
        yield return CqrsStreamChunk<ImportProgress, ImportReport>.Progress(new ImportProgress(50));
        yield return CqrsStreamChunk<ImportProgress, ImportReport>.Completed(new ImportReport(10));
    }

    private static Task DoWorkAsync(CancellationToken cancellationToken)
    {
        return Task.Delay(1, cancellationToken);
    }

    [Test]
    public async Task ThePushStyleSampleStreamsProgressAndCompletes()
    {
        await using var app = await CqrsTestHost.StartMinimalApiAsync(
            app => app.MapGet("/import", (CancellationToken ct) => Import(ct)).WithCommunicationCqrsResults(),
            services => services.AddCommunicationCqrs());

        var (percentages, imported, problem) = await ConsumeAsync(app, "/import");

        // README: "Reading a stream"
        percentages.ShouldBe([0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100]);
        imported.ShouldBe(10);
        problem.ShouldBeNull();
    }

    [Test]
    public async Task TheHandWrittenIteratorSampleCompilesAndStreams()
    {
        await using var app = await CqrsTestHost.StartMinimalApiAsync(
            app => app.MapGet("/import", () => ImportAsync()).WithCommunicationCqrsResults(),
            services => services.AddCommunicationCqrs());

        var (percentages, imported, problem) = await ConsumeAsync(app, "/import");

        percentages.ShouldBe([0, 50]);
        imported.ShouldBe(10);
        problem.ShouldBeNull();
    }

    [Test]
    public async Task TheSampleReportsAFailedResultThroughTheSameLoop()
    {
        await using var app = await CqrsTestHost.StartMinimalApiAsync(
            app => app.MapGet("/import", () => CqrsStream.Create<ImportProgress, ImportReport>(async writer =>
                {
                    await writer.StartedAsync(new ImportProgress(0));
                    return Result<ImportReport>.Fail(Problem.Create("source_unavailable", "The feed is offline.", 503));
                }))
                .WithCommunicationCqrsResults(),
            services => services.AddCommunicationCqrs());

        var (percentages, imported, problem) = await ConsumeAsync(app, "/import");

        percentages.ShouldBe([0]);
        imported.ShouldBeNull();
        problem.ShouldNotBeNull();
        problem!.Title.ShouldBe("source_unavailable");
    }

    [Test]
    public async Task TheClientOptionsSampleCompilesAndRuns()
    {
        // README: "Tuning" — client side.
        var options = new CqrsStreamClientOptions
        {
            MalformedChunkBehavior = CqrsMalformedChunkBehavior.Skip,
            EnsureTerminalChunk = true
        };

        await using var app = await CqrsTestHost.StartMinimalApiAsync(
            app => app.MapGet("/import", () => ImportAsync()).WithCommunicationCqrsResults(),
            services => services.AddCommunicationCqrs());

        var count = 0;
        await foreach (var chunk in app.GetTestClient()
                           .GetForCqrsStreamAsync<ImportProgress, ImportReport>("/import", options))
        {
            count++;
            chunk.ShouldNotBeNull();
        }

        count.ShouldBe(3);
    }

    [Test]
    public async Task ThePerEndpointOptionsSampleCompilesAndRuns()
    {
        // README: "Tuning" — server side, per endpoint.
        await using var app = await CqrsTestHost.StartMinimalApiAsync(
            app => app.MapGet("/import", () => ImportAsync())
                .WithCommunicationCqrsResults(new CqrsStreamServerOptions { EnsureTerminalChunk = false }),
            services => services.AddCommunicationCqrs(options =>
            {
                options.AssignSequenceNumbers = true;
                options.EnsureTerminalChunk = true;
            }));

        using var response = await app.GetTestClient().GetAsync("/import");
        (await SseTestReader.ReadFramesAsync(response)).Count.ShouldBe(3);
    }

    private static async Task<(List<int> Percentages, int? Imported, Problem? Problem)> ConsumeAsync(
        WebApplication app,
        string route)
    {
        var percentages = new List<int>();
        int? imported = null;
        Problem? failure = null;

        await foreach (var chunk in app.GetTestClient()
                           .GetForCqrsStreamAsync<ImportProgress, ImportReport>(route))
        {
            if (chunk.TryGetProgress(out var progress))
            {
                percentages.Add(progress.Percent);
            }
            else if (chunk.TryGetResult(out var report))
            {
                imported = report.Imported;
            }
            else if (chunk.TryGetProblem(out var problem))
            {
                failure = problem;
            }
        }

        return (percentages, imported, failure);
    }
}

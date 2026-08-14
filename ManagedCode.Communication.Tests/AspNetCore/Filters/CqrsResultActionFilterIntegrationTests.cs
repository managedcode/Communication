using System.Collections.Generic;
using System;
using System.Net;
using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Filters;

public class CqrsResultActionFilterIntegrationTests
{
    [Fact]
    public async Task WithCommunicationCqrsFilters_StreamsIAsyncEnumerableAndConvertsExceptionsToFailedChunk()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/cqrs/action-filter/stream-exception");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<ProgressUpdate, FinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        var chunks = new List<CqrsStreamChunk<ProgressUpdate, FinalResult>>();
        await foreach (var item in parser.EnumerateAsync())
        {
            chunks.Add(item.Data);
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[2].Final.ShouldNotBeNull();
        chunks[2].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[2].Final!.Value.Problem.ShouldNotBeNull();
        chunks[2].Final!.Value.Problem!.StatusCode.ShouldBe(500);
        chunks[2].Final!.Value.Problem!.Detail.ShouldBe("Action filter stream failure");
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddControllers(options =>
        {
            options.AddCommunicationCqrsFilters();
        })
            .AddApplicationPart(typeof(CqrsActionFilterController).Assembly);

        var app = builder.Build();
        app.MapControllers();

        await app.StartAsync();
        return app;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

[ApiController]
[Route("api/cqrs/action-filter")]
public sealed class CqrsActionFilterController : ControllerBase
{
    [HttpGet("stream-exception")]
    public IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunExceptionStream()
    {
        return RunStreamThatThrows();
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunStreamThatThrows()
    {
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Started(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("started")),
            sequence: 1);

        await Task.Delay(1);
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Progress(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("processing")),
            sequence: 2);

        throw new InvalidOperationException("Action filter stream failure");
    }
}

public sealed record ProgressUpdate(string State);
public sealed record FinalResult(string Status);

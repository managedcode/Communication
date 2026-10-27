using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

public class CqrsStreamChunkConversionTests
{
    private const string Payload = "complete";
    private const string Progress = "running";
    private const string FailureTitle = "rejected";
    private const string FailureDetail = "The operation was rejected.";
    private const string Message = "terminal outcome";
    private const string EventType = "custom-terminal";
    private const string EventId = "terminal-2";
    private const string Route = "/converted";

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void FromResult_PreservesOutcomeAndMetadata(bool success)
    {
        var result = CreateResult(success);
        var chunk = CqrsStreamChunk<string, string>.FromResult(result, Message, EventType, EventId, 2);

        chunk.Kind.ShouldBe(success ? CqrsStreamChunkKind.Completed : CqrsStreamChunkKind.Failed);
        chunk.Final.ShouldBe(result);
        chunk.Problem.ShouldBeSameAs(result.Problem);
        chunk.ProgressResult.ShouldBeNull();
        chunk.Message.ShouldBe(Message);
        chunk.EventType.ShouldBe(EventType);
        chunk.EventId.ShouldBe(EventId);
        chunk.Sequence.ShouldBe(2);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void ImplicitConversion_WithSamePayloadTypes_IsAlwaysTerminal(bool success)
    {
        var result = CreateResult(success);
        CqrsStreamChunk<string, string> chunk = result;

        chunk.IsTerminal.ShouldBeTrue();
        chunk.IsProgress.ShouldBeFalse();
        chunk.IsCompleted.ShouldBe(success);
        chunk.IsFailed.ShouldBe(!success);
        chunk.Final.ShouldBe(result);
        chunk.ProgressResult.ShouldBeNull();
        chunk.EventType.ShouldBe(success
            ? CqrsStreamChunk<string, string>.CompletedEventType
            : CqrsStreamChunk<string, string>.FailedEventType);
        chunk.Message.ShouldBeNull();
        chunk.EventId.ShouldBeNull();
        chunk.Sequence.ShouldBeNull();
    }

    [Test]
    [Arguments(true, true)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(false, false)]
    public async Task ConvertedResult_TraversesSseToTerminalOutcome(bool success, bool implicitConversion)
    {
        var result = CreateResult(success);
        await using var app = await CqrsTestHost.StartMinimalApiAsync(app => app
            .MapGet(Route, () => StreamAsync(result, implicitConversion))
            .WithCommunicationCqrsResults());
        using var client = app.GetTestClient();
        var chunks = new List<CqrsStreamChunk<string, string>>();

        await foreach (var chunk in client.GetForCqrsStreamAsync<string, string>(Route))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(2);
        chunks[0].TryGetProgress(out var progress).ShouldBeTrue();
        progress.ShouldBe(Progress);
        chunks[1].Kind.ShouldBe(success ? CqrsStreamChunkKind.Completed : CqrsStreamChunkKind.Failed);
        chunks[1].Final!.Value.IsSuccess.ShouldBe(success);
        if (success)
        {
            chunks[1].TryGetResult(out var payload).ShouldBeTrue();
            payload.ShouldBe(Payload);
        }
        else
        {
            chunks[1].TryGetProblem(out var problem).ShouldBeTrue();
            problem.Title.ShouldBe(FailureTitle);
            problem.Detail.ShouldBe(FailureDetail);
            problem.StatusCode.ShouldBe(409);
        }
        if (!implicitConversion)
        {
            chunks[1].Message.ShouldBe(Message);
            chunks[1].EventType.ShouldBe(EventType);
            chunks[1].EventId.ShouldBe(EventId);
        }
        chunks[1].Sequence.ShouldBe(2);
    }

    private static Result<string> CreateResult(bool success) => success
        ? Result<string>.Succeed(Payload)
        : Result<string>.Fail(Problem.Create(FailureTitle, FailureDetail, 409));

    private static async IAsyncEnumerable<CqrsStreamChunk<string, string>> StreamAsync(
        Result<string> result, bool implicitConversion)
    {
        await Task.Yield();
        yield return CqrsStreamChunk<string, string>.Progress(Progress);
        if (implicitConversion)
        {
            yield return result;
        }
        else
        {
            yield return CqrsStreamChunk<string, string>.FromResult(result, Message, EventType, EventId, 2);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ResultFromTTests
{
    [Fact]
    public void From_FromFunc_ReturnsSucceededResult()
    {
        var result = Result.From<int>((Func<int>)(() => 42));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void From_FromFunc_WhenThrows_ReturnsFailedResult()
    {
        var result = Result.From<int>((Func<int>)(() => throw new InvalidOperationException("boom")));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_FromFuncResult_TaskAsync_ReturnsResult()
    {
        var result = await Result.From(async () => Result.Succeed(12));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(12);
    }

    [Fact]
    public async Task From_FromFuncResult_TaskAsync_WhenThrows_ReturnsFailedResult()
    {
        var result = await Result.From<int>(async () => throw new InvalidOperationException("task boom"));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_FromTask_ReturnsResult()
    {
        var result = await Result.From(Task.FromResult(10));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(10);
    }

    [Fact]
    public async Task From_FromTask_WhenFaulted_ReturnsFailedResult()
    {
        var result = await Result.From<int>(Task.FromException<int>(new InvalidOperationException("fault")));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_FromTaskResult_ReturnsResult()
    {
        var result = await Result.From(Task.FromResult(Result.Succeed(11)));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(11);
    }

    [Fact]
    public async Task From_FromTaskResult_WhenFaulted_ReturnsFailedResult()
    {
        var task = Task.FromException<Result<int>>(new InvalidOperationException("result task boom"));
        var result = await Result.From(task);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_FromTaskFunc_UsesCancellationToken_ReturnsFailedResultWhenCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await Result.From(async () => await Task.FromResult(5), cts.Token);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(TaskCanceledException));
    }

    [Fact]
    public async Task From_FromTaskFunc_ReturnsResult()
    {
        var result = await Result.From(async () => 99);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(99);
    }

    [Fact]
    public async Task From_FromTaskFunc_WhenThrows_ReturnsFailedResult()
    {
        var result = await Result.From<int>(async () => throw new InvalidOperationException("delayed boom"));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_FromTaskResultFunc_ReturnsResult()
    {
        var result = await Result.From(async () => Result.Succeed(77));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(77);
    }

    [Fact]
    public async Task From_FromTaskResultFunc_WhenThrows_ReturnsFailedResult()
    {
        var result = await Result.From<int>(async () => throw new InvalidOperationException("result task func boom"));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));

    }

    [Fact]
    public async Task From_FromValueTask_ReturnsResult()
    {
        var result = await Result.From(new ValueTask<int>(45));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(45);
    }

    [Fact]
    public async Task From_FromValueTask_WhenFaulted_ReturnsFailedResult()
    {
        var result = await Result.From<int>(new ValueTask<int>(Task.FromException<int>(new InvalidOperationException("value fault"))));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_FromValueTaskResult_ReturnsResult()
    {
        var result = await Result.From(new ValueTask<Result<int>>(Result.Succeed(33)));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(33);
    }

    [Fact]
    public async Task From_FromValueTaskResult_WhenFaulted_ReturnsFailedResult()
    {
        var result = await Result.From<int>(new ValueTask<Result<int>>(Task.FromException<Result<int>>(new InvalidOperationException("value result fault"))));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }
}

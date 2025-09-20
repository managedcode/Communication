using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results.Extensions;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ResultValueExecutionExtensionsTests
{
    [Fact]
    public void From_Func_ReturnsValue()
    {
        var result = Result<int>.From(new Func<int>(() => 42));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void From_Func_Throws_ReturnsFailure()
    {
        var result = Result<int>.From(new Func<int>(() => throw new InvalidOperationException("fail")));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
        result.Problem.Detail.ShouldBe("fail");
    }

    [Fact]
    public void From_FuncResult_ReturnsUnderlyingResult()
    {
        var original = Result<int>.Succeed(7);

        var result = Result<int>.From(new Func<Result<int>>(() => original));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(7);
    }

    [Fact]
    public void From_FuncResult_Throws_ReturnsFailure()
    {
        var result = Result<int>.From(new Func<Result<int>>(() => throw new InvalidOperationException("boom")));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_Task_ReturnsSuccess()
    {
        var result = await Result<int>.From(Task.FromResult(11));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(11);
    }

    [Fact]
    public async Task From_TaskReturningResult_Succeeds()
    {
        var resultTask = Task.FromResult(Result<int>.Succeed(77));

        var result = await Result<int>.From(resultTask);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(77);
    }

    [Fact]
    public async Task From_Task_Throws_ReturnsFailure()
    {
        var exception = new InvalidOperationException("task boom");
        var task = Task.FromException<int>(exception);

        var result = await Result<int>.From(task);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
        result.Problem.Detail.ShouldBe("task boom");
    }

    [Fact]
    public async Task From_FuncTask_WithCanceledToken_ReturnsFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await Result<int>.From(async () =>
        {
            await Task.Delay(1);
            return 99;
        }, cts.Token);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(TaskCanceledException));
    }

    [Fact]
    public async Task From_TaskReturningResult_Throws_ReturnsFailure()
    {
        var exception = new InvalidOperationException("task result");
        var result = await Result<int>.From(Task.FromException<Result<int>>(exception));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_ValueTask_ReturnsSuccess()
    {
        var result = await Result<int>.From(new ValueTask<int>(13));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(13);
    }

    [Fact]
    public async Task From_ValueTask_Faulted_ReturnsFailure()
    {
        var valueTask = new ValueTask<int>(Task.FromException<int>(new InvalidOperationException("vt")));

        var result = await Result<int>.From(valueTask);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_FuncValueTask_ReturnsFailureOnException()
    {
        static async ValueTask<int> ThrowingValueTask()
        {
            await Task.Yield();
            throw new InvalidOperationException("func value task");
        }

        var result = await Result<int>.From((Func<ValueTask<int>>)ThrowingValueTask);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_FuncTaskReturningResult_Success()
    {
        var result = await Result<int>.From(() => Task.FromResult(Result<int>.Succeed(101)));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(101);
    }

    [Fact]
    public async Task From_FuncTaskReturningResult_Exception()
    {
        static Task<Result<int>> ThrowingTask()
        {
            return Task.FromException<Result<int>>(new InvalidOperationException("factory result"));
        }

        var result = await Result<int>.From((Func<Task<Result<int>>>)ThrowingTask);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task From_ValueTaskReturningResult_Success()
    {
        static ValueTask<Result<int>> Factory() => new(Result<int>.Succeed(205));

        var result = await Result<int>.From((Func<ValueTask<Result<int>>>)Factory);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(205);
    }

    [Fact]
    public async Task From_ValueTaskReturningResult_Exception()
    {
        static async ValueTask<Result<int>> ThrowingFactory()
        {
            await Task.Yield();
            throw new InvalidOperationException("value task result");
        }

        var result = await Result<int>.From((Func<ValueTask<Result<int>>>)ThrowingFactory);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public void ToResult_FromSuccessfulGenericResult_ReturnsSuccess()
    {
        IResult<int> generic = Result<int>.Succeed(5);

        var result = generic.ToResult();

        result.IsSuccess.ShouldBeTrue();
        result.Problem.ShouldBeNull();
    }

    [Fact]
    public void ToResult_FromFailedGenericResult_PreservesProblem()
    {
        var problem = Problem.Create("Failure", "problem detail");
        IResult<int> generic = Result<int>.Fail(problem);

        var result = generic.ToResult();

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldBeSameAs(problem);
    }

    [Fact]
    public async Task ResultT_AsTaskAndAsValueTask_RoundTrip()
    {
        var original = Result<int>.Fail("fail", "metadata");

        var task = original.AsTask();
        var fromTask = await task;
        fromTask.Problem.ShouldBeSameAs(original.Problem);

        var valueTask = original.AsValueTask();
        var fromValueTask = await valueTask;
        fromValueTask.Problem.ShouldBeSameAs(original.Problem);
    }
}

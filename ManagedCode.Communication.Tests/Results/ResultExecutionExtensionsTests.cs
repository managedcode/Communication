using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Constants;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ResultExecutionExtensionsTests
{
    [Fact]
    public void From_Action_Success_ReturnsSuccess()
    {
        var executed = false;

        var result = Result.From(() => executed = true);

        executed.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        result.Problem.ShouldBeNull();
    }

    [Fact]
    public void From_Action_Exception_ReturnsFailure()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result.From(new Action(() => throw exception));

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
        result.Problem.Detail.ShouldBe("boom");
    }

    [Fact]
    public async Task From_Task_CompletedTask_ReturnsSuccess()
    {
        var result = await Result.From(Task.CompletedTask);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task From_Task_Faulted_ReturnsProblemWithExceptionDetails()
    {
        var exception = new InvalidOperationException("faulted");
        var task = Task.FromException(exception);

        var result = await Result.From(task);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(AggregateException));
        result.Problem.Detail.ShouldNotBeNull();
        result.Problem.Detail!.ShouldContain("faulted");
    }

    [Fact]
    public async Task From_Task_Canceled_ReturnsTaskCanceledProblem()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var task = Task.FromCanceled(cts.Token);

        var result = await Result.From(task);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(TaskCanceledException));
    }

    [Fact]
    public async Task From_FuncTask_Exception_ReturnsFailure()
    {
        var result = await Result.From(async () =>
        {
            await Task.Delay(10);
            throw new InvalidOperationException("delayed error");
        });

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Detail.ShouldBe("delayed error");
    }

    [Fact]
    public async Task From_ValueTask_Success_ReturnsSuccess()
    {
        var valueTask = new ValueTask();

        var result = await Result.From(valueTask);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task From_ValueTask_Faulted_ReturnsGenericFailure()
    {
        var valueTask = new ValueTask(Task.FromException(new InvalidOperationException("vt boom")));

        var result = await Result.From(valueTask);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(ProblemConstants.Titles.Error);
    }

    [Fact]
    public async Task From_FuncValueTask_Exception_ReturnsFailure()
    {
        static async ValueTask ThrowingValueTask()
        {
            await Task.Yield();
            throw new InvalidOperationException("value task failure");
        }

        var result = await Result.From((Func<ValueTask>)ThrowingValueTask);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Detail.ShouldBe("value task failure");
    }

    [Fact]
    public void From_Bool_WithProblemFalse_ReturnsProvidedProblem()
    {
        var problem = Problem.Create("Custom", "Failure");

        var result = Result.From(false, problem);

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldBeSameAs(problem);
    }

    [Fact]
    public void From_FuncBool_WithProblemFalse_ReturnsProvidedProblem()
    {
        var problem = Problem.Create("Predicate", "Failed");

        var result = Result.From(() => false, problem);

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldBeSameAs(problem);
    }

    [Fact]
    public void From_Result_RoundTripsInstance()
    {
        var failure = Result.Fail("title", "detail");

        var copy = Result.From(failure);

        copy.IsFailed.ShouldBeTrue();
        copy.Problem.ShouldBeSameAs(failure.Problem);
    }

    [Fact]
    public void From_ResultT_PreservesProblem()
    {
        var generic = Result<string>.Fail("failure", "something broke");

        var result = Result.From(generic);

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldBeSameAs(generic.Problem);
    }

    [Fact]
    public async Task AsTask_And_AsValueTask_RoundTripResult()
    {
        var original = Result.Fail("oh no", "still broken");

        var task = original.AsTask();
        (await task).Problem.ShouldBeSameAs(original.Problem);

        var valueTask = original.AsValueTask();
        var awaited = await valueTask;
        awaited.Problem.ShouldBeSameAs(original.Problem);
    }
}

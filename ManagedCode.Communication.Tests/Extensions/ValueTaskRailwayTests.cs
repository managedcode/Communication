using System;
using System.Threading.Tasks;
using ManagedCode.Communication.Extensions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Extensions;

public class ValueTaskRailwayTests
{
    private const int InitialValue = 20;
    private const int RecoveredValue = 80;

    [Test]
    public async Task ValueTaskRailway_WhenSuccess_ShouldComposeWithoutTaskConversion()
    {
        var tapped = 0;
        Func<int, int> mapper = static value => value + 1;
        Func<int, Result<int>> binder = static value => Result<int>.Succeed(value * 2);
        Action<int> tap = value => tapped = value;

        ValueTask<Result<int>> pipeline = Result<int>.Succeed(InitialValue)
            .AsValueTask()
            .Map(mapper)
            .BindAsync(binder)
            .TapAsync(tap);

        var result = await pipeline;

        result.Value.ShouldBe((InitialValue + 1) * 2);
        tapped.ShouldBe(result.Value);
    }

    [Test]
    public async Task ValueTaskRailway_WhenFailed_ShouldShortCircuitAndPreserveProblem()
    {
        var originalProblem = Problem.InvalidState();
        var continuationInvoked = false;
        var finallyInvoked = false;
        Func<int, ValueTask<int>> mapper = value =>
        {
            continuationInvoked = true;
            return ValueTask.FromResult(value + 1);
        };
        Func<Result<int>, ValueTask> finallyAction = _ =>
        {
            finallyInvoked = true;
            return ValueTask.CompletedTask;
        };

        var result = await Result<int>.Fail(originalProblem)
            .AsValueTask()
            .MapAsync(mapper)
            .FinallyAsync(finallyAction);

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldBeSameAs(originalProblem);
        continuationInvoked.ShouldBeFalse();
        finallyInvoked.ShouldBeTrue();
    }

    [Test]
    public async Task ValueTaskRailway_WhenRecovered_ShouldReturnRecoveryResult()
    {
        Func<Problem, ValueTask<Result<int>>> recovery = static _ =>
            ValueTask.FromResult(Result<int>.Succeed(RecoveredValue));

        ValueTask<Result<int>> pipeline = Result<int>.Fail(Problem.InvalidState())
            .AsValueTask()
            .CompensateAsync(recovery);

        var result = await pipeline;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(RecoveredValue);
    }

    [Test]
    public async Task ValueTaskRailway_NonGenericResult_ShouldPreserveValueTaskShape()
    {
        var actionInvoked = false;
        Func<ValueTask<Result>> next = static () => ValueTask.FromResult(Result.Succeed());
        Func<ValueTask> action = () =>
        {
            actionInvoked = true;
            return ValueTask.CompletedTask;
        };

        ValueTask<Result> pipeline = Result.Succeed()
            .AsValueTask()
            .ThenAsync(next)
            .TapAsync(action);

        var result = await pipeline;

        result.IsSuccess.ShouldBeTrue();
        actionInvoked.ShouldBeTrue();
    }
}

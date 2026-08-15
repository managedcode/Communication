using System;
using System.Threading.Tasks;
using ManagedCode.Communication.Extensions;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

/// <summary>
///     An asynchronous chain must accept ordinary synchronous delegates.
/// </summary>
/// <remarks>
///     Without these overloads a chain has to be broken with an <c>await</c> and a temporary variable the moment
///     one step happens not to be asynchronous — which is most of them. Every test here is really a compilation
///     test: the assertions confirm the behaviour, but the fact that the chain compiles at all is the point.
/// </remarks>
public class AsyncRailwaySyncDelegateTests
{
    private static Task<Result<int>> SucceedAsync(int value = 2) => Task.FromResult(Result<int>.Succeed(value));

    private static Task<Result<int>> FailAsync() =>
        Task.FromResult(Result<int>.Fail(Problem.Create("boom", "detail", 409)));

    [Fact]
    public async Task AChainOfSynchronousStepsNeverBreaksTheAwait()
    {
        var seen = 0;

        var result = await SucceedAsync(2)
            .EnsureAsync(value => value > 0, Problem.Validation(("value", "must be positive")))
            .BindAsync(value => Result<int>.Succeed(value * 5))
            .Map(value => value + 1)
            .TapAsync(value => seen = value)
            .FinallyAsync(_ => { });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(11);
        seen.ShouldBe(11);
    }

    [Fact]
    public async Task MapRunsOnSuccessAndPropagatesFailureUntouched()
    {
        (await SucceedAsync(3).Map(v => v * 2)).Value.ShouldBe(6);

        var failed = await FailAsync().Map(v => v * 2);
        failed.IsFailed.ShouldBeTrue();
        failed.Problem!.StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task MapStillAcceptsAnAsynchronousMapper()
    {
        // The overload that takes Func<TIn, Task<TOut>> keeps its name; adding a synchronous Map must not make
        // this call ambiguous.
        (await SucceedAsync(3).MapAsync(v => Task.FromResult(v * 2))).Value.ShouldBe(6);
    }

    [Fact]
    public async Task BindShortCircuitsOnFailure()
    {
        var ran = false;

        var result = await FailAsync().BindAsync(value =>
        {
            ran = true;
            return Result<int>.Succeed(value);
        });

        ran.ShouldBeFalse();
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task TapAndDoRunOnlyOnSuccess()
    {
        var taps = 0;

        await SucceedAsync().TapAsync(_ => taps++);
        await SucceedAsync().DoAsync(_ => taps++);
        await FailAsync().TapAsync(_ => taps++);
        await FailAsync().DoAsync(_ => taps++);

        taps.ShouldBe(2);
    }

    [Fact]
    public async Task CompensateReceivesTheProblemAndMayItselfFail()
    {
        Problem? seen = null;

        var recovered = await FailAsync().CompensateAsync(problem =>
        {
            seen = problem;
            return Result<int>.Succeed(99);
        });

        recovered.Value.ShouldBe(99);
        seen!.StatusCode.ShouldBe(409);

        var stillFailing = await FailAsync().CompensateAsync(Result<int>.Fail);
        stillFailing.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task CompensateLeavesASuccessAlone()
    {
        var ran = false;

        var result = await SucceedAsync(7).CompensateAsync(_ =>
        {
            ran = true;
            return Result<int>.Succeed(0);
        });

        ran.ShouldBeFalse();
        result.Value.ShouldBe(7);
    }

    [Fact]
    public async Task ElseSubstitutesOnlyForAFailure()
    {
        (await FailAsync().ElseAsync(() => Result<int>.Succeed(5))).Value.ShouldBe(5);
        (await SucceedAsync(7).ElseAsync(() => Result<int>.Succeed(5))).Value.ShouldBe(7);
    }

    [Fact]
    public async Task FinallyRunsWhateverTheOutcome()
    {
        var calls = 0;

        await SucceedAsync().FinallyAsync(_ => calls++);
        await FailAsync().FinallyAsync(_ => calls++);

        calls.ShouldBe(2);
    }

    [Fact]
    public async Task ANullDelegateIsRejected()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => SucceedAsync().Map<int, int>(null!));
        await Should.ThrowAsync<ArgumentNullException>(() => SucceedAsync().BindAsync((Func<int, Result<int>>)null!));
        await Should.ThrowAsync<ArgumentNullException>(() => SucceedAsync().TapAsync((Action<int>)null!));
        await Should.ThrowAsync<ArgumentNullException>(() => SucceedAsync().DoAsync((Action<int>)null!));
        await Should.ThrowAsync<ArgumentNullException>(() => FailAsync().CompensateAsync((Func<Problem, Result<int>>)null!));
        await Should.ThrowAsync<ArgumentNullException>(() => FailAsync().ElseAsync((Func<Result<int>>)null!));
        await Should.ThrowAsync<ArgumentNullException>(() => SucceedAsync().FinallyAsync((Action<Result<int>>)null!));
    }
}

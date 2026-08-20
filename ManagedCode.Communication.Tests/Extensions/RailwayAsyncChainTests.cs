using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedCode.Communication.Extensions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Extensions;

/// <summary>
///     Asynchronous railway chains.
/// </summary>
/// <remarks>
///     Only four operators used to accept a <c>Task&lt;Result&lt;T&gt;&gt;</c> receiver, so a chain had to be
///     broken with an <c>await</c> and a temporary the moment it reached any of the others. These tests are
///     written as unbroken chains on purpose — if an overload goes missing again, they stop compiling.
/// </remarks>
public class RailwayAsyncChainTests
{
    private static Task<Result<int>> StartAsync(int value) => Task.FromResult(Result<int>.Succeed(value));

    private static Task<Result<int>> FailAsync(string title = "boom") =>
        Task.FromResult(Result<int>.Fail(Problem.Create(title, "detail", 500)));

    [Test]
    public async Task AFullyAsyncHappyPathChainsWithoutBreaking()
    {
        var log = new List<string>();

        var result = await StartAsync(2)
            .MapAsync(value => Task.FromResult(value * 3))
            .EnsureAsync(value => value > 0, Problem.Create("non_positive", "must be positive", 400))
            .TapAsync(value => { log.Add($"tapped {value}"); return Task.CompletedTask; })
            .DoAsync(value => { log.Add($"did {value}"); return Task.CompletedTask; })
            .BindAsync(value => Task.FromResult(Result<string>.Succeed($"value={value}")));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("value=6");
        log.ShouldBe(["tapped 6", "did 6"]);
    }

    [Test]
    public async Task AFailureShortCircuitsEveryLaterStep()
    {
        var ran = false;

        var result = await FailAsync()
            .MapAsync(value => Task.FromResult(value * 3))
            .TapAsync(_ => { ran = true; return Task.CompletedTask; })
            .DoAsync(_ => { ran = true; return Task.CompletedTask; })
            .EnsureAsync(_ => true, Problem.Create("unused", "d", 400));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe("boom");
        ran.ShouldBeFalse();
    }

    [Test]
    public async Task EnsureAsyncFailsTheChainOnAFailingPredicate()
    {
        var result = await StartAsync(-1)
            .EnsureAsync(value => value > 0, Problem.Create("non_positive", "must be positive", 400));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe("non_positive");
    }

    [Test]
    public async Task EnsureAsyncSupportsAnAsynchronousPredicate()
    {
        var result = await StartAsync(5)
            .EnsureAsync(async value =>
            {
                await Task.Yield();
                return value > 10;
            }, Problem.Create("too_small", "d", 400));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe("too_small");
    }

    [Test]
    public async Task CompensateAsyncRecoversFromAFailure()
    {
        var result = await FailAsync()
            .CompensateAsync(problem =>
            {
                problem.Title.ShouldBe("boom");
                return Task.FromResult(Result<int>.Succeed(99));
            });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(99);
    }

    [Test]
    public async Task CompensateWithAsyncSubstitutesADefault()
    {
        (await FailAsync().CompensateWithAsync(7)).Value.ShouldBe(7);
        (await StartAsync(1).CompensateWithAsync(7)).Value.ShouldBe(1);
    }

    [Test]
    public async Task ElseAsyncSubstitutesAnAlternative()
    {
        var result = await FailAsync().ElseAsync(() => Task.FromResult(Result<int>.Succeed(42)));

        result.Value.ShouldBe(42);
    }

    [Test]
    public async Task FinallyAsyncRunsOnBothBranches()
    {
        var seen = new List<bool>();

        await StartAsync(1).FinallyAsync(r => { seen.Add(r.IsSuccess); return Task.CompletedTask; });
        await FailAsync().FinallyAsync(r => { seen.Add(r.IsSuccess); return Task.CompletedTask; });

        seen.ShouldBe([true, false]);
    }

    [Test]
    public async Task MatchAsyncLeavesTheRailway()
    {
        var success = await StartAsync(3).MatchAsync(value => $"ok:{value}", problem => $"err:{problem.Title}");
        var failure = await FailAsync().MatchAsync(value => $"ok:{value}", problem => $"err:{problem.Title}");

        success.ShouldBe("ok:3");
        failure.ShouldBe("err:boom");
    }

    [Test]
    public async Task MatchAsyncSupportsAsynchronousHandlers()
    {
        var value = await StartAsync(3).MatchAsync(
            async v => { await Task.Yield(); return $"ok:{v}"; },
            async p => { await Task.Yield(); return $"err:{p.Title}"; });

        value.ShouldBe("ok:3");
    }

    [Test]
    public async Task ANonGenericResultChainsAsynchronouslyToo()
    {
        var log = new List<string>();

        var result = await Task.FromResult(Result.Succeed())
            .ThenAsync(() => Task.FromResult(Result.Succeed()))
            .TapAsync(() => { log.Add("tapped"); return Task.CompletedTask; })
            .FinallyAsync(_ => { log.Add("finally"); return Task.CompletedTask; });

        result.IsSuccess.ShouldBeTrue();
        log.ShouldBe(["tapped", "finally"]);
    }

    [Test]
    public async Task ANonGenericResultCanBindIntoATypedOne()
    {
        var result = await Task.FromResult(Result.Succeed())
            .BindAsync(() => Task.FromResult(Result<int>.Succeed(5)));

        result.Value.ShouldBe(5);
    }

    [Test]
    public async Task ATypedResultCanBindIntoANonGenericOne()
    {
        var result = await StartAsync(5).BindAsync(value =>
        {
            value.ShouldBe(5);
            return Task.FromResult(Result.Succeed());
        });

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task ANonGenericFailurePropagatesThroughTheChain()
    {
        var ran = false;

        var result = await Task.FromResult(Result.Fail(Problem.Create("nope", "d", 403)))
            .ThenAsync(() => Task.FromResult(Result.Succeed()))
            .TapAsync(() => { ran = true; return Task.CompletedTask; });

        result.IsFailed.ShouldBeTrue();
        result.Problem!.StatusCode.ShouldBe(403);
        ran.ShouldBeFalse();
    }

    [Test]
    public async Task ASynchronousResultStartsAnAsyncChain()
    {
        var log = new List<string>();

        var result = await Result<int>.Succeed(4)
            .TapAsync(value => { log.Add($"tapped {value}"); return Task.CompletedTask; })
            .MapAsync(value => Task.FromResult(value + 1));

        result.Value.ShouldBe(5);
        log.ShouldBe(["tapped 4"]);
    }

    [Test]
    public async Task MatchAsyncWorksOnASynchronousResult()
    {
        var value = await Result<int>.Succeed(1).MatchAsync(
            async v => { await Task.Yield(); return v * 2; },
            async _ => { await Task.Yield(); return -1; });

        value.ShouldBe(2);
    }
}

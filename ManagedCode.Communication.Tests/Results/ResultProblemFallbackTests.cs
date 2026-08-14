using System;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

/// <summary>
///     The generic-error fallback used when a failed result carries no <see cref="Problem" />.
/// </summary>
/// <remarks>
///     The fallback used to be written back into the struct's field from the getter. That assignment is
///     discarded whenever the struct is read through a defensive copy — a readonly field, an <c>in</c> parameter,
///     a boxed value — so the "cache" quietly allocated on every read in exactly those cases.
/// </remarks>
public class ResultProblemFallbackTests
{
    private sealed class ReadonlyHolder(Result result, Result<int> resultOfT)
    {
        private readonly Result _result = result;
        private readonly Result<int> _resultOfT = resultOfT;

        public Problem? ReadProblem() => _result.Problem;

        public Problem? ReadProblemOfT() => _resultOfT.Problem;
    }

    [Fact]
    public void DefaultResult_ReportsAFailureWithAGenericProblem()
    {
        var result = default(Result);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
    }

    [Fact]
    public void DefaultResultOfT_ReportsAFailureWithAGenericProblem()
    {
        var result = default(Result<int>);

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
    }

    [Fact]
    public void SuccessfulResult_HasNoProblemHoweverItIsRead()
    {
        var holder = new ReadonlyHolder(Result.Succeed(), Result<int>.Succeed(1));

        holder.ReadProblem().ShouldBeNull();
        holder.ReadProblemOfT().ShouldBeNull();
    }

    [Fact]
    public void ReadingTheFallbackThroughAReadonlyFieldStaysConsistent()
    {
        var holder = new ReadonlyHolder(default, default);

        var first = holder.ReadProblem();
        var second = holder.ReadProblem();

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first!.Title.ShouldBe(second!.Title);
        first.StatusCode.ShouldBe(second.StatusCode);
    }

    [Fact]
    public void AnExplicitProblemIsNeverReplacedByTheFallback()
    {
        var problem = Problem.Create("specific", "detail", 409);
        var holder = new ReadonlyHolder(Result.Fail(problem), Result<int>.Fail(problem));

        for (var i = 0; i < 3; i++)
        {
            holder.ReadProblem()!.Title.ShouldBe("specific");
            holder.ReadProblemOfT()!.StatusCode.ShouldBe(409);
        }
    }

    [Fact]
    public void ReadingAnExplicitProblemFromAReadonlyFieldAllocatesNothing()
    {
        var holder = new ReadonlyHolder(Result.Fail(Problem.Create("t", "d", 500)), default);

        for (var i = 0; i < 100; i++)
        {
            _ = holder.ReadProblem();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            _ = holder.ReadProblem();
        }
        var perRead = (GC.GetAllocatedBytesForCurrentThread() - before) / 1000.0;

        perRead.ShouldBe(0);
    }

    [Fact]
    public void AFailurePayloadWithoutAProblemStillDeserializesToAFailure()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var result = JsonSerializer.Deserialize<Result>("""{"isSuccess":false}""", options);

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
    }
}

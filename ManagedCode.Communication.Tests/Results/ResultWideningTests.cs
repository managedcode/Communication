using ManagedCode.Communication;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

/// <summary>
///     The implicit conversions that let a guard clause stay short.
/// </summary>
/// <remarks>
///     These are the shapes the README teaches, so they are pinned here: if a conversion is ever removed or its
///     behaviour changed, the documented examples stop compiling or stop meaning what they say.
/// </remarks>
public class ResultWideningTests
{
    private sealed record Order(string Id);

    private static Result<Order> GuardClause(bool empty)
    {
        if (empty)
        {
            return Result.FailValidation(("cart", "is empty"));
        }

        return Result<Order>.Succeed(new Order("o-1"));
    }

    [Fact]
    public void ANonGenericFailureWidensAndKeepsItsProblem()
    {
        var result = GuardClause(empty: true);

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.GetValidationErrors().ShouldNotBeNull();
        result.Problem!.GetValidationErrors()!.ShouldContainKey("cart");
    }

    [Fact]
    public void TheSuccessPathIsUnaffected()
    {
        var result = GuardClause(empty: false);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe("o-1");
    }

    [Fact]
    public void AFailedResultCanBePassedStraightAlong()
    {
        var probe = Result.Fail(Problem.Create("upstream", "went down", 503));

        Result<Order> widened = probe;

        widened.IsFailed.ShouldBeTrue();
        widened.Problem!.StatusCode.ShouldBe(503);
        widened.Problem!.Title.ShouldBe("upstream");
    }

    [Fact]
    public void ASuccessDoesNotSurviveTheConversion()
    {
        // Deliberate: a Result carries no value, so the alternative would be a "success" whose Value is null,
        // contradicting the nullable annotations. Documented on the operator itself.
        Result<Order> widened = Result.Succeed();

        widened.IsFailed.ShouldBeTrue();
        widened.Value.ShouldBeNull();
    }

    [Fact]
    public void AProblemWidensDirectly()
    {
        Result<Order> fromProblem = Problem.Validation(("cart", "is empty"));

        fromProblem.IsFailed.ShouldBeTrue();
        fromProblem.Problem!.GetValidationErrors()!.ShouldContainKey("cart");
    }

    [Fact]
    public void ABareValueWidensToASuccess()
    {
        Result<Order> fromValue = new Order("o-2");

        fromValue.IsSuccess.ShouldBeTrue();
        fromValue.Value!.Id.ShouldBe("o-2");
    }
}

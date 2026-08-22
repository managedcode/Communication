using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results;
using Shouldly;

namespace ManagedCode.Communication.Tests.Results;

public class PrimitiveFailureTests
{
    private const string CustomNullDetail = "The customer identifier was null.";
    private const string CustomArgumentDetail = "The page size was invalid.";
    private const string CustomRangeDetail = "The page size must be positive.";
    private const string CustomStateDetail = "The order has already shipped.";

    [Test]
    public void Problem_PrimitiveFactories_ShouldCreateStableFailures()
    {
        var nullProblem = Problem.Null();
        var argumentProblem = Problem.Argument();
        var rangeProblem = Problem.OutOfRange();
        var stateProblem = Problem.InvalidState();

        AssertProblem(
            nullProblem,
            ProblemConstants.Titles.NullValue,
            ProblemConstants.Messages.NullValue,
            ProblemConstants.ErrorCodes.Null,
            400);
        AssertProblem(
            argumentProblem,
            ProblemConstants.Titles.InvalidArgument,
            ProblemConstants.Messages.InvalidArgument,
            ProblemConstants.ErrorCodes.InvalidArgument,
            400);
        AssertProblem(
            rangeProblem,
            ProblemConstants.Titles.ArgumentOutOfRange,
            ProblemConstants.Messages.ArgumentOutOfRange,
            ProblemConstants.ErrorCodes.ArgumentOutOfRange,
            400);
        AssertProblem(
            stateProblem,
            ProblemConstants.Titles.InvalidState,
            ProblemConstants.Messages.InvalidState,
            ProblemConstants.ErrorCodes.InvalidState,
            409);
    }

    [Test]
    public void Problem_PrimitiveFactories_WithDetails_ShouldPreserveDetails()
    {
        Problem.Null(CustomNullDetail).Detail.ShouldBe(CustomNullDetail);
        Problem.Argument(CustomArgumentDetail).Detail.ShouldBe(CustomArgumentDetail);
        Problem.OutOfRange(CustomRangeDetail).Detail.ShouldBe(CustomRangeDetail);
        Problem.InvalidState(CustomStateDetail).Detail.ShouldBe(CustomStateDetail);
    }

    [Test]
    public void Result_PrimitiveFactories_ShouldCoverEveryResultShape()
    {
        AssertFailure(Result.FailNull(), ProblemConstants.ErrorCodes.Null, 400);
        AssertFailure(Result.FailArgument(CustomArgumentDetail), ProblemConstants.ErrorCodes.InvalidArgument, 400);
        AssertFailure(Result.FailOutOfRange(CustomRangeDetail), ProblemConstants.ErrorCodes.ArgumentOutOfRange, 400);
        AssertFailure(Result.FailInvalidState(CustomStateDetail), ProblemConstants.ErrorCodes.InvalidState, 409);

        AssertFailure(Result<int>.FailNull(), ProblemConstants.ErrorCodes.Null, 400);
        AssertFailure(Result<int>.FailArgument(CustomArgumentDetail), ProblemConstants.ErrorCodes.InvalidArgument, 400);
        AssertFailure(Result<int>.FailOutOfRange(CustomRangeDetail), ProblemConstants.ErrorCodes.ArgumentOutOfRange, 400);
        AssertFailure(Result<int>.FailInvalidState(CustomStateDetail), ProblemConstants.ErrorCodes.InvalidState, 409);

        AssertFailure(Result.FailNull<int>(), ProblemConstants.ErrorCodes.Null, 400);
        AssertFailure(Result.FailArgument<int>(), ProblemConstants.ErrorCodes.InvalidArgument, 400);
        AssertFailure(Result.FailOutOfRange<int>(), ProblemConstants.ErrorCodes.ArgumentOutOfRange, 400);
        AssertFailure(Result.FailInvalidState<int>(), ProblemConstants.ErrorCodes.InvalidState, 409);

        AssertFailure(CollectionResult<int>.FailNull(), ProblemConstants.ErrorCodes.Null, 400);
        AssertFailure(CollectionResult<int>.FailArgument(), ProblemConstants.ErrorCodes.InvalidArgument, 400);
        AssertFailure(CollectionResult<int>.FailOutOfRange(), ProblemConstants.ErrorCodes.ArgumentOutOfRange, 400);
        AssertFailure(CollectionResult<int>.FailInvalidState(), ProblemConstants.ErrorCodes.InvalidState, 409);
    }

    [Test]
    public void IResultFactory_PrimitiveDefaults_ShouldCreateFailures()
    {
        AssertFailure(CreateNull<Result>(), ProblemConstants.ErrorCodes.Null, 400);
        AssertFailure(CreateArgument<Result<int>>(), ProblemConstants.ErrorCodes.InvalidArgument, 400);
        AssertFailure(CreateOutOfRange<CollectionResult<int>>(), ProblemConstants.ErrorCodes.ArgumentOutOfRange, 400);
        AssertFailure(CreateInvalidState<Result>(), ProblemConstants.ErrorCodes.InvalidState, 409);
    }

    private static TSelf CreateNull<TSelf>()
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.FailNull();
    }

    private static TSelf CreateArgument<TSelf>()
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.FailArgument();
    }

    private static TSelf CreateOutOfRange<TSelf>()
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.FailOutOfRange();
    }

    private static TSelf CreateInvalidState<TSelf>()
        where TSelf : struct, IResultFactory<TSelf>
    {
        return TSelf.FailInvalidState();
    }

    private static void AssertFailure(IResult result, string errorCode, int statusCode)
    {
        result.IsFailed.ShouldBeTrue();
        result.Problem!.ErrorCode.ShouldBe(errorCode);
        result.Problem.StatusCode.ShouldBe(statusCode);
    }

    private static void AssertProblem(Problem problem, string title, string detail, string errorCode, int statusCode)
    {
        problem.Title.ShouldBe(title);
        problem.Detail.ShouldBe(detail);
        problem.ErrorCode.ShouldBe(errorCode);
        problem.StatusCode.ShouldBe(statusCode);
        problem.Type.ShouldBe(ProblemConstants.Types.HttpStatus(statusCode));
    }
}

using System.Linq;
using System.Net;
using ManagedCode.Communication.Constants;
using Shouldly;

namespace ManagedCode.Communication.Tests.Results;

public class ResultRailwayFacadeTests
{
    private const string SharedField = "email";
    private const string EmptyField = "empty";
    private const string FirstMessage = "required";
    private const string SecondMessage = "must be unique";
    private const string ThirdMessage = "invalid format";
    private const string GeneralMessage = "validation incomplete";

    [Test]
    public void Merge_WithFirstFailure_ShouldReturnFirstFailure()
    {
        // Arrange
        var success = Result.Succeed();
        var failure = Result.Fail("First failure", "Pipeline stopped", HttpStatusCode.BadRequest);
        var laterFailure = Result.Fail("Second failure", "Should not be returned", HttpStatusCode.InternalServerError);

        // Act
        var result = Result.Merge(success, failure, laterFailure);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Title.ShouldBe("First failure");
        result.Problem.StatusCode.ShouldBe(400);
    }

    [Test]
    public void MergeAll_WithOneDefaultFailure_PreservesOriginalResult()
    {
        var merged = Result.MergeAll(Result.Succeed(), default, Result.Succeed());

        merged.IsFailed.ShouldBeTrue();
        merged.GetProblemNoFallback().ShouldBeNull();
        merged.Problem.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Test]
    public void MergeAll_WithMixedFailures_ShouldReturnAggregateProblem()
    {
        // Arrange
        var unauthorized = Result.Fail("Unauthorized", "Auth required", HttpStatusCode.Unauthorized);
        var forbidden = Result.Fail("Forbidden", "Access denied", HttpStatusCode.Forbidden);

        // Act
        var result = Result.MergeAll(unauthorized, forbidden);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(500);
        result.Problem.TryGetExtension(ProblemConstants.ExtensionKeys.Errors, out Problem[]? errors).ShouldBeTrue();
        errors.ShouldNotBeNull();
        errors.Length.ShouldBe(2);
        errors.Select(problem => problem.StatusCode).ShouldContain(401);
        errors.Select(problem => problem.StatusCode).ShouldContain(403);
    }

    [Test]
    public void Combine_WithSuccessfulValues_ShouldReturnCollection()
    {
        // Arrange
        var one = Result<int>.Succeed(1);
        var two = Result<int>.Succeed(2);
        var three = Result<int>.Succeed(3);

        // Act
        var result = Result.Combine(one, two, three);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Collection.ShouldBeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public void AggregatingValidationProblems_PreservesMessageOrderAndCopiesLists(bool combine)
    {
        var first = Problem.Validation((SharedField, FirstMessage));
        first.GetValidationErrors()![EmptyField] = [];
        var second = Problem.Validation((SharedField, SecondMessage), (SharedField, ThirdMessage));
        var last = Problem.Validation((SharedField, FirstMessage));
        var empty = Problem.Validation();
        empty.Detail = GeneralMessage;

        var problem = combine
            ? Result.CombineAll(
                Result<int>.Fail(first), Result<int>.Fail(second), Result<int>.Fail(last), Result<int>.Fail(empty)).Problem
            : Result.MergeAll(Result.Fail(first), Result.Fail(second), Result.Fail(last), Result.Fail(empty)).Problem;
        first.AddValidationError(SharedField, GeneralMessage);

        problem.ShouldNotBeNull();
        problem.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        var errors = problem.GetValidationErrors();
        errors.ShouldNotBeNull();
        errors.Count.ShouldBe(2);
        errors[SharedField].ShouldBe([FirstMessage, SecondMessage, ThirdMessage, FirstMessage]);
        errors[ProblemConstants.ValidationFields.General].ShouldBe([GeneralMessage]);
    }

    [Test]
    public void CombineAll_WithMixedFailures_ShouldReturnAggregateProblem()
    {
        // Arrange
        var success = Result<string>.Succeed("ok");
        var validation = Result<string>.FailValidation(("email", "Invalid email"));
        var unauthorized = Result<string>.Fail("Unauthorized", "Auth required", HttpStatusCode.Unauthorized);

        // Act
        var result = Result.CombineAll(success, validation, unauthorized);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(500);
        result.Problem.TryGetExtension(ProblemConstants.ExtensionKeys.Errors, out Problem[]? errors).ShouldBeTrue();
        errors.ShouldNotBeNull();
        errors.Length.ShouldBe(2);
        errors.Select(problem => problem.StatusCode).ShouldContain(400);
        errors.Select(problem => problem.StatusCode).ShouldContain(401);
    }
}

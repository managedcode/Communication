using System.Linq;
using System.Net;
using ManagedCode.Communication.Constants;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ResultRailwayFacadeTests
{
    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

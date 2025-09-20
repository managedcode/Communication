using System;
using System.Collections.Generic;
using System.Net;
using Shouldly;
using ManagedCode.Communication.CollectionResultT;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Results;

public class CollectionResultTests
{
    [Fact]
    public void Succeed_WithArray_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var items = new[] { "item1", "item2", "item3" };

        // Act
        var result = CollectionResult<string>.Succeed(items, pageNumber: 1, pageSize: 10, totalItems: 3);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.IsFailed
            .ShouldBeFalse();
        result.Collection
            .ShouldBeEquivalentTo(items);
        result.PageNumber
            .ShouldBe(1);
        result.PageSize
            .ShouldBe(10);
        result.TotalItems
            .ShouldBe(3);
        result.TotalPages
            .ShouldBe(1);
        result.HasItems
            .ShouldBeTrue();
        result.IsEmpty
            .ShouldBeFalse();
        result.Problem
            .ShouldBeNull();
    }

    [Fact]
    public void Succeed_WithEnumerable_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = CollectionResult<string>.Succeed(items, pageNumber: 1, pageSize: 10, totalItems: 3);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Collection
            .ShouldBeEquivalentTo(items);
        result.PageNumber
            .ShouldBe(1);
        result.PageSize
            .ShouldBe(10);
        result.TotalItems
            .ShouldBe(3);
    }

    [Fact]
    public void Succeed_WithArrayOnly_ShouldCalculatePagingInfo()
    {
        // Arrange
        var items = new[] { "item1", "item2", "item3" };

        // Act
        var result = CollectionResult<string>.Succeed(items);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Collection
            .ShouldBeEquivalentTo(items);
        result.PageNumber
            .ShouldBe(1);
        result.PageSize
            .ShouldBe(3);
        result.TotalItems
            .ShouldBe(3);
        result.TotalPages
            .ShouldBe(1);
    }

    [Fact]
    public void Succeed_WithEnumerableOnly_ShouldCalculatePagingInfo()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = CollectionResult<string>.Succeed(items);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Collection
            .ShouldBeEquivalentTo(items);
        result.PageNumber
            .ShouldBe(1);
        result.PageSize
            .ShouldBe(3);
        result.TotalItems
            .ShouldBe(3);
        result.TotalPages
            .ShouldBe(1);
    }

    [Fact]
    public void Empty_ShouldCreateEmptyResult()
    {
        // Act
        var result = CollectionResult<string>.Empty();

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Collection
            .ShouldBeEmpty();
        result.PageNumber
            .ShouldBe(0);
        result.PageSize
            .ShouldBe(0);
        result.TotalItems
            .ShouldBe(0);
        result.TotalPages
            .ShouldBe(0);
        result.HasItems
            .ShouldBeFalse();
        result.IsEmpty
            .ShouldBeTrue();
    }

    [Fact]
    public void Fail_WithMessage_ShouldCreateFailedResult()
    {
        // Arrange
        const string title = "Operation failed";
        const string detail = "Something went wrong";

        // Act
        var result = CollectionResult<string>.Fail(title, detail, HttpStatusCode.BadRequest);

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
        result.IsFailed
            .ShouldBeTrue();
        result.Collection
            .ShouldBeEmpty();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.Title
            .ShouldBe(title);
        result.Problem
            .Detail
            .ShouldBe(detail);
        result.Problem
            .StatusCode
            .ShouldBe(400);
    }

    [Fact]
    public void Fail_WithProblem_ShouldCreateFailedResult()
    {
        // Arrange
        var problem = Problem.Create("Bad Request", "Invalid input", 400, "https://httpstatuses.io/400");

        // Act
        var result = CollectionResult<string>.Fail(problem);

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
        result.IsFailed
            .ShouldBeTrue();
        result.Collection
            .ShouldBeEmpty();
        result.Problem
            .ShouldBe(problem);
    }

    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var result1 = CollectionResult<string>.Succeed(new[] { "item1" }, pageNumber: 1, pageSize: 10, totalItems: 25);
        var result2 = CollectionResult<string>.Succeed(new[] { "item1" }, pageNumber: 1, pageSize: 10, totalItems: 30);
        var result3 = CollectionResult<string>.Succeed(new[] { "item1" }, pageNumber: 1, pageSize: 10, totalItems: 10);

        // Assert
        result1.TotalPages
            .ShouldBe(3); // 25 items / 10 per page = 3 pages
        result2.TotalPages
            .ShouldBe(3); // 30 items / 10 per page = 3 pages
        result3.TotalPages
            .ShouldBe(1); // 10 items / 10 per page = 1 page
    }

    [Fact]
    public void InvalidField_WithValidationProblem_ShouldReturnCorrectResult()
    {
        // Arrange
        var result = CollectionResult<string>.FailValidation(("email", "Email is required"), ("age", "Age must be greater than 0"));

        // Act & Assert
        result.InvalidField("email")
            .ShouldBeTrue();
        result.InvalidField("age")
            .ShouldBeTrue();
        result.InvalidField("name")
            .ShouldBeFalse();
    }

    [Fact]
    public void InvalidFieldError_WithValidationProblem_ShouldReturnErrorMessage()
    {
        // Arrange
        var result = CollectionResult<string>.FailValidation(("email", "Email is required"), ("email", "Email format is invalid"),
            ("age", "Age must be greater than 0"));

        // Act
        var emailErrors = result.InvalidFieldError("email");
        var ageErrors = result.InvalidFieldError("age");
        var nameErrors = result.InvalidFieldError("name");

        // Assert
        emailErrors.ShouldContain("Email is required");
        emailErrors.ShouldContain("Email format is invalid");
        ageErrors.ShouldBe("Age must be greater than 0");
        nameErrors.ShouldBeEmpty();
    }

    [Fact]
    public void ThrowIfFail_WithSuccessfulResult_ShouldNotThrow()
    {
        // Arrange
        var result = CollectionResult<string>.Succeed(new[] { "item1" });

        // Act & Assert
        Should.NotThrow(() => result.ThrowIfFail());
    }

    [Fact]
    public void ThrowIfFail_WithFailedResult_ShouldThrow()
    {
        // Arrange
        var result = CollectionResult<string>.Fail("Operation failed", "Something went wrong", HttpStatusCode.BadRequest);

        // Act & Assert
        var exception = Should.Throw<ProblemException>(() => result.ThrowIfFail());
        exception.Problem.Title.ShouldBe("Operation failed");
    }

    [Fact]
    public void ImplicitOperator_ToBool_ShouldReturnIsSuccess()
    {
        // Arrange
        var successResult = CollectionResult<string>.Succeed(new[] { "item1" });
        var failResult = CollectionResult<string>.Fail("Failed", "Failed", HttpStatusCode.BadRequest);

        // Act & Assert
        ((bool)successResult).ShouldBeTrue();
        ((bool)failResult).ShouldBeFalse();
    }

    [Fact]
    public void TryGetProblem_WithSuccessfulResult_ShouldReturnFalse()
    {
        // Arrange
        var result = CollectionResult<string>.Succeed(new[] { "item1", "item2" });

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.ShouldBeFalse();
        problem.ShouldBeNull();
    }

    [Fact]
    public void TryGetProblem_WithFailedResult_ShouldReturnTrueAndProblem()
    {
        // Arrange
        var expectedProblem = Problem.Create("Service Unavailable", "Service is temporarily unavailable", 503, "https://httpstatuses.io/503");
        var result = CollectionResult<string>.Fail(expectedProblem);

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.ShouldBeTrue();
        problem.ShouldNotBeNull();
        problem.ShouldBe(expectedProblem);
    }

    [Fact]
    public void TryGetProblem_WithEmptyCollection_ButSuccessful_ShouldReturnFalse()
    {
        // Arrange
        var result = CollectionResult<string>.Empty();

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.ShouldBeFalse();
        problem.ShouldBeNull();
        result.IsEmpty.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ThrowIfFail_WithProblemException_ShouldPreserveProblemDetails()
    {
        // Arrange
        var problem = Problem.Create("Too Many Requests", "Rate limit exceeded", 429, "https://httpstatuses.io/429");
        problem.Extensions["retryAfter"] = 60;
        var result = CollectionResult<string>.Fail(problem);

        // Act & Assert
        var exception = Should.Throw<ProblemException>(() => result.ThrowIfFail());

        exception.Problem.ShouldBe(problem);
        exception.Problem.Extensions["retryAfter"].ShouldBe(60);
    }

    [Fact]
    public void ThrowIfFail_WithValidationFailure_ShouldThrowWithValidationDetails()
    {
        // Arrange
        var result = CollectionResult<string>.FailValidation(("filter", "Invalid filter format"), ("pageSize", "Page size must be between 1 and 100"));

        // Act & Assert
        var exception = Should.Throw<ProblemException>(() => result.ThrowIfFail());

        exception.Problem.Title.ShouldBe("Validation Failed");
        exception.Problem.StatusCode.ShouldBe(400);

        var validationErrors = exception.Problem.GetValidationErrors();
        validationErrors.ShouldNotBeNull();
        validationErrors!["filter"].ShouldContain("Invalid filter format");
        validationErrors!["pageSize"].ShouldContain("Page size must be between 1 and 100");
    }
}

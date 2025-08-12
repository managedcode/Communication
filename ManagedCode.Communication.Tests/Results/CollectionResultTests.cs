using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using ManagedCode.Communication.CollectionResultT;
using Xunit;

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
            .Should()
            .BeTrue();
        result.IsFailed
            .Should()
            .BeFalse();
        result.Collection
            .Should()
            .BeEquivalentTo(items);
        result.PageNumber
            .Should()
            .Be(1);
        result.PageSize
            .Should()
            .Be(10);
        result.TotalItems
            .Should()
            .Be(3);
        result.TotalPages
            .Should()
            .Be(1);
        result.HasItems
            .Should()
            .BeTrue();
        result.IsEmpty
            .Should()
            .BeFalse();
        result.Problem
            .Should()
            .BeNull();
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
            .Should()
            .BeTrue();
        result.Collection
            .Should()
            .BeEquivalentTo(items);
        result.PageNumber
            .Should()
            .Be(1);
        result.PageSize
            .Should()
            .Be(10);
        result.TotalItems
            .Should()
            .Be(3);
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
            .Should()
            .BeTrue();
        result.Collection
            .Should()
            .BeEquivalentTo(items);
        result.PageNumber
            .Should()
            .Be(1);
        result.PageSize
            .Should()
            .Be(3);
        result.TotalItems
            .Should()
            .Be(3);
        result.TotalPages
            .Should()
            .Be(1);
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
            .Should()
            .BeTrue();
        result.Collection
            .Should()
            .BeEquivalentTo(items);
        result.PageNumber
            .Should()
            .Be(1);
        result.PageSize
            .Should()
            .Be(3);
        result.TotalItems
            .Should()
            .Be(3);
        result.TotalPages
            .Should()
            .Be(1);
    }

    [Fact]
    public void Empty_ShouldCreateEmptyResult()
    {
        // Act
        var result = CollectionResult<string>.Empty();

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();
        result.Collection
            .Should()
            .BeEmpty();
        result.PageNumber
            .Should()
            .Be(1);
        result.PageSize
            .Should()
            .Be(0);
        result.TotalItems
            .Should()
            .Be(0);
        result.TotalPages
            .Should()
            .Be(0);
        result.HasItems
            .Should()
            .BeFalse();
        result.IsEmpty
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Empty_WithPaging_ShouldCreateEmptyResultWithPaging()
    {
        // Act
        var result = CollectionResult<string>.Empty(pageNumber: 2, pageSize: 10);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();
        result.Collection
            .Should()
            .BeEmpty();
        result.PageNumber
            .Should()
            .Be(2);
        result.PageSize
            .Should()
            .Be(10);
        result.TotalItems
            .Should()
            .Be(0);
        result.TotalPages
            .Should()
            .Be(0);
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
            .Should()
            .BeFalse();
        result.IsFailed
            .Should()
            .BeTrue();
        result.Collection
            .Should()
            .BeEmpty();
        result.Problem
            .Should()
            .NotBeNull();
        result.Problem!.Title
            .Should()
            .Be(title);
        result.Problem
            .Detail
            .Should()
            .Be(detail);
        result.Problem
            .StatusCode
            .Should()
            .Be(400);
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
            .Should()
            .BeFalse();
        result.IsFailed
            .Should()
            .BeTrue();
        result.Collection
            .Should()
            .BeEmpty();
        result.Problem
            .Should()
            .Be(problem);
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
            .Should()
            .Be(3); // 25 items / 10 per page = 3 pages
        result2.TotalPages
            .Should()
            .Be(3); // 30 items / 10 per page = 3 pages
        result3.TotalPages
            .Should()
            .Be(1); // 10 items / 10 per page = 1 page
    }

    [Fact]
    public void InvalidField_WithValidationProblem_ShouldReturnCorrectResult()
    {
        // Arrange
        var result = CollectionResult<string>.FailValidation(("email", "Email is required"), ("age", "Age must be greater than 0"));

        // Act & Assert
        result.InvalidField("email")
            .Should()
            .BeTrue();
        result.InvalidField("age")
            .Should()
            .BeTrue();
        result.InvalidField("name")
            .Should()
            .BeFalse();
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
        emailErrors.Should()
            .Contain("Email is required");
        emailErrors.Should()
            .Contain("Email format is invalid");
        ageErrors.Should()
            .Be("Age must be greater than 0");
        nameErrors.Should()
            .BeEmpty();
    }

    [Fact]
    public void AddInvalidMessage_ShouldAddValidationError()
    {
        // Arrange
        var result = CollectionResult<string>.Empty();

        // Act
        result.AddInvalidMessage("email", "Email is required");
        result.AddInvalidMessage("email", "Email format is invalid");

        // Assert
        result.InvalidField("email")
            .Should()
            .BeTrue();
        var emailErrors = result.InvalidFieldError("email");
        emailErrors.Should()
            .Contain("Email is required");
        emailErrors.Should()
            .Contain("Email format is invalid");
    }

    [Fact]
    public void AddInvalidMessage_WithGeneralMessage_ShouldAddToGeneralErrors()
    {
        // Arrange
        var result = CollectionResult<string>.Empty();

        // Act
        result.AddInvalidMessage("General error occurred");

        // Assert
        result.InvalidField("_general")
            .Should()
            .BeTrue();
        var generalErrors = result.InvalidFieldError("_general");
        generalErrors.Should()
            .Be("General error occurred");
    }

    [Fact]
    public void ThrowIfFail_WithSuccessfulResult_ShouldNotThrow()
    {
        // Arrange
        var result = CollectionResult<string>.Succeed(new[] { "item1" });

        // Act & Assert
        result.Invoking(r => r.ThrowIfFail())
            .Should()
            .NotThrow();
    }

    [Fact]
    public void ThrowIfFail_WithFailedResult_ShouldThrow()
    {
        // Arrange
        var result = CollectionResult<string>.Fail("Operation failed", "Something went wrong", HttpStatusCode.BadRequest);

        // Act & Assert
        result.Invoking(r => r.ThrowIfFail())
            .Should()
            .Throw<ProblemException>()
            .Which.Problem.Title
            .Should()
            .Be("Operation failed");
    }

    [Fact]
    public void ImplicitOperator_ToBool_ShouldReturnIsSuccess()
    {
        // Arrange
        var successResult = CollectionResult<string>.Succeed(new[] { "item1" });
        var failResult = CollectionResult<string>.Fail("Failed", "Failed", HttpStatusCode.BadRequest);

        // Act & Assert
        ((bool)successResult).Should()
            .BeTrue();
        ((bool)failResult).Should()
            .BeFalse();
    }

    [Fact]
    public void TryGetProblem_WithSuccessfulResult_ShouldReturnFalse()
    {
        // Arrange
        var result = CollectionResult<string>.Succeed(new[] { "item1", "item2" });

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.Should().BeFalse();
        problem.Should().BeNull();
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
        hasProblem.Should().BeTrue();
        problem.Should().NotBeNull();
        problem.Should().Be(expectedProblem);
    }

    [Fact]
    public void TryGetProblem_WithEmptyCollection_ButSuccessful_ShouldReturnFalse()
    {
        // Arrange
        var result = CollectionResult<string>.Empty();

        // Act
        var hasProblem = result.TryGetProblem(out var problem);

        // Assert
        hasProblem.Should().BeFalse();
        problem.Should().BeNull();
        result.IsEmpty.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ThrowIfFail_WithProblemException_ShouldPreserveProblemDetails()
    {
        // Arrange
        var problem = Problem.Create("Too Many Requests", "Rate limit exceeded", 429, "https://httpstatuses.io/429");
        problem.Extensions["retryAfter"] = 60;
        var result = CollectionResult<string>.Fail(problem);

        // Act & Assert
        var exception = result.Invoking(r => r.ThrowIfFail())
            .Should()
            .Throw<ProblemException>()
            .Which;

        exception.Problem.Should().BeEquivalentTo(problem);
        exception.Problem.Extensions["retryAfter"].Should().Be(60);
    }

    [Fact]
    public void ThrowIfFail_WithValidationFailure_ShouldThrowWithValidationDetails()
    {
        // Arrange
        var result = CollectionResult<string>.FailValidation(("filter", "Invalid filter format"), ("pageSize", "Page size must be between 1 and 100"));

        // Act & Assert
        var exception = result.Invoking(r => r.ThrowIfFail())
            .Should()
            .Throw<ProblemException>()
            .Which;

        exception.Problem.Title.Should().Be("Validation Failed");
        exception.Problem.StatusCode.Should().Be(400);
        
        var validationErrors = exception.Problem.GetValidationErrors();
        validationErrors.Should().NotBeNull();
        validationErrors!["filter"].Should().Contain("Invalid filter format");
        validationErrors!["pageSize"].Should().Contain("Page size must be between 1 and 100");
    }
}
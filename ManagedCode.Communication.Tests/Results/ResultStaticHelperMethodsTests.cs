using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using ManagedCode.Communication.Constants;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Results;

public class ResultStaticHelperMethodsTests
{
    #region Fail<T> Helper Methods

    [Fact]
    public void Result_FailT_NoParameters_ShouldCreateFailedResultT()
    {
        // Act
        var result = Result.Fail<string>();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.HasProblem.Should().BeTrue();
    }

    [Fact]
    public void Result_FailT_WithMessage_ShouldCreateFailedResultTWithProblem()
    {
        // Arrange
        const string message = "Operation failed";

        // Act
        var result = Result.Fail<int>(message);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle(message);
        result.ShouldHaveProblem().WithDetail(message);
        result.ShouldHaveProblem().WithStatusCode(500);
        result.Value.Should().Be(0);
    }

    [Fact]
    public void Result_FailT_WithProblem_ShouldCreateFailedResultTWithProblem()
    {
        // Arrange
        var problem = Problem.Create("Custom Error", "Custom Detail", 400);

        // Act
        var result = Result.Fail<User>(problem);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.Problem.Should().Be(problem);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Result_FailT_WithEnum_ShouldCreateFailedResultTWithErrorCode()
    {
        // Act
        var result = Result.Fail<string, TestError>(TestError.InvalidInput);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithErrorCode("InvalidInput");
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Result_FailT_WithEnumAndDetail_ShouldCreateFailedResultTWithErrorCodeAndDetail()
    {
        // Arrange
        const string detail = "Invalid input provided";

        // Act
        var result = Result.Fail<int, TestError>(TestError.ValidationFailed, detail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithErrorCode("ValidationFailed");
        result.ShouldHaveProblem().WithDetail(detail);
        result.Value.Should().Be(0);
    }

    [Fact]
    public void Result_FailT_WithException_ShouldCreateFailedResultTWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result.Fail<string>(exception);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("InvalidOperationException");
        result.ShouldHaveProblem().WithDetail("Test exception");
        result.ShouldHaveProblem().WithStatusCode(500);
        result.Value.Should().BeNull();
    }

    #endregion

    #region FailValidation<T> Helper Methods

    [Fact]
    public void Result_FailValidationT_WithSingleError_ShouldCreateValidationFailedResultT()
    {
        // Act
        var result = Result.FailValidation<string>(("email", "Email is required"));

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(400);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.ValidationFailed);
        var errors = result.AssertValidationErrors();
        errors["email"].Should().Contain("Email is required");
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Result_FailValidationT_WithMultipleErrors_ShouldCreateValidationFailedResultTWithAllErrors()
    {
        // Act
        var result = Result.FailValidation<User>(
            ("name", "Name is required"),
            ("email", "Invalid email format"),
            ("age", "Must be 18 or older")
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(400);
        var errors = result.AssertValidationErrors();
        errors.Should().HaveCount(3);
        errors["name"].Should().Contain("Name is required");
        errors["email"].Should().Contain("Invalid email format");
        errors["age"].Should().Contain("Must be 18 or older");
        result.Value.Should().BeNull();
    }

    #endregion

    #region FailUnauthorized<T> Helper Methods

    [Fact]
    public void Result_FailUnauthorizedT_NoParameters_ShouldCreateUnauthorizedResultT()
    {
        // Act
        var result = Result.FailUnauthorized<int>();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(401);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Unauthorized);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.UnauthorizedAccess);
        result.Value.Should().Be(0);
    }

    [Fact]
    public void Result_FailUnauthorizedT_WithDetail_ShouldCreateUnauthorizedResultTWithCustomDetail()
    {
        // Arrange
        const string detail = "Session expired";

        // Act
        var result = Result.FailUnauthorized<string>(detail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(401);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Unauthorized);
        result.ShouldHaveProblem().WithDetail(detail);
        result.Value.Should().BeNull();
    }

    #endregion

    #region FailForbidden<T> Helper Methods

    [Fact]
    public void Result_FailForbiddenT_NoParameters_ShouldCreateForbiddenResultT()
    {
        // Act
        var result = Result.FailForbidden<User>();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(403);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Forbidden);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.ForbiddenAccess);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Result_FailForbiddenT_WithDetail_ShouldCreateForbiddenResultTWithCustomDetail()
    {
        // Arrange
        const string detail = "Admin access required";

        // Act
        var result = Result.FailForbidden<List<string>>(detail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(403);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Forbidden);
        result.ShouldHaveProblem().WithDetail(detail);
        result.Value.Should().BeNull();
    }

    #endregion

    #region FailNotFound<T> Helper Methods

    [Fact]
    public void Result_FailNotFoundT_NoParameters_ShouldCreateNotFoundResultT()
    {
        // Act
        var result = Result.FailNotFound<User>();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.NotFound);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.ResourceNotFound);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Result_FailNotFoundT_WithDetail_ShouldCreateNotFoundResultTWithCustomDetail()
    {
        // Arrange
        const string detail = "Product with ID 456 not found";

        // Act
        var result = Result.FailNotFound<Product>(detail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.NotFound);
        result.ShouldHaveProblem().WithDetail(detail);
        result.Value.Should().BeNull();
    }

    #endregion

    #region Complex Type Tests

    [Fact]
    public void Result_StaticHelpers_WithComplexTypes_ShouldWorkCorrectly()
    {
        // Act
        var result1 = Result.Fail<Dictionary<string, int>>("Dictionary creation failed");
        var result2 = Result.FailValidation<List<User>>(("users", "Invalid user list"));
        var result3 = Result.FailNotFound<Tuple<int, string, bool>>();
        var result4 = Result.Fail<User, TestError>(TestError.SystemError);

        // Assert
        result1.IsFailed.Should().BeTrue();
        result1.Value.Should().BeNull();

        result2.IsFailed.Should().BeTrue();
        result2.Value.Should().BeNull();

        result3.IsFailed.Should().BeTrue();
        result3.Value.Should().BeNull();

        result4.IsFailed.Should().BeTrue();
        result4.Value.Should().BeNull();
    }

    [Fact]
    public void Result_StaticHelpers_ChainedCalls_ShouldMaintainFailureState()
    {
        // Act
        var problem = Problem.Create("Initial Error", "Initial Detail", 500);
        var result1 = Result.Fail<int>(problem);
        var result2 = result1.IsFailed ? Result.Fail<string>(result1.Problem!) : Result<string>.Succeed("Success");
        var result3 = result2.IsFailed ? Result.Fail<bool>(result2.Problem!) : Result<bool>.Succeed(true);

        // Assert
        result1.IsFailed.Should().BeTrue();
        result2.IsFailed.Should().BeTrue();
        result3.IsFailed.Should().BeTrue();
        result3.Problem!.Title.Should().Be("Initial Error");
        result3.Problem.Detail.Should().Be("Initial Detail");
    }

    #endregion

    #region Test Helpers

    private enum TestError
    {
        InvalidInput,
        ValidationFailed,
        SystemError
    }

    private class User
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }

    #endregion
}
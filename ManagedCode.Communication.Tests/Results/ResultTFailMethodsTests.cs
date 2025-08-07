using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using ManagedCode.Communication.Constants;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Results;

public class ResultTFailMethodsTests
{
    #region Fail() Tests

    [Fact]
    public void ResultT_Fail_NoParameters_ShouldCreateFailedResult()
    {
        // Act
        var result = Result<string>.Fail();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.HasProblem.Should().BeFalse();
    }

    #endregion

    #region Fail(T value) Tests

    [Fact]
    public void ResultT_Fail_WithValue_ShouldCreateFailedResultWithValue()
    {
        // Arrange
        const int value = 42;

        // Act
        var result = Result<int>.Fail(value);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Value.Should().Be(value);
        result.HasProblem.Should().BeFalse();
    }

    [Fact]
    public void ResultT_Fail_WithNullValue_ShouldCreateFailedResultWithNull()
    {
        // Act
        User? nullUser = null;
        var result = Result<User>.Fail(nullUser!);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Value.Should().BeNull();
        result.HasProblem.Should().BeFalse();
    }

    #endregion

    #region Fail(Problem) Tests

    [Fact]
    public void ResultT_Fail_WithProblem_ShouldCreateFailedResultWithProblem()
    {
        // Arrange
        var problem = Problem.Create("Test Error", "Test Detail", 400);

        // Act
        var result = Result<int>.Fail(problem);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Value.Should().Be(0);
        result.Problem.Should().Be(problem);
        result.ShouldHaveProblem().WithTitle("Test Error");
        result.ShouldHaveProblem().WithDetail("Test Detail").WithStatusCode(400);
    }

    #endregion

    #region Fail(string title) Tests

    [Fact]
    public void ResultT_Fail_WithTitle_ShouldCreateFailedResultWithInternalServerError()
    {
        // Arrange
        const string title = "Operation Failed";

        // Act
        var result = Result<User>.Fail(title);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle(title);
        result.ShouldHaveProblem().WithDetail(title);
        result.ShouldHaveProblem().WithStatusCode(500);
    }

    #endregion

    #region Fail(string title, string detail) Tests

    [Fact]
    public void ResultT_Fail_WithTitleAndDetail_ShouldCreateFailedResultWithDefaultStatus()
    {
        // Arrange
        const string title = "Validation Error";
        const string detail = "The input is invalid";

        // Act
        var result = Result<int>.Fail(title, detail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle(title);
        result.ShouldHaveProblem().WithDetail(detail);
        result.ShouldHaveProblem().WithStatusCode(500);
    }

    #endregion

    #region Fail(string title, string detail, HttpStatusCode status) Tests

    [Fact]
    public void ResultT_Fail_WithTitleDetailAndStatus_ShouldCreateFailedResultWithSpecifiedStatus()
    {
        // Arrange
        const string title = "Not Found";
        const string detail = "Resource does not exist";
        const HttpStatusCode status = HttpStatusCode.NotFound;

        // Act
        var result = Result<string>.Fail(title, detail, status);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle(title);
        result.ShouldHaveProblem().WithDetail(detail);
        result.ShouldHaveProblem().WithStatusCode(404);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 400)]
    [InlineData(HttpStatusCode.Unauthorized, 401)]
    [InlineData(HttpStatusCode.Forbidden, 403)]
    [InlineData(HttpStatusCode.NotFound, 404)]
    [InlineData(HttpStatusCode.InternalServerError, 500)]
    public void ResultT_Fail_WithVariousStatusCodes_ShouldSetCorrectStatusCode(HttpStatusCode statusCode, int expectedCode)
    {
        // Act
        var result = Result<int>.Fail("Error", "Detail", statusCode);

        // Assert
        result.ShouldHaveProblem().WithStatusCode(expectedCode);
    }

    #endregion

    #region Fail(Exception) Tests

    [Fact]
    public void ResultT_Fail_WithException_ShouldCreateFailedResultWithInternalServerError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result<string>.Fail(exception);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("InvalidOperationException");
        result.ShouldHaveProblem().WithDetail("Test exception");
        result.ShouldHaveProblem().WithStatusCode(500);
        result.ShouldHaveProblem().WithErrorCode(exception.GetType().FullName ?? exception.GetType().Name);
    }

    [Fact]
    public void ResultT_Fail_WithInnerException_ShouldPreserveExceptionInfo()
    {
        // Arrange
        var innerException = new ArgumentNullException("param", "Parameter is null");
        var exception = new InvalidOperationException("Outer exception", innerException);

        // Act
        var result = Result<int>.Fail(exception);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("InvalidOperationException");
        result.ShouldHaveProblem().WithDetail("Outer exception");
    }

    #endregion

    #region Fail(Exception, HttpStatusCode) Tests

    [Fact]
    public void ResultT_Fail_WithExceptionAndStatus_ShouldCreateFailedResultWithSpecifiedStatus()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");
        const HttpStatusCode status = HttpStatusCode.Forbidden;

        // Act
        var result = Result<User>.Fail(exception, status);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("UnauthorizedAccessException");
        result.ShouldHaveProblem().WithDetail("Access denied");
        result.ShouldHaveProblem().WithStatusCode(403);
    }

    #endregion

    #region FailValidation Tests

    [Fact]
    public void ResultT_FailValidation_WithSingleError_ShouldCreateValidationFailedResult()
    {
        // Act
        var result = Result<string>.FailValidation(("email", "Email is required"));

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(400);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.ValidationFailed);
        var errors = result.AssertValidationErrors();
        errors.Should().ContainKey("email");
        errors["email"].Should().Contain("Email is required");
    }

    [Fact]
    public void ResultT_FailValidation_WithMultipleErrors_ShouldCreateValidationFailedResultWithAllErrors()
    {
        // Act
        var result = Result<User>.FailValidation(
            ("name", "Name is required"),
            ("email", "Invalid email format"),
            ("age", "Must be 18 or older")
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(400);
        var errors = result.AssertValidationErrors();
        errors.Should().NotBeNull();
        errors.Should().HaveCount(3);
        errors["name"].Should().Contain("Name is required");
        errors["email"].Should().Contain("Invalid email format");
        errors["age"].Should().Contain("Must be 18 or older");
    }

    [Fact]
    public void ResultT_FailValidation_WithDuplicateFields_ShouldCombineErrors()
    {
        // Act
        var result = Result<int>.FailValidation(
            ("password", "Too short"),
            ("password", "Must contain numbers"),
            ("password", "Must contain special characters")
        );

        // Assert
        result.IsFailed.Should().BeTrue();
        var errors = result.AssertValidationErrors();
        errors["password"].Should().HaveCount(3);
        errors["password"].Should().Contain("Too short");
        errors["password"].Should().Contain("Must contain numbers");
        errors["password"].Should().Contain("Must contain special characters");
    }

    #endregion

    #region FailUnauthorized Tests

    [Fact]
    public void ResultT_FailUnauthorized_NoParameters_ShouldCreateUnauthorizedResult()
    {
        // Act
        var result = Result<string>.FailUnauthorized();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(401);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Unauthorized);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.UnauthorizedAccess);
    }

    [Fact]
    public void ResultT_FailUnauthorized_WithDetail_ShouldCreateUnauthorizedResultWithCustomDetail()
    {
        // Arrange
        const string detail = "Invalid API key";

        // Act
        var result = Result<int>.FailUnauthorized(detail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(401);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Unauthorized);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    #endregion

    #region FailForbidden Tests

    [Fact]
    public void ResultT_FailForbidden_NoParameters_ShouldCreateForbiddenResult()
    {
        // Act
        var result = Result<User>.FailForbidden();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(403);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Forbidden);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.ForbiddenAccess);
    }

    [Fact]
    public void ResultT_FailForbidden_WithDetail_ShouldCreateForbiddenResultWithCustomDetail()
    {
        // Arrange
        const string detail = "Insufficient permissions to perform this action";

        // Act
        var result = Result<string>.FailForbidden(detail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(403);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Forbidden);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    #endregion

    #region FailNotFound Tests

    [Fact]
    public void ResultT_FailNotFound_NoParameters_ShouldCreateNotFoundResult()
    {
        // Act
        var result = Result<User>.FailNotFound();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.NotFound);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.ResourceNotFound);
    }

    [Fact]
    public void ResultT_FailNotFound_WithDetail_ShouldCreateNotFoundResultWithCustomDetail()
    {
        // Arrange
        const string detail = "User with ID 123 not found";

        // Act
        var result = Result<User>.FailNotFound(detail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.NotFound);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    #endregion

    #region Fail<TEnum> Tests

    [Fact]
    public void ResultT_Fail_WithEnum_ShouldCreateFailedResultWithErrorCode()
    {
        // Act
        var result = Result<string>.Fail(TestError.InvalidInput);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithErrorCode("InvalidInput");
        result.ShouldHaveProblem().WithStatusCode(400); // Default for domain errors
    }

    [Fact]
    public void ResultT_Fail_WithEnumAndDetail_ShouldCreateFailedResultWithErrorCodeAndDetail()
    {
        // Arrange
        const string detail = "The input value is not valid";

        // Act
        var result = Result<int>.Fail(TestError.ValidationFailed, detail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithErrorCode("ValidationFailed");
        result.ShouldHaveProblem().WithDetail(detail);
        result.ShouldHaveProblem().WithStatusCode(400);
    }

    [Fact]
    public void ResultT_Fail_WithEnumAndStatus_ShouldCreateFailedResultWithErrorCodeAndStatus()
    {
        // Act
        var result = Result<User>.Fail(TestError.SystemError, HttpStatusCode.InternalServerError);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithErrorCode("SystemError");
        result.ShouldHaveProblem().WithTitle("SystemError");
        result.ShouldHaveProblem().WithStatusCode(500);
    }

    [Fact]
    public void ResultT_Fail_WithEnumDetailAndStatus_ShouldCreateFailedResultWithAllSpecified()
    {
        // Arrange
        const string detail = "Database connection failed";
        const HttpStatusCode status = HttpStatusCode.ServiceUnavailable;

        // Act
        var result = Result<string>.Fail(TestError.DatabaseError, detail, status);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithErrorCode("DatabaseError");
        result.ShouldHaveProblem().WithDetail(detail);
        result.ShouldHaveProblem().WithStatusCode(503);
    }

    [Fact]
    public void ResultT_Fail_WithHttpStatusEnum_ShouldUseEnumValueAsStatusCode()
    {
        // Act
        var result = Result<int>.Fail(HttpError.NotFound404);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithErrorCode("NotFound404");
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void ResultT_Fail_WithVeryLongStrings_ShouldHandleCorrectly()
    {
        // Arrange
        var longTitle = new string('A', 1000);
        var longDetail = new string('B', 2000);

        // Act
        var result = Result<string>.Fail(longTitle, longDetail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle(longTitle);
        result.ShouldHaveProblem().WithDetail(longDetail);
    }

    [Fact]
    public void ResultT_Fail_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        const string title = "Error with 'quotes', \"double quotes\", and \n newlines";
        const string detail = "Contains: <>&@#$%^*(){}[]|\\`;";

        // Act
        var result = Result<string>.Fail(title, detail);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle(title);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    [Fact]
    public void ResultT_Fail_WithNullStrings_ShouldHandleGracefully()
    {
        // Act
        var result = Result<int>.Fail(null!, null!);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
    }

    [Fact]
    public void ResultT_Fail_ChainedOperations_ShouldMaintainFailureState()
    {
        // Act
        var result1 = Result<int>.Fail("Error 1");
        var result2 = result1.IsFailed ? Result<string>.Fail(result1.Problem!) : Result<string>.Succeed("Success");
        var result3 = result2.IsFailed ? Result<bool>.Fail(result2.Problem!) : Result<bool>.Succeed(true);

        // Assert
        result1.IsFailed.Should().BeTrue();
        result2.IsFailed.Should().BeTrue();
        result3.IsFailed.Should().BeTrue();
        result3.Problem!.Title.Should().Be("Error 1");
    }

    [Fact]
    public void ResultT_Fail_WithComplexTypes_ShouldWorkCorrectly()
    {
        // Act
        var result1 = Result<User>.Fail("User creation failed");
        var result2 = Result<List<string>>.FailNotFound();
        var result3 = Result<Dictionary<string, int>>.FailValidation(("data", "Invalid format"));

        // Assert
        result1.IsFailed.Should().BeTrue();
        result2.IsFailed.Should().BeTrue();
        result3.IsFailed.Should().BeTrue();
        result1.Value.Should().BeNull();
        result2.Value.Should().BeNull();
        result3.Value.Should().BeNull();
    }

    #endregion

    #region Test Helpers

    private enum TestError
    {
        InvalidInput,
        ValidationFailed,
        SystemError,
        DatabaseError
    }

    private enum HttpError
    {
        BadRequest400 = 400,
        NotFound404 = 404,
        InternalError500 = 500
    }

    private class User
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    #endregion
}
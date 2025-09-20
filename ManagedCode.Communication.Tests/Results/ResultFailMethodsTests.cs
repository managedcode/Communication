using System;
using System.Net;
using Shouldly;
using ManagedCode.Communication.Constants;
using Xunit;

using ManagedCode.Communication.Tests.TestHelpers;
namespace ManagedCode.Communication.Tests.Results;

public class ResultFailMethodsTests
{
    #region Fail() Tests

    [Fact]
    public void Result_Fail_NoParameters_ShouldCreateFailedResult()
    {
        // Act
        var result = Result.Fail();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.HasProblem.ShouldBeTrue();
    }

    #endregion

    #region Fail(Problem) Tests

    [Fact]
    public void Result_Fail_WithProblem_ShouldCreateFailedResultWithProblem()
    {
        // Arrange
        var problem = Problem.Create("Error", "Error detail", 400);

        // Act
        var result = Result.Fail(problem);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.Problem.ShouldBe(problem);
        result.ShouldHaveProblem().WithTitle("Error");
        result.ShouldHaveProblem().WithDetail("Error detail");
        result.ShouldHaveProblem().WithStatusCode(400);
    }

    #endregion

    #region Fail(string) Tests

    [Fact]
    public void Result_Fail_WithTitle_ShouldCreateFailedResultWithInternalServerError()
    {
        // Arrange
        const string title = "Operation Failed";

        // Act
        var result = Result.Fail(title);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle(title);
        result.ShouldHaveProblem().WithDetail(title);
        result.ShouldHaveProblem().WithStatusCode(500);
    }

    #endregion

    #region Fail(string, string) Tests

    [Fact]
    public void Result_Fail_WithTitleAndDetail_ShouldCreateFailedResultWithDefaultStatus()
    {
        // Arrange
        const string title = "Validation Error";
        const string detail = "The input is invalid";

        // Act
        var result = Result.Fail(title, detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle(title);
        result.ShouldHaveProblem().WithDetail(detail);
        result.ShouldHaveProblem().WithStatusCode(500);
    }

    #endregion

    #region Fail(string, string, HttpStatusCode) Tests

    [Fact]
    public void Result_Fail_WithTitleDetailAndStatus_ShouldCreateFailedResultWithSpecifiedStatus()
    {
        // Arrange
        const string title = "Not Found";
        const string detail = "Resource does not exist";
        const HttpStatusCode status = HttpStatusCode.NotFound;

        // Act
        var result = Result.Fail(title, detail, status);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
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
    public void Result_Fail_WithVariousStatusCodes_ShouldSetCorrectStatusCode(HttpStatusCode statusCode, int expectedCode)
    {
        // Act
        var result = Result.Fail("Error", "Detail", statusCode);

        // Assert
        result.ShouldHaveProblem().WithStatusCode(expectedCode);
    }

    #endregion

    #region Fail(Exception) Tests

    [Fact]
    public void Result_Fail_WithException_ShouldCreateFailedResultWithInternalServerError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result.Fail(exception);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle("InvalidOperationException");
        result.ShouldHaveProblem().WithDetail("Test exception");
        result.ShouldHaveProblem().WithStatusCode(500);
        result.ShouldHaveProblem().WithErrorCode(exception.GetType().FullName ?? exception.GetType().Name);
    }

    [Fact]
    public void Result_Fail_WithInnerException_ShouldPreserveExceptionInfo()
    {
        // Arrange
        var innerException = new ArgumentNullException("param", "Parameter is null");
        var exception = new InvalidOperationException("Outer exception", innerException);

        // Act
        var result = Result.Fail(exception);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle("InvalidOperationException");
        result.ShouldHaveProblem().WithDetail("Outer exception");
    }

    #endregion

    #region Fail(Exception, HttpStatusCode) Tests

    [Fact]
    public void Result_Fail_WithExceptionAndStatus_ShouldCreateFailedResultWithSpecifiedStatus()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");
        const HttpStatusCode status = HttpStatusCode.Forbidden;

        // Act
        var result = Result.Fail(exception, status);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle("UnauthorizedAccessException");
        result.ShouldHaveProblem().WithDetail("Access denied");
        result.ShouldHaveProblem().WithStatusCode(403);
    }

    #endregion

    #region FailValidation Tests

    [Fact]
    public void Result_FailValidation_WithSingleError_ShouldCreateValidationFailedResult()
    {
        // Act
        var result = Result.FailValidation(("email", "Email is required"));

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(400);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.ValidationFailed);
        var errors = result.AssertValidationErrors();
        errors.ShouldContainKey("email");
        errors["email"].ShouldContain("Email is required");
    }

    [Fact]
    public void Result_FailValidation_WithMultipleErrors_ShouldCreateValidationFailedResultWithAllErrors()
    {
        // Act
        var result = Result.FailValidation(
            ("name", "Name is required"),
            ("email", "Invalid email format"),
            ("age", "Must be 18 or older")
        );

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(400);
        var errors = result.AssertValidationErrors();
        errors.ShouldNotBeNull();
        errors.ShouldHaveCount(3);
        errors["name"].ShouldContain("Name is required");
        errors["email"].ShouldContain("Invalid email format");
        errors["age"].ShouldContain("Must be 18 or older");
    }

    #endregion

    #region FailUnauthorized Tests

    [Fact]
    public void Result_FailUnauthorized_NoParameters_ShouldCreateUnauthorizedResult()
    {
        // Act
        var result = Result.FailUnauthorized();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(401);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Unauthorized);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.UnauthorizedAccess);
    }

    [Fact]
    public void Result_FailUnauthorized_WithDetail_ShouldCreateUnauthorizedResultWithCustomDetail()
    {
        // Arrange
        const string detail = "Invalid API key";

        // Act
        var result = Result.FailUnauthorized(detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(401);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Unauthorized);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    #endregion

    #region FailForbidden Tests

    [Fact]
    public void Result_FailForbidden_NoParameters_ShouldCreateForbiddenResult()
    {
        // Act
        var result = Result.FailForbidden();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(403);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Forbidden);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.ForbiddenAccess);
    }

    [Fact]
    public void Result_FailForbidden_WithDetail_ShouldCreateForbiddenResultWithCustomDetail()
    {
        // Arrange
        const string detail = "Insufficient permissions to perform this action";

        // Act
        var result = Result.FailForbidden(detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(403);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Forbidden);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    #endregion

    #region FailNotFound Tests

    [Fact]
    public void Result_FailNotFound_NoParameters_ShouldCreateNotFoundResult()
    {
        // Act
        var result = Result.FailNotFound();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.NotFound);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.ResourceNotFound);
    }

    [Fact]
    public void Result_FailNotFound_WithDetail_ShouldCreateNotFoundResultWithCustomDetail()
    {
        // Arrange
        const string detail = "User with ID 123 not found";

        // Act
        var result = Result.FailNotFound(detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.NotFound);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    #endregion

    #region Fail<TEnum> Tests

    [Fact]
    public void Result_Fail_WithEnum_ShouldCreateFailedResultWithErrorCode()
    {
        // Act
        var result = Result.Fail(TestError.InvalidInput);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithErrorCode("InvalidInput");
        result.ShouldHaveProblem().WithStatusCode(400); // Default for domain errors
    }

    [Fact]
    public void Result_Fail_WithEnumAndDetail_ShouldCreateFailedResultWithErrorCodeAndDetail()
    {
        // Arrange
        const string detail = "The input value is not valid";

        // Act
        var result = Result.Fail(TestError.ValidationFailed, detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithErrorCode("ValidationFailed");
        result.ShouldHaveProblem().WithDetail(detail);
        result.ShouldHaveProblem().WithStatusCode(400);
    }

    [Fact]
    public void Result_Fail_WithEnumAndStatus_ShouldCreateFailedResultWithErrorCodeAndStatus()
    {
        // Act
        var result = Result.Fail(TestError.SystemError, HttpStatusCode.InternalServerError);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithErrorCode("SystemError");
        result.ShouldHaveProblem().WithTitle("SystemError");
        result.ShouldHaveProblem().WithStatusCode(500);
    }

    [Fact]
    public void Result_Fail_WithEnumDetailAndStatus_ShouldCreateFailedResultWithAllSpecified()
    {
        // Arrange
        const string detail = "Database connection failed";
        const HttpStatusCode status = HttpStatusCode.ServiceUnavailable;

        // Act
        var result = Result.Fail(TestError.DatabaseError, detail, status);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithErrorCode("DatabaseError");
        result.ShouldHaveProblem().WithDetail(detail);
        result.ShouldHaveProblem().WithStatusCode(503);
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

    #endregion
}
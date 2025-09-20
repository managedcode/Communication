using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Shouldly;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Tests.TestHelpers;
using Xunit;

namespace ManagedCode.Communication.Tests.CollectionResults;

public class CollectionResultFailMethodsTests
{
    #region Fail() Tests

    [Fact]
    public void Fail_NoParameters_ShouldCreateFailedResult()
    {
        // Act
        var result = CollectionResult<string>.Fail();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Collection.ShouldBeEmpty();
        result.PageNumber.ShouldBe(0);
        result.PageSize.ShouldBe(0);
        result.TotalItems.ShouldBe(0);
        result.TotalPages.ShouldBe(0);
        result.HasProblem.ShouldBeTrue();
    }

    #endregion

    #region Fail(IEnumerable<T>) Tests

    [Fact]
    public void Fail_WithEnumerable_ShouldCreateFailedResultWithItems()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var result = CollectionResult<int>.Fail(items);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Collection.ShouldBeEquivalentTo(items);
        result.Collection.ShouldHaveCount(5);
        result.HasProblem.ShouldBeTrue();
    }

    [Fact]
    public void Fail_WithEmptyEnumerable_ShouldCreateFailedResultWithEmptyCollection()
    {
        // Arrange
        var items = Enumerable.Empty<string>();

        // Act
        var result = CollectionResult<string>.Fail(items);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Collection.ShouldBeEmpty();
        result.IsEmpty.ShouldBeTrue();
        result.HasItems.ShouldBeFalse();
    }

    #endregion

    #region Fail(T[]) Tests

    [Fact]
    public void Fail_WithArray_ShouldCreateFailedResultWithItems()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };

        // Act
        var result = CollectionResult<string>.Fail(items);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Collection.ShouldBeEquivalentTo(items);
        result.Collection.ShouldHaveCount(3);
        result.HasProblem.ShouldBeTrue();
    }

    [Fact]
    public void Fail_WithEmptyArray_ShouldCreateFailedResultWithEmptyCollection()
    {
        // Act
        var result = CollectionResult<User>.Fail(Array.Empty<User>());

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Collection.ShouldBeEmpty();
        result.IsEmpty.ShouldBeTrue();
    }

    #endregion

    #region Fail(Problem) Tests

    [Fact]
    public void Fail_WithProblem_ShouldCreateFailedResultWithProblem()
    {
        // Arrange
        var problem = Problem.Create("Test Error", "Test Detail", 400);

        // Act
        var result = CollectionResult<int>.Fail(problem);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Collection.ShouldBeEmpty();
        result.HasProblem.ShouldBeTrue();
        result.Problem.ShouldBe(problem);
        result.ShouldHaveProblem().WithTitle("Test Error");
        result.ShouldHaveProblem().WithDetail("Test Detail");
        result.ShouldHaveProblem().WithStatusCode(400);
    }

    #endregion

    #region Fail(string title) Tests

    [Fact]
    public void Fail_WithTitle_ShouldCreateFailedResultWithInternalServerError()
    {
        // Arrange
        const string title = "Operation Failed";

        // Act
        var result = CollectionResult<User>.Fail(title);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle(title);
        result.ShouldHaveProblem().WithDetail(title);
        result.ShouldHaveProblem().WithStatusCode(500);
        result.Collection.ShouldBeEmpty();
    }

    #endregion

    #region Fail(string title, string detail) Tests

    [Fact]
    public void Fail_WithTitleAndDetail_ShouldCreateFailedResultWithDefaultStatus()
    {
        // Arrange
        const string title = "Validation Error";
        const string detail = "The input is invalid";

        // Act
        var result = CollectionResult<int>.Fail(title, detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle(title);
        result.ShouldHaveProblem().WithDetail(detail);
        result.ShouldHaveProblem().WithStatusCode(500);
    }

    #endregion

    #region Fail(string title, string detail, HttpStatusCode status) Tests

    [Fact]
    public void Fail_WithTitleDetailAndStatus_ShouldCreateFailedResultWithSpecifiedStatus()
    {
        // Arrange
        const string title = "Not Found";
        const string detail = "Resources do not exist";
        const HttpStatusCode status = HttpStatusCode.NotFound;

        // Act
        var result = CollectionResult<string>.Fail(title, detail, status);

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
    [InlineData(HttpStatusCode.ServiceUnavailable, 503)]
    public void Fail_WithVariousStatusCodes_ShouldSetCorrectStatusCode(HttpStatusCode statusCode, int expectedCode)
    {
        // Act
        var result = CollectionResult<int>.Fail("Error", "Detail", statusCode);

        // Assert
        result.ShouldHaveProblem().WithStatusCode(expectedCode);
    }

    #endregion

    #region Fail(Exception) Tests

    [Fact]
    public void Fail_WithException_ShouldCreateFailedResultWithInternalServerError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = CollectionResult<string>.Fail(exception);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle("InvalidOperationException");
        result.ShouldHaveProblem().WithDetail("Test exception");
        result.ShouldHaveProblem().WithStatusCode(500);
        result.ShouldHaveProblem().WithErrorCode(exception.GetType().FullName ?? exception.GetType().Name);
        result.Collection.ShouldBeEmpty();
    }

    [Fact]
    public void Fail_WithInnerException_ShouldPreserveOuterExceptionInfo()
    {
        // Arrange
        var innerException = new ArgumentNullException("param", "Parameter is null");
        var exception = new InvalidOperationException("Outer exception", innerException);

        // Act
        var result = CollectionResult<int>.Fail(exception);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle("InvalidOperationException");
        result.ShouldHaveProblem().WithDetail("Outer exception");
    }

    #endregion

    #region Fail(Exception, HttpStatusCode) Tests

    [Fact]
    public void Fail_WithExceptionAndStatus_ShouldCreateFailedResultWithSpecifiedStatus()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");
        const HttpStatusCode status = HttpStatusCode.Forbidden;

        // Act
        var result = CollectionResult<User>.Fail(exception, status);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle("UnauthorizedAccessException");
        result.ShouldHaveProblem().WithDetail("Access denied");
        result.ShouldHaveProblem().WithStatusCode(403);
        result.ShouldHaveProblem().WithErrorCode(exception.GetType().FullName ?? exception.GetType().Name);
    }

    #endregion

    #region FailValidation Tests

    [Fact]
    public void FailValidation_WithSingleError_ShouldCreateValidationFailedResult()
    {
        // Act
        var result = CollectionResult<string>.FailValidation(("email", "Email is required"));

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(400);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.ValidationFailed);
        var errors = result.AssertValidationErrors();
        errors.ShouldContainKey("email");
        errors["email"].ShouldContain("Email is required");
        result.Collection.ShouldBeEmpty();
    }

    [Fact]
    public void FailValidation_WithMultipleErrors_ShouldCreateValidationFailedResultWithAllErrors()
    {
        // Act
        var result = CollectionResult<User>.FailValidation(
            ("name", "Name is required"),
            ("email", "Invalid email format"),
            ("age", "Must be 18 or older")
        );

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(400);
        var errors = result.AssertValidationErrors();
        errors.ShouldHaveCount(3);
        errors["name"].ShouldContain("Name is required");
        errors["email"].ShouldContain("Invalid email format");
        errors["age"].ShouldContain("Must be 18 or older");
    }

    [Fact]
    public void FailValidation_WithDuplicateFields_ShouldCombineErrors()
    {
        // Act
        var result = CollectionResult<int>.FailValidation(
            ("password", "Too short"),
            ("password", "Must contain numbers"),
            ("password", "Must contain special characters")
        );

        // Assert
        result.IsFailed.ShouldBeTrue();
        var errors = result.AssertValidationErrors();
        errors["password"].ShouldHaveCount(3);
        errors["password"].ShouldContain("Too short");
        errors["password"].ShouldContain("Must contain numbers");
        errors["password"].ShouldContain("Must contain special characters");
    }

    #endregion

    #region FailUnauthorized Tests

    [Fact]
    public void FailUnauthorized_NoParameters_ShouldCreateUnauthorizedResult()
    {
        // Act
        var result = CollectionResult<string>.FailUnauthorized();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(401);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Unauthorized);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.UnauthorizedAccess);
        result.Collection.ShouldBeEmpty();
    }

    [Fact]
    public void FailUnauthorized_WithDetail_ShouldCreateUnauthorizedResultWithCustomDetail()
    {
        // Arrange
        const string detail = "Invalid API key";

        // Act
        var result = CollectionResult<int>.FailUnauthorized(detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(401);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Unauthorized);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    #endregion

    #region FailForbidden Tests

    [Fact]
    public void FailForbidden_NoParameters_ShouldCreateForbiddenResult()
    {
        // Act
        var result = CollectionResult<User>.FailForbidden();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(403);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Forbidden);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.ForbiddenAccess);
        result.Collection.ShouldBeEmpty();
    }

    [Fact]
    public void FailForbidden_WithDetail_ShouldCreateForbiddenResultWithCustomDetail()
    {
        // Arrange
        const string detail = "Insufficient permissions to perform this action";

        // Act
        var result = CollectionResult<string>.FailForbidden(detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(403);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.Forbidden);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    #endregion

    #region FailNotFound Tests

    [Fact]
    public void FailNotFound_NoParameters_ShouldCreateNotFoundResult()
    {
        // Act
        var result = CollectionResult<User>.FailNotFound();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.NotFound);
        result.ShouldHaveProblem().WithDetail(ProblemConstants.Messages.ResourceNotFound);
        result.Collection.ShouldBeEmpty();
    }

    [Fact]
    public void FailNotFound_WithDetail_ShouldCreateNotFoundResultWithCustomDetail()
    {
        // Arrange
        const string detail = "Users not found in the specified group";

        // Act
        var result = CollectionResult<User>.FailNotFound(detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithTitle(ProblemConstants.Titles.NotFound);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    #endregion

    #region Fail<TEnum> Tests

    [Fact]
    public void Fail_WithEnum_ShouldCreateFailedResultWithErrorCode()
    {
        // Act
        var result = CollectionResult<string>.Fail(TestError.InvalidInput);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasProblem.ShouldBeTrue();
        result.ShouldHaveProblem().WithErrorCode("InvalidInput");
        result.ShouldHaveProblem().WithStatusCode(400); // Default for domain errors
        result.Collection.ShouldBeEmpty();
    }

    [Fact]
    public void Fail_WithEnumAndDetail_ShouldCreateFailedResultWithErrorCodeAndDetail()
    {
        // Arrange
        const string detail = "The input values are not valid";

        // Act
        var result = CollectionResult<int>.Fail(TestError.ValidationFailed, detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithErrorCode("ValidationFailed");
        result.ShouldHaveProblem().WithDetail(detail);
        result.ShouldHaveProblem().WithStatusCode(400);
    }

    [Fact]
    public void Fail_WithEnumAndStatus_ShouldCreateFailedResultWithErrorCodeAndStatus()
    {
        // Act
        var result = CollectionResult<User>.Fail(TestError.SystemError, HttpStatusCode.InternalServerError);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithErrorCode("SystemError");
        result.ShouldHaveProblem().WithTitle("SystemError");
        result.ShouldHaveProblem().WithStatusCode(500);
    }

    [Fact]
    public void Fail_WithEnumDetailAndStatus_ShouldCreateFailedResultWithAllSpecified()
    {
        // Arrange
        const string detail = "Database connection failed";
        const HttpStatusCode status = HttpStatusCode.ServiceUnavailable;

        // Act
        var result = CollectionResult<string>.Fail(TestError.DatabaseError, detail, status);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithErrorCode("DatabaseError");
        result.ShouldHaveProblem().WithDetail(detail);
        result.ShouldHaveProblem().WithStatusCode(503);
    }

    [Fact]
    public void Fail_WithHttpStatusEnum_ShouldUseEnumValueAsStatusCode()
    {
        // Act
        var result = CollectionResult<int>.Fail(HttpError.NotFound404);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithErrorCode("NotFound404");
    }

    #endregion

    #region Complex Type Tests

    [Fact]
    public void Fail_WithComplexTypes_ShouldHandleCorrectly()
    {
        // Act
        var result1 = CollectionResult<Dictionary<string, int>>.Fail("Dictionary operation failed");
        var result2 = CollectionResult<List<User>>.FailValidation(("users", "Invalid user list"));
        var result3 = CollectionResult<Tuple<int, string, bool>>.FailNotFound();
        var result4 = CollectionResult<User>.Fail(TestError.SystemError);

        // Assert
        result1.IsFailed.ShouldBeTrue();
        result1.Collection.ShouldBeEmpty();

        result2.IsFailed.ShouldBeTrue();
        result2.Collection.ShouldBeEmpty();

        result3.IsFailed.ShouldBeTrue();
        result3.Collection.ShouldBeEmpty();

        result4.IsFailed.ShouldBeTrue();
        result4.Collection.ShouldBeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Fail_WithVeryLongStrings_ShouldHandleCorrectly()
    {
        // Arrange
        var longTitle = new string('A', 1000);
        var longDetail = new string('B', 2000);

        // Act
        var result = CollectionResult<string>.Fail(longTitle, longDetail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle(longTitle);
        result.ShouldHaveProblem().WithDetail(longDetail);
    }

    [Fact]
    public void Fail_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        const string title = "Error with 'quotes', \"double quotes\", and \n newlines";
        const string detail = "Contains: <>&@#$%^*(){}[]|\\`;";

        // Act
        var result = CollectionResult<string>.Fail(title, detail);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.ShouldHaveProblem().WithTitle(title);
        result.ShouldHaveProblem().WithDetail(detail);
    }

    [Fact]
    public void Fail_ChainedOperations_ShouldMaintainFailureState()
    {
        // Act
        var problem = Problem.Create("Initial Error", "Initial Detail", 500);
        var result1 = CollectionResult<int>.Fail(problem);
        
        var result2 = result1.IsFailed 
            ? CollectionResult<string>.Fail(result1.Problem!) 
            : CollectionResult<string>.Succeed(new[] { "Success" });
            
        var result3 = result2.IsFailed 
            ? CollectionResult<bool>.Fail(result2.Problem!) 
            : CollectionResult<bool>.Succeed(new[] { true });

        // Assert
        result1.IsFailed.ShouldBeTrue();
        result2.IsFailed.ShouldBeTrue();
        result3.IsFailed.ShouldBeTrue();
        result3.Problem!.Title.ShouldBe("Initial Error");
        result3.Problem.Detail.ShouldBe("Initial Detail");
    }

    [Fact]
    public void Fail_WithLargeCollection_ShouldPreserveAllItems()
    {
        // Arrange
        var items = Enumerable.Range(1, 10000).ToArray();

        // Act
        var result = CollectionResult<int>.Fail(items);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Collection.ShouldHaveCount(10000);
        result.Collection.First().ShouldBe(1);
        result.Collection.Last().ShouldBe(10000);
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
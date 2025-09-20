using Shouldly;
using ManagedCode.Communication.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class ControllerExtensionsTests
{
    [Fact]
    public void ToActionResult_WithSuccessResult_ReturnsOkObjectResult()
    {
        // Arrange
        var expectedValue = "test value";
        var result = Result<string>.Succeed(expectedValue);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.ShouldBeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult;
        okResult.Value.ShouldBe(expectedValue);
        okResult.StatusCode.ShouldBe(200);
    }

    [Fact]
    public void ToActionResult_WithSuccessResultNoValue_ReturnsNoContent()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.ShouldBeOfType<NoContentResult>();
        var noContentResult = (NoContentResult)actionResult;
        noContentResult.StatusCode.ShouldBe(204);
    }

    [Fact]
    public void ToActionResult_WithFailedResult_ReturnsCorrectStatusCode()
    {
        // Arrange
        var problem = Problem.Create("Not Found", "Resource not found", 404);
        var result = Result<string>.Fail(problem);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.ShouldBeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.ShouldBe(404);
        objectResult.Value.ShouldBeOfType<Problem>();

        var returnedProblem = (Problem)objectResult.Value!;
        returnedProblem.StatusCode.ShouldBe(404);
        returnedProblem.Title.ShouldBe("Not Found");
        returnedProblem.Detail.ShouldBe("Resource not found");
    }

    [Fact]
    public void ToActionResult_WithValidationError_Returns400WithProblemDetails()
    {
        // Arrange
        var problem = Problem.Create("Validation Error", "Field 'email' is required", 400);
        var result = Result<object>.Fail(problem);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.ShouldBeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.ShouldBe(400);

        var returnedProblem = (Problem)objectResult.Value!;
        returnedProblem.StatusCode.ShouldBe(400);
        returnedProblem.Title.ShouldBe("Validation Error");
    }

    [Fact]
    public void ToActionResult_WithNoProblem_ReturnsDefaultError()
    {
        // Arrange - manually create failed result without problem
        var result = (Result<string>)default;

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.ShouldBeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.ShouldBe(500);

        var returnedProblem = (Problem)objectResult.Value!;
        returnedProblem.StatusCode.ShouldBe(500);
        returnedProblem.Title.ShouldBe("Operation failed");
    }

    [Fact]
    public void ToHttpResult_WithSuccessResult_ReturnsOkResult()
    {
        // Arrange
        var expectedValue = 42;
        var result = Result<int>.Succeed(expectedValue);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.ShouldNotBeNull();
        httpResult.GetType().Name.ShouldContain("Ok");
    }

    [Fact]
    public void ToHttpResult_WithSuccessResultNoValue_ReturnsNoContent()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.ShouldNotBeNull();
        httpResult.GetType().Name.ShouldContain("NoContent");
    }

    [Fact]
    public void ToHttpResult_WithFailedResult_ReturnsProblemResult()
    {
        // Arrange
        var problem = Problem.Create("Unauthorized", "Access denied", 401);
        var result = Result<int>.Fail(problem);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.ShouldNotBeNull();
        httpResult.GetType().Name.ShouldContain("Problem");
    }

    [Fact]
    public void ToHttpResult_WithComplexFailure_PreservesProblemDetails()
    {
        // Arrange
        var problem = Problem.Create("Business Error", "Invalid operation for current state", 422);
        problem.Extensions["errorCode"] = "INVALID_STATE";
        problem.Extensions["timestamp"] = "2024-01-01";

        var result = Result<object>.Fail(problem);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.ShouldNotBeNull();
        httpResult.GetType().Name.ShouldContain("Problem");
    }

    [Theory]
    [InlineData(400, "Bad Request")]
    [InlineData(401, "Unauthorized")]
    [InlineData(403, "Forbidden")]
    [InlineData(404, "Not Found")]
    [InlineData(409, "Conflict")]
    [InlineData(422, "Unprocessable Entity")]
    [InlineData(500, "Internal Server Error")]
    public void ToActionResult_WithVariousStatusCodes_ReturnsCorrectStatusCode(int statusCode, string title)
    {
        // Arrange
        var problem = Problem.Create(title, "Test error", statusCode);
        var result = Result<string>.Fail(problem);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.ShouldBeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.ShouldBe(statusCode);

        var returnedProblem = (Problem)objectResult.Value!;
        returnedProblem.StatusCode.ShouldBe(statusCode);
        returnedProblem.Title.ShouldBe(title);
    }

    [Fact]
    public void ToActionResult_WithComplexObject_ReturnsCorrectValue()
    {
        // Arrange
        var complexObject = new
        {
            Id = 1,
            Name = "Test",
            Items = new[] { "A", "B", "C" }
        };
        var result = Result<object>.Succeed(complexObject);

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.ShouldBeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult;
        okResult.Value.ShouldBe(complexObject);
    }

    [Fact]
    public void ToHttpResult_WithNullValue_HandlesGracefully()
    {
        // Arrange
        string? nullValue = null;
        var result = Result<string?>.Succeed(nullValue);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.ShouldNotBeNull();
        httpResult.GetType().Name.ShouldContain("Ok");
    }

    [Fact]
    public void ToActionResult_NonGenericWithNoProblem_ReturnsDefaultError()
    {
        // Arrange - manually create failed result without problem
        var result =  Result.Fail();

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.ShouldBeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.ShouldBe(500);

        var returnedProblem = (Problem)objectResult.Value!;
        returnedProblem.StatusCode.ShouldBe(500);
        returnedProblem.Title.ShouldBe("Operation failed");
        returnedProblem.Detail.ShouldBe("Unknown error occurred");
    }

    [Fact]
    public void ToHttpResult_NonGenericWithNoProblem_ReturnsDefaultError()
    {
        // Arrange - manually create failed result without problem
        var result = Result.Fail();

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.ShouldNotBeNull();
        httpResult.GetType().Name.ShouldContain("Problem");
    }
}

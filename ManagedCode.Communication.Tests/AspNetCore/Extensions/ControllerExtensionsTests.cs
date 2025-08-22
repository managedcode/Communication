using FluentAssertions;
using ManagedCode.Communication.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

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
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult;
        okResult.Value.Should().Be(expectedValue);
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public void ToActionResult_WithSuccessResultNoValue_ReturnsNoContent()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.Should().BeOfType<NoContentResult>();
        var noContentResult = (NoContentResult)actionResult;
        noContentResult.StatusCode.Should().Be(204);
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
        actionResult.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.Should().Be(404);
        objectResult.Value.Should().BeOfType<Problem>();
        
        var returnedProblem = (Problem)objectResult.Value!;
        returnedProblem.StatusCode.Should().Be(404);
        returnedProblem.Title.Should().Be("Not Found");
        returnedProblem.Detail.Should().Be("Resource not found");
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
        actionResult.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.Should().Be(400);
        
        var returnedProblem = (Problem)objectResult.Value!;
        returnedProblem.StatusCode.Should().Be(400);
        returnedProblem.Title.Should().Be("Validation Error");
    }

    [Fact]
    public void ToActionResult_WithNoProblem_ReturnsDefaultError()
    {
        // Arrange - manually create failed result without problem
        var result = (Result<string>)default;

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.Should().Be(500);
        
        var returnedProblem = (Problem)objectResult.Value!;
        returnedProblem.StatusCode.Should().Be(500);
        returnedProblem.Title.Should().Be("Operation failed");
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
        httpResult.Should().NotBeNull();
        httpResult.GetType().Name.Should().Contain("Ok");
    }

    [Fact]
    public void ToHttpResult_WithSuccessResultNoValue_ReturnsNoContent()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.GetType().Name.Should().Contain("NoContent");
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
        httpResult.Should().NotBeNull();
        httpResult.GetType().Name.Should().Contain("Problem");
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
        httpResult.Should().NotBeNull();
        httpResult.GetType().Name.Should().Contain("Problem");
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
        actionResult.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.Should().Be(statusCode);
        
        var returnedProblem = (Problem)objectResult.Value!;
        returnedProblem.StatusCode.Should().Be(statusCode);
        returnedProblem.Title.Should().Be(title);
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
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult;
        okResult.Value.Should().BeEquivalentTo(complexObject);
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
        httpResult.Should().NotBeNull();
        httpResult.GetType().Name.Should().Contain("Ok");
    }

    [Fact]
    public void ToActionResult_NonGenericWithNoProblem_ReturnsDefaultError()
    {
        // Arrange - manually create failed result without problem
        var result = new Result { IsSuccess = false, Problem = null };

        // Act
        var actionResult = result.ToActionResult();

        // Assert
        actionResult.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.Should().Be(500);
        
        var returnedProblem = (Problem)objectResult.Value!;
        returnedProblem.StatusCode.Should().Be(500);
        returnedProblem.Title.Should().Be("Operation failed");
        returnedProblem.Detail.Should().Be("Unknown error occurred");
    }

    [Fact]
    public void ToHttpResult_NonGenericWithNoProblem_ReturnsDefaultError()
    {
        // Arrange - manually create failed result without problem
        var result = new Result { IsSuccess = false, Problem = null };

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().NotBeNull();
        httpResult.GetType().Name.Should().Contain("Problem");
    }
}
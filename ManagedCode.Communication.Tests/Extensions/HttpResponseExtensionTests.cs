using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.Extensions;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

public class HttpResponseExtensionTests
{
    #region FromJsonToResult<T> Tests

    [Fact]
    public async Task FromJsonToResult_SuccessStatusCode_WithValidJson_ReturnsSuccessResult()
    {
        // Arrange
        var testData = new TestData { Id = 42, Name = "Test" };
        var resultData = Result<TestData>.Succeed(testData);
        var json = JsonSerializer.Serialize(resultData);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Act
        var result = await response.FromJsonToResult<TestData>();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Id.ShouldBe(42);
        result.Value.Name.ShouldBe("Test");
    }

    [Fact]
    public async Task FromJsonToResult_SuccessStatusCode_WithFailedResultJson_ReturnsFailedResult()
    {
        // Arrange
        var problem = Problem.Create("Error", "Something went wrong", 400);
        var resultData = Result<TestData>.Fail(problem);
        var json = JsonSerializer.Serialize(resultData);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Act
        var result = await response.FromJsonToResult<TestData>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Title.ShouldBe("Error");
        result.Problem.Detail.ShouldBe("Something went wrong");
    }

    [Fact]
    public async Task FromJsonToResult_ErrorStatusCode_ReturnsFailedResult()
    {
        // Arrange
        var errorContent = "Internal Server Error";
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(errorContent, Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromJsonToResult<TestData>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Title.ShouldBe("Internal Server Error");
        result.Problem.Detail.ShouldBe("Internal Server Error");
        result.Problem.StatusCode.ShouldBe(500);
    }

    [Fact]
    public async Task FromJsonToResult_BadRequestStatusCode_ReturnsFailedResultWithCorrectStatus()
    {
        // Arrange
        var errorContent = "Bad Request - Invalid input";
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorContent, Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromJsonToResult<string>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.Title.ShouldBe("Bad Request - Invalid input");
        result.Problem.Detail.ShouldBe("Bad Request - Invalid input");
    }

    [Fact]
    public async Task FromJsonToResult_NotFoundStatusCode_ReturnsFailedResult()
    {
        // Arrange
        var errorContent = "Resource not found";
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(errorContent, Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromJsonToResult<TestData>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.StatusCode.ShouldBe(404);
        result.Problem.Title.ShouldBe("Resource not found");
        result.Problem.Detail.ShouldBe("Resource not found");
    }

    [Fact]
    public async Task FromJsonToResult_EmptyContent_WithErrorStatus_ReturnsFailedResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("", Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromJsonToResult<TestData>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.StatusCode.ShouldBe(401);
        result.Problem.Title.ShouldBeEmpty();
        result.Problem.Detail.ShouldBeEmpty();
    }

    #endregion

    #region FromRequestToResult Tests

    [Fact]
    public async Task FromRequestToResult_SuccessStatusCode_ReturnsSuccessResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Success", Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromRequestToResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Problem.ShouldBeNull();
    }

    [Fact]
    public async Task FromRequestToResult_CreatedStatusCode_ReturnsSuccessResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("Created", Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromRequestToResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Problem.ShouldBeNull();
    }

    [Fact]
    public async Task FromRequestToResult_NoContentStatusCode_ReturnsSuccessResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);

        // Act
        var result = await response.FromRequestToResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Problem.ShouldBeNull();
    }

    [Fact]
    public async Task FromRequestToResult_ErrorStatusCode_ReturnsFailedResult()
    {
        // Arrange
        var errorContent = "Internal Server Error";
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(errorContent, Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromRequestToResult();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Title.ShouldBe("Internal Server Error");
        result.Problem.Detail.ShouldBe("Internal Server Error");
        result.Problem.StatusCode.ShouldBe(500);
    }

    [Fact]
    public async Task FromRequestToResult_ForbiddenStatusCode_ReturnsFailedResult()
    {
        // Arrange
        var errorContent = "Access forbidden";
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(errorContent, Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromRequestToResult();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.StatusCode.ShouldBe(403);
        result.Problem.Title.ShouldBe("Access forbidden");
        result.Problem.Detail.ShouldBe("Access forbidden");
    }

    [Fact]
    public async Task FromRequestToResult_ConflictStatusCode_ReturnsFailedResult()
    {
        // Arrange
        var errorContent = "Resource conflict";
        var response = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(errorContent, Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromRequestToResult();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.StatusCode.ShouldBe(409);
        result.Problem.Title.ShouldBe("Resource conflict");
        result.Problem.Detail.ShouldBe("Resource conflict");
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task FromJsonToResult_NullContent_WithErrorStatus_ReturnsFailedResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        // Content is null by default

        // Act
        var result = await response.FromJsonToResult<TestData>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task FromRequestToResult_NullContent_WithErrorStatus_ReturnsFailedResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        // Content is null by default

        // Act
        var result = await response.FromRequestToResult();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.StatusCode.ShouldBe(400);
    }

    #endregion

    #region Different Success Status Codes Tests

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task FromRequestToResult_VariousSuccessCodes_ReturnsSuccessResult(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("Success content", Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromRequestToResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Problem.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task FromRequestToResult_VariousErrorCodes_ReturnsFailedResult(HttpStatusCode statusCode)
    {
        // Arrange
        var errorContent = $"Error: {statusCode}";
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(errorContent, Encoding.UTF8, "text/plain")
        };

        // Act
        var result = await response.FromRequestToResult();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.StatusCode.ShouldBe((int)statusCode);
        result.Problem.Title.ShouldBe(errorContent);
        result.Problem.Detail.ShouldBe(errorContent);
    }

    #endregion

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
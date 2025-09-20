using System.Text.Json;
using Shouldly;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Serialization;

public class ProblemJsonConverterTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public void Serialize_BasicProblem_ProducesCorrectJson()
    {
        // Arrange
        var problem = Problem.Create("Validation Error", "Field is required", 400, "https://example.com/validation");

        // Act
        var json = JsonSerializer.Serialize(problem, _jsonOptions);

        // Assert
        json.ShouldContain("\"type\": \"https://example.com/validation\"");
        json.ShouldContain("\"title\": \"Validation Error\"");
        json.ShouldContain("\"status\": 400");
        json.ShouldContain("\"detail\": \"Field is required\"");
    }

    [Fact]
    public void Deserialize_BasicProblem_RestoresCorrectObject()
    {
        // Arrange
        var json = """
        {
            "type": "https://example.com/test-error",
            "title": "Test Error",
            "status": 422,
            "detail": "Something went wrong",
            "instance": "/api/test"
        }
        """;

        // Act
        var problem = JsonSerializer.Deserialize<Problem>(json, _jsonOptions);

        // Assert
        problem.ShouldNotBeNull();
        problem!.Type.ShouldBe("https://example.com/test-error");
        problem.Title.ShouldBe("Test Error");
        problem.StatusCode.ShouldBe(422);
        problem.Detail.ShouldBe("Something went wrong");
        problem.Instance.ShouldBe("/api/test");
    }

    [Fact]
    public void SerializeDeserialize_WithExtensions_PreservesData()
    {
        // Arrange
        var originalProblem = Problem.Create("Business Error", "Invalid state transition", 409);
        originalProblem.Extensions["errorCode"] = "INVALID_TRANSITION";
        originalProblem.Extensions["timestamp"] = "2024-01-01T10:00:00Z";
        originalProblem.Extensions["userId"] = 12345;

        // Act
        var json = JsonSerializer.Serialize(originalProblem, _jsonOptions);
        var deserializedProblem = JsonSerializer.Deserialize<Problem>(json, _jsonOptions);

        // Assert
        deserializedProblem.ShouldNotBeNull();
        deserializedProblem!.Type.ShouldBe(originalProblem.Type);
        deserializedProblem.Title.ShouldBe(originalProblem.Title);
        deserializedProblem.StatusCode.ShouldBe(originalProblem.StatusCode);
        deserializedProblem.Detail.ShouldBe(originalProblem.Detail);
        
        deserializedProblem.Extensions.ShouldHaveCount(3);
        deserializedProblem.Extensions.ShouldContainKey("errorCode");
        deserializedProblem.Extensions.ShouldContainKey("timestamp");
        deserializedProblem.Extensions.ShouldContainKey("userId");
    }

    [Fact]
    public void Serialize_ProblemWithErrorCode_IncludesErrorCode()
    {
        // Arrange
        var problem = Problem.Create(TestErrorEnum.InvalidInput, "Invalid input provided");

        // Act
        var json = JsonSerializer.Serialize(problem, _jsonOptions);

        // Assert
        json.ShouldContain("\"errorCode\": \"InvalidInput\"");
        json.ShouldContain("\"status\": 400");
    }

    [Fact]
    public void Deserialize_ProblemWithErrorCode_RestoresErrorCode()
    {
        // Arrange
        var json = """
        {
            "type": "about:blank",
            "title": "Invalid Input",
            "status": 400,
            "detail": "The input is invalid",
            "errorCode": "InvalidInput"
        }
        """;

        // Act
        var problem = JsonSerializer.Deserialize<Problem>(json, _jsonOptions);

        // Assert
        problem.ShouldNotBeNull();
        problem!.ErrorCode.ShouldBe("InvalidInput");
        problem.StatusCode.ShouldBe(400);
        problem.Title.ShouldBe("Invalid Input");
    }

    [Fact]
    public void Serialize_ValidationProblem_IncludesValidationErrors()
    {
        // Arrange
        var problem = Problem.Validation(
            ("email", "Email is required"),
            ("password", "Password must be at least 8 characters"),
            ("age", "Age must be positive")
        );

        // Act
        var json = JsonSerializer.Serialize(problem, _jsonOptions);

        // Assert
        json.ShouldContain("\"errors\":");
        json.ShouldContain("email");
        json.ShouldContain("Email is required");
        json.ShouldContain("password");
        json.ShouldContain("Password must be at least 8 characters");
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesBasicProperties()
    {
        // Arrange
        var originalProblem = new Problem
        {
            Type = "https://example.com/specific-error",
            Title = "Specific Error",
            StatusCode = 418,
            Detail = "I'm a teapot",
            Instance = "/api/coffee"
        };
        originalProblem.Extensions["custom"] = "value";

        // Act
        var json = JsonSerializer.Serialize(originalProblem, _jsonOptions);
        var roundTripProblem = JsonSerializer.Deserialize<Problem>(json, _jsonOptions);

        // Assert
        roundTripProblem.ShouldNotBeNull();
        roundTripProblem!.Type.ShouldBe(originalProblem.Type);
        roundTripProblem.Title.ShouldBe(originalProblem.Title);
        roundTripProblem.StatusCode.ShouldBe(originalProblem.StatusCode);
        roundTripProblem.Detail.ShouldBe(originalProblem.Detail);
        roundTripProblem.Instance.ShouldBe(originalProblem.Instance);
        roundTripProblem.Extensions.ShouldContainKey("custom");
    }

    [Theory]
    [InlineData(null, "about:blank")]
    [InlineData("", "")]
    [InlineData("custom-type", "custom-type")]
    public void Serialize_DifferentTypeValues_HandlesCorrectly(string? inputType, string expectedType)
    {
        // Arrange
        var problem = new Problem
        {
            Type = inputType!,
            Title = "Test",
            StatusCode = 400
        };

        // Act
        var json = JsonSerializer.Serialize(problem, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<Problem>(json, _jsonOptions);

        // Assert
        deserialized!.Type.ShouldBe(expectedType);
    }

    public enum TestErrorEnum
    {
        InvalidInput = 400,
        NotFound = 404,
        ServerError = 500
    }
}
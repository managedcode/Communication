using System.Text.Json;
using FluentAssertions;
using Xunit;

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
        json.Should().Contain("\"type\": \"https://example.com/validation\"");
        json.Should().Contain("\"title\": \"Validation Error\"");
        json.Should().Contain("\"status\": 400");
        json.Should().Contain("\"detail\": \"Field is required\"");
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
        problem.Should().NotBeNull();
        problem!.Type.Should().Be("https://example.com/test-error");
        problem.Title.Should().Be("Test Error");
        problem.StatusCode.Should().Be(422);
        problem.Detail.Should().Be("Something went wrong");
        problem.Instance.Should().Be("/api/test");
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
        deserializedProblem.Should().NotBeNull();
        deserializedProblem!.Type.Should().Be(originalProblem.Type);
        deserializedProblem.Title.Should().Be(originalProblem.Title);
        deserializedProblem.StatusCode.Should().Be(originalProblem.StatusCode);
        deserializedProblem.Detail.Should().Be(originalProblem.Detail);
        
        deserializedProblem.Extensions.Should().HaveCount(3);
        deserializedProblem.Extensions.Should().ContainKey("errorCode");
        deserializedProblem.Extensions.Should().ContainKey("timestamp");
        deserializedProblem.Extensions.Should().ContainKey("userId");
    }

    [Fact]
    public void Serialize_ProblemWithErrorCode_IncludesErrorCode()
    {
        // Arrange
        var problem = Problem.Create(TestErrorEnum.InvalidInput, "Invalid input provided");

        // Act
        var json = JsonSerializer.Serialize(problem, _jsonOptions);

        // Assert
        json.Should().Contain("\"errorCode\": \"InvalidInput\"");
        json.Should().Contain("\"status\": 400");
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
        problem.Should().NotBeNull();
        problem!.ErrorCode.Should().Be("InvalidInput");
        problem.StatusCode.Should().Be(400);
        problem.Title.Should().Be("Invalid Input");
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
        json.Should().Contain("\"errors\":");
        json.Should().Contain("email");
        json.Should().Contain("Email is required");
        json.Should().Contain("password");
        json.Should().Contain("Password must be at least 8 characters");
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
        roundTripProblem.Should().NotBeNull();
        roundTripProblem!.Type.Should().Be(originalProblem.Type);
        roundTripProblem.Title.Should().Be(originalProblem.Title);
        roundTripProblem.StatusCode.Should().Be(originalProblem.StatusCode);
        roundTripProblem.Detail.Should().Be(originalProblem.Detail);
        roundTripProblem.Instance.Should().Be(originalProblem.Instance);
        roundTripProblem.Extensions.Should().ContainKey("custom");
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
        deserialized!.Type.Should().Be(expectedType);
    }

    public enum TestErrorEnum
    {
        InvalidInput = 400,
        NotFound = 404,
        ServerError = 500
    }
}
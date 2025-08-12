using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Constants;
using Xunit;

namespace ManagedCode.Communication.Tests.Serialization;

public class SerializationTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #region Problem Serialization

    [Fact]
    public void Problem_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = Problem.Create("TestTitle", "TestDetail", 400, "https://test.com/error", "/api/test");
        original.Extensions["customKey"] = "customValue";
        original.Extensions["number"] = 42;

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Problem>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().Be(original.Type);
        deserialized.Title.Should().Be(original.Title);
        deserialized.StatusCode.Should().Be(original.StatusCode);
        deserialized.Detail.Should().Be(original.Detail);
        deserialized.Instance.Should().Be(original.Instance);
        deserialized.Extensions["customKey"]?.ToString().Should().Be("customValue");
        deserialized.Extensions["number"]?.ToString().Should().Be("42");
    }

    [Fact]
    public void Problem_WithValidationErrors_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = Problem.Validation(
            ("email", "Invalid format"),
            ("password", "Too short")
        );

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Problem>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().Be(ProblemConstants.Types.ValidationFailed);
        deserialized.Title.Should().Be(ProblemConstants.Titles.ValidationFailed);
        deserialized.StatusCode.Should().Be(400);

        var errors = deserialized.GetValidationErrors();
        errors.Should().NotBeNull();
        errors!["email"].Should().Contain("Invalid format");
        errors["password"].Should().Contain("Too short");
    }

    [Fact]
    public void Problem_FromException_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        exception.Data["key"] = "value";
        var original = Problem.Create(exception, 500);

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Problem>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Title.Should().Be("InvalidOperationException");
        deserialized.Detail.Should().Be("Test exception");
        deserialized.StatusCode.Should().Be(500);
        deserialized.ErrorCode.Should().Be(exception.GetType().FullName);
    }

    #endregion

    #region Result Serialization

    [Fact]
    public void Result_Success_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = Result.Succeed();

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Problem.Should().BeNull();
    }

    [Fact]
    public void Result_WithProblem_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = Result.Fail("Error Title", "Error Detail", HttpStatusCode.BadRequest);

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result>(json, _options);

        // Assert
        deserialized.IsFailed.Should().BeTrue();
        deserialized.Problem.Should().NotBeNull();
        deserialized.Problem!.Title.Should().Be("Error Title");
        deserialized.Problem.Detail.Should().Be("Error Detail");
        deserialized.Problem.StatusCode.Should().Be(400);
    }

    [Fact]
    public void ResultT_WithValue_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = Result<string>.Succeed("Test Value");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().Be("Test Value");
        deserialized.Problem.Should().BeNull();
    }

    [Fact]
    public void ResultT_WithComplexValue_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var complexValue = new TestModel
        {
            Id = 123,
            Name = "Test",
            Tags = new[] { "tag1", "tag2" }
        };
        var original = Result<TestModel>.Succeed(complexValue);

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result<TestModel>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().NotBeNull();
        deserialized.Value!.Id.Should().Be(123);
        deserialized.Value.Name.Should().Be("Test");
        deserialized.Value.Tags.Should().BeEquivalentTo(new[] { "tag1", "tag2" });
    }

    [Fact]
    public void ResultT_Failed_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = Result<int>.FailNotFound("Resource not found");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result<int>>(json, _options);

        // Assert
        deserialized.IsFailed.Should().BeTrue();
        deserialized.Value.Should().Be(default(int));
        deserialized.Problem.Should().NotBeNull();
        deserialized.Problem!.Title.Should().Be(ProblemConstants.Titles.NotFound);
        deserialized.Problem!.Detail.Should().Be("Resource not found");
        deserialized.Problem!.StatusCode.Should().Be(404);
    }

    #endregion

    #region CollectionResult Serialization

    [Fact]
    public void CollectionResult_WithItems_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var items = new[] { "item1", "item2", "item3" };
        var original = CollectionResult<string>.Succeed(items, 1, 10, 25);

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<CollectionResult<string>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Collection.Should().BeEquivalentTo(items);
        deserialized.PageNumber.Should().Be(1);
        deserialized.PageSize.Should().Be(10);
        deserialized.TotalItems.Should().Be(25);
        deserialized.TotalPages.Should().Be(3);
    }

    [Fact]
    public void CollectionResult_Empty_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = CollectionResult<int>.Empty();

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<CollectionResult<int>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Collection.Should().BeEmpty();
        deserialized.PageNumber.Should().Be(0);
        deserialized.PageSize.Should().Be(0);
        deserialized.TotalItems.Should().Be(0);
    }

    [Fact]
    public void CollectionResult_Failed_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = CollectionResult<TestModel>.FailUnauthorized("Access denied");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<CollectionResult<TestModel>>(json, _options);

        // Assert
        deserialized.IsFailed.Should().BeTrue();
        deserialized.Collection.Should().BeEmpty();
        deserialized.Problem.Should().NotBeNull();
        deserialized.Problem!.Title.Should().Be(ProblemConstants.Titles.Unauthorized);
        deserialized.Problem!.Detail.Should().Be("Access denied");
        deserialized.Problem!.StatusCode.Should().Be(401);
    }

    #endregion

    #region Command Serialization

    [Fact]
    public void Command_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = Command.Create("TestCommand");
        original.CorrelationId = "corr-123";
        original.UserId = "user-456";
        original.TraceId = "trace-789";
        original.Metadata = new CommandMetadata
        {
            RetryCount = 3,
            Priority = CommandPriority.High,
            Tags = new Dictionary<string, string> { ["env"] = "test" }
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Command>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.CommandId.Should().Be(original.CommandId);
        deserialized.CommandType.Should().Be("TestCommand");
        deserialized.CorrelationId.Should().Be("corr-123");
        deserialized.UserId.Should().Be("user-456");
        deserialized.TraceId.Should().Be("trace-789");
        deserialized.Metadata.Should().NotBeNull();
        deserialized.Metadata!.RetryCount.Should().Be(3);
        deserialized.Metadata!.Priority.Should().Be(CommandPriority.High);
        deserialized.Metadata!.Tags["env"].Should().Be("test");
    }

    [Fact]
    public void CommandT_WithValue_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var commandId = Guid.NewGuid();
        var value = new TestModel
        {
            Id = 999,
            Name = "Command Test",
            Tags = new[] { "cmd", "test" }
        };
        var original = Command<TestModel>.Create(commandId, value);
        original.SessionId = "session-123";
        original.SpanId = "span-456";

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Command<TestModel>>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.CommandId.Should().Be(commandId);
        deserialized.CommandType.Should().Be("TestModel");
        deserialized.Value.Should().NotBeNull();
        deserialized.Value!.Id.Should().Be(999);
        deserialized.Value!.Name.Should().Be("Command Test");
        deserialized.Value!.Tags.Should().BeEquivalentTo(new[] { "cmd", "test" });
        deserialized.SessionId.Should().Be("session-123");
        deserialized.SpanId.Should().Be("span-456");
    }

    [Fact]
    public void CommandT_WithCustomType_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var commandId = Guid.NewGuid();
        var original = Command<string>.Create(commandId, "CustomType", "Test Value");
        original.CausationId = "cause-123";

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Command<string>>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.CommandId.Should().Be(commandId);
        deserialized.CommandType.Should().Be("CustomType");
        deserialized.Value.Should().Be("Test Value");
        deserialized.CausationId.Should().Be("cause-123");
    }

    #endregion

    #region CommandMetadata Serialization

    [Fact]
    public void CommandMetadata_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = new CommandMetadata
        {
            RetryCount = 5,
            MaxRetries = 10,
            Priority = CommandPriority.Low,
            TimeoutSeconds = 30,
            ExecutionTime = TimeSpan.FromMilliseconds(1500),
            Tags = new Dictionary<string, string>
            {
                ["environment"] = "production",
                ["version"] = "1.0.0"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<CommandMetadata>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.RetryCount.Should().Be(5);
        deserialized.MaxRetries.Should().Be(10);
        deserialized.Priority.Should().Be(CommandPriority.Low);
        deserialized.TimeoutSeconds.Should().Be(30);
        deserialized.ExecutionTime.Should().Be(TimeSpan.FromMilliseconds(1500));
        deserialized.Tags.Should().NotBeNull();
        deserialized.Tags["environment"].Should().Be("production");
        deserialized.Tags["version"].Should().Be("1.0.0");
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void ComplexNestedStructure_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var innerResult = Result<TestModel>.Succeed(new TestModel
        {
            Id = 1,
            Name = "Inner",
            Tags = new[] { "nested" }
        });

        var command = Command<Result<TestModel>>.Create(innerResult);
        var outerResult = Result<Command<Result<TestModel>>>.Succeed(command);

        // Act
        var json = JsonSerializer.Serialize(outerResult, _options);
        var deserialized = JsonSerializer.Deserialize<Result<Command<Result<TestModel>>>>(json, _options);

        // Assert
        deserialized.IsSuccess.Should().BeTrue();
        deserialized.Value.Should().NotBeNull();
        deserialized.Value!.Value.Should().NotBeNull();
        deserialized.Value!.Value.IsSuccess.Should().BeTrue();
        deserialized.Value!.Value.Value.Should().NotBeNull();
        deserialized.Value!.Value.Value!.Id.Should().Be(1);
        deserialized.Value.Value.Value.Name.Should().Be("Inner");
    }

    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        // Arrange
        var problem = Problem.Create("Title", "Detail", 500, "https://test.com", "/instance");
        problem.Extensions["key1"] = "value1";
        problem.Extensions["nested"] = new Dictionary<string, object>
        {
            ["innerKey"] = "innerValue"
        };

        // Act - Multiple round trips
        var json1 = JsonSerializer.Serialize(problem, _options);
        var deserialized1 = JsonSerializer.Deserialize<Problem>(json1, _options);
        var json2 = JsonSerializer.Serialize(deserialized1, _options);
        var deserialized2 = JsonSerializer.Deserialize<Problem>(json2, _options);

        // Assert
        deserialized2.Should().NotBeNull();
        deserialized2!.Type.Should().Be(problem.Type);
        deserialized2!.Title.Should().Be(problem.Title);
        deserialized2!.StatusCode.Should().Be(problem.StatusCode);
        deserialized2!.Detail.Should().Be(problem.Detail);
        deserialized2!.Instance.Should().Be(problem.Instance);
        deserialized2!.Extensions["key1"]?.ToString().Should().Be("value1");
    }

    #endregion

    // Test model for complex serialization scenarios
    private class TestModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string[]? Tags { get; set; }
    }
}

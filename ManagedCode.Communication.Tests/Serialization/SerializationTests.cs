using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Shouldly;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Constants;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

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
        deserialized.ShouldNotBeNull();
        deserialized!.Type.ShouldBe(original.Type);
        deserialized.Title.ShouldBe(original.Title);
        deserialized.StatusCode.ShouldBe(original.StatusCode);
        deserialized.Detail.ShouldBe(original.Detail);
        deserialized.Instance.ShouldBe(original.Instance);
        deserialized.Extensions["customKey"]?.ToString().ShouldBe("customValue");
        deserialized.Extensions["number"]?.ToString().ShouldBe("42");
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
        deserialized.ShouldNotBeNull();
        deserialized!.Type.ShouldBe(ProblemConstants.Types.ValidationFailed);
        deserialized.Title.ShouldBe(ProblemConstants.Titles.ValidationFailed);
        deserialized.StatusCode.ShouldBe(400);

        var errors = deserialized.GetValidationErrors();
        errors.ShouldNotBeNull();
        errors!["email"].ShouldContain("Invalid format");
        errors["password"].ShouldContain("Too short");
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
        deserialized.ShouldNotBeNull();
        deserialized!.Title.ShouldBe("InvalidOperationException");
        deserialized.Detail.ShouldBe("Test exception");
        deserialized.StatusCode.ShouldBe(500);
        deserialized.ErrorCode.ShouldBe(exception.GetType().FullName);
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
        deserialized.IsSuccess.ShouldBeTrue();
        deserialized.Problem.ShouldBeNull();
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
        deserialized.IsFailed.ShouldBeTrue();
        deserialized.Problem.ShouldNotBeNull();
        deserialized.Problem!.Title.ShouldBe("Error Title");
        deserialized.Problem.Detail.ShouldBe("Error Detail");
        deserialized.Problem.StatusCode.ShouldBe(400);
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
        deserialized.IsSuccess.ShouldBeTrue();
        deserialized.Value.ShouldBe("Test Value");
        deserialized.Problem.ShouldBeNull();
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
        deserialized.IsSuccess.ShouldBeTrue();
        deserialized.Value.ShouldNotBeNull();
        deserialized.Value!.Id.ShouldBe(123);
        deserialized.Value.Name.ShouldBe("Test");
        deserialized.Value!.Tags!.ShouldBeEquivalentTo(new[] { "tag1", "tag2" });
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
        deserialized.IsFailed.ShouldBeTrue();
        deserialized.Value.ShouldBe(default(int));
        deserialized.Problem.ShouldNotBeNull();
        deserialized.Problem!.Title.ShouldBe(ProblemConstants.Titles.NotFound);
        deserialized.Problem!.Detail.ShouldBe("Resource not found");
        deserialized.Problem!.StatusCode.ShouldBe(404);
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
        deserialized.IsSuccess.ShouldBeTrue();
        deserialized.Collection.ShouldBeEquivalentTo(items);
        deserialized.PageNumber.ShouldBe(1);
        deserialized.PageSize.ShouldBe(10);
        deserialized.TotalItems.ShouldBe(25);
        deserialized.TotalPages.ShouldBe(3);
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
        deserialized.IsSuccess.ShouldBeTrue();
        deserialized.Collection.ShouldBeEmpty();
        deserialized.PageNumber.ShouldBe(0);
        deserialized.PageSize.ShouldBe(0);
        deserialized.TotalItems.ShouldBe(0);
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
        deserialized.IsFailed.ShouldBeTrue();
        deserialized.Collection.ShouldBeEmpty();
        deserialized.Problem.ShouldNotBeNull();
        deserialized.Problem!.Title.ShouldBe(ProblemConstants.Titles.Unauthorized);
        deserialized.Problem!.Detail.ShouldBe("Access denied");
        deserialized.Problem!.StatusCode.ShouldBe(401);
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
        deserialized.ShouldNotBeNull();
        deserialized!.CommandId.ShouldBe(original.CommandId);
        deserialized.CommandType.ShouldBe("TestCommand");
        deserialized.CorrelationId.ShouldBe("corr-123");
        deserialized.UserId.ShouldBe("user-456");
        deserialized.TraceId.ShouldBe("trace-789");
        deserialized.Metadata.ShouldNotBeNull();
        deserialized.Metadata!.RetryCount.ShouldBe(3);
        deserialized.Metadata!.Priority.ShouldBe(CommandPriority.High);
        deserialized.Metadata!.Tags["env"].ShouldBe("test");
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
        deserialized.ShouldNotBeNull();
        deserialized!.CommandId.ShouldBe(commandId);
        deserialized.CommandType.ShouldBe("TestModel");
        deserialized.Value.ShouldNotBeNull();
        deserialized.Value!.Id.ShouldBe(999);
        deserialized.Value!.Name.ShouldBe("Command Test");
        deserialized.Value!.Tags!.ShouldBeEquivalentTo(new[] { "cmd", "test" });
        deserialized.SessionId.ShouldBe("session-123");
        deserialized.SpanId.ShouldBe("span-456");
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
        deserialized.ShouldNotBeNull();
        deserialized!.CommandId.ShouldBe(commandId);
        deserialized.CommandType.ShouldBe("CustomType");
        deserialized.Value.ShouldBe("Test Value");
        deserialized.CausationId.ShouldBe("cause-123");
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
        deserialized.ShouldNotBeNull();
        deserialized!.RetryCount.ShouldBe(5);
        deserialized.MaxRetries.ShouldBe(10);
        deserialized.Priority.ShouldBe(CommandPriority.Low);
        deserialized.TimeoutSeconds.ShouldBe(30);
        deserialized.ExecutionTime.ShouldBe(TimeSpan.FromMilliseconds(1500));
        deserialized.Tags.ShouldNotBeNull();
        deserialized.Tags["environment"].ShouldBe("production");
        deserialized.Tags["version"].ShouldBe("1.0.0");
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
        deserialized.IsSuccess.ShouldBeTrue();
        deserialized.Value.ShouldNotBeNull();
        deserialized.Value!.Value.IsSuccess.ShouldBeTrue();
        deserialized.Value.Value.Value.ShouldNotBeNull();
        deserialized.Value.Value.Value!.Id.ShouldBe(1);
        deserialized.Value.Value.Value.Name.ShouldBe("Inner");
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
        deserialized2.ShouldNotBeNull();
        deserialized2!.Type.ShouldBe(problem.Type);
        deserialized2!.Title.ShouldBe(problem.Title);
        deserialized2!.StatusCode.ShouldBe(problem.StatusCode);
        deserialized2!.Detail.ShouldBe(problem.Detail);
        deserialized2!.Instance.ShouldBe(problem.Instance);
        deserialized2!.Extensions["key1"]?.ToString().ShouldBe("value1");
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Helpers;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using ManagedCode.Communication.Tests.Orleans.Models;
using Orleans;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Orleans.Serialization;

/// <summary>
/// Tests for Command serialization through Orleans grain calls
/// </summary>
public class CommandSerializationTests : IClassFixture<OrleansClusterFixture>
{
    private readonly IGrainFactory _grainFactory;

    public CommandSerializationTests(OrleansClusterFixture fixture)
    {
        _grainFactory = fixture.Cluster.GrainFactory;
    }

    [Fact]
    public async Task Command_WithAllFields_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var metadata = new CommandMetadata
        {
            InitiatedBy = "user123",
            Source = "web-app",
            Target = "payment-service",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            SessionId = "session-456",
            TraceId = "trace-789",
            SpanId = "span-012",
            Version = 1,
            Priority = CommandPriority.High,
            TimeToLiveSeconds = 300,
            Tags = new Dictionary<string, string>
            {
                ["environment"] = "production",
                ["region"] = "us-west"
            },
            Extensions = new Dictionary<string, object?>
            {
                ["customField"] = "customValue",
                ["retryCount"] = 3
            }
        };

        var command = Command.Create("ProcessPayment")
            .WithCorrelationId("correlation-123")
            .WithCausationId("causation-456")
            .WithMetadata(m =>
            {
                m.InitiatedBy = metadata.InitiatedBy;
                m.Source = metadata.Source;
                m.Target = metadata.Target;
                m.IpAddress = metadata.IpAddress;
                m.UserAgent = metadata.UserAgent;
                m.SessionId = metadata.SessionId;
                m.TraceId = metadata.TraceId;
                m.SpanId = metadata.SpanId;
                m.Version = metadata.Version;
                m.Priority = metadata.Priority;
                m.TimeToLiveSeconds = metadata.TimeToLiveSeconds;
                m.Tags = metadata.Tags;
                m.Extensions = metadata.Extensions;
            });

        // Act
        var result = await grain.EchoCommandAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.CommandId.ShouldBe(command.CommandId);
        result.CommandType.ShouldBe(command.CommandType);
        result.Timestamp.ShouldBeCloseTo(command.Timestamp, TimeSpan.FromSeconds(1));
        result.CorrelationId.ShouldBe(command.CorrelationId);
        result.CausationId.ShouldBe(command.CausationId);
        
        // Verify metadata
        result.Metadata.ShouldNotBeNull();
        result.Metadata!.InitiatedBy.ShouldBe(metadata.InitiatedBy);
        result.Metadata.Source.ShouldBe(metadata.Source);
        result.Metadata.Target.ShouldBe(metadata.Target);
        result.Metadata.IpAddress.ShouldBe(metadata.IpAddress);
        result.Metadata.UserAgent.ShouldBe(metadata.UserAgent);
        result.Metadata.SessionId.ShouldBe(metadata.SessionId);
        result.Metadata.TraceId.ShouldBe(metadata.TraceId);
        result.Metadata.SpanId.ShouldBe(metadata.SpanId);
        result.Metadata.Version.ShouldBe(metadata.Version);
        result.Metadata.Priority.ShouldBe(metadata.Priority);
        result.Metadata.TimeToLiveSeconds.ShouldBe(metadata.TimeToLiveSeconds);
        
        result.Metadata.Tags.ShouldNotBeNull();
        result.Metadata.Tags.ShouldHaveCount(2);
        result.Metadata.Tags!["environment"].ShouldBe("production");
        result.Metadata.Tags!["region"].ShouldBe("us-west");
        
        result.Metadata.Extensions.ShouldNotBeNull();
        result.Metadata.Extensions.ShouldHaveCount(2);
        result.Metadata.Extensions!["customField"].ShouldBe("customValue");
        result.Metadata.Extensions!["retryCount"].ShouldBe(3);
    }

    [Fact]
    public async Task Command_WithMinimalFields_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        var command = Command.Create("SimpleCommand");

        // Act
        var result = await grain.EchoCommandAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.CommandId.ShouldBe(command.CommandId);
        result.CommandType.ShouldBe("SimpleCommand");
        result.Timestamp.ShouldBeCloseTo(command.Timestamp, TimeSpan.FromSeconds(1));
        result.CorrelationId.ShouldBeNull();
        result.CausationId.ShouldBeNull();
        result.Metadata.ShouldBeNull();
    }

    [Fact]
    public async Task CommandT_WithComplexPayload_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var payload = new PaymentRequest
        {
            OrderId = "order-123",
            Amount = 99.99m,
            Currency = "USD",
            Items = new List<OrderItem>
            {
                new() { ProductId = "prod-1", Quantity = 2, Price = 25.50m },
                new() { ProductId = "prod-2", Quantity = 1, Price = 48.99m }
            },
            Metadata = new Dictionary<string, string>
            {
                ["customer"] = "cust-456",
                ["promotion"] = "SUMMER20"
            }
        };

        var command = Command<PaymentRequest>.From(GuidHelper.CreateVersion7(), payload);
        command.CommandType = "ProcessPayment";
        command.CorrelationId = "correlation-789";
        command.CausationId = "causation-012";
        command.Metadata = new CommandMetadata
        {
            InitiatedBy = "system",
            Priority = CommandPriority.Critical,
            Tags = new Dictionary<string, string> { ["urgent"] = "true" }
        };

        // Act
        var result = await grain.EchoCommandAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.CommandId.ShouldBe(command.CommandId);
        result.CommandType.ShouldBe("ProcessPayment");
        result.CorrelationId.ShouldBe("correlation-789");
        result.CausationId.ShouldBe("causation-012");
        
        result.Value.ShouldNotBeNull();
        result.Value!.OrderId.ShouldBe(payload.OrderId);
        result.Value.Amount.ShouldBe(payload.Amount);
        result.Value.Currency.ShouldBe(payload.Currency);
        
        result.Value!.Items.ShouldNotBeNull();
        result.Value.Items.ShouldHaveCount(2);
        result.Value.Items[0].ProductId.ShouldBe("prod-1");
        result.Value.Items[0].Quantity.ShouldBe(2);
        result.Value.Items[0].Price.ShouldBe(25.50m);
        
        result.Value.Metadata.ShouldNotBeNull();
        result.Value.Metadata["customer"].ShouldBe("cust-456");
        result.Value.Metadata["promotion"].ShouldBe("SUMMER20");
        
        result.Metadata!.InitiatedBy.ShouldBe("system");
        result.Metadata.Priority.ShouldBe(CommandPriority.Critical);
        result.Metadata.Tags!["urgent"].ShouldBe("true");
    }

    [Fact]
    public async Task PaginationCommand_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        var options = new PaginationOptions(defaultPageSize: 25, maxPageSize: 50, minPageSize: 10);

        var command = PaginationCommand.Create(GuidHelper.CreateVersion7(), skip: 25, take: 5, options);
        command.CorrelationId = "pagination-correlation";
        command.Metadata = new CommandMetadata
        {
            InitiatedBy = "tests",
            Priority = CommandPriority.Low,
            Tags = new Dictionary<string, string> { ["scope"] = "pagination" }
        };

        // Act
        var pagination = await grain.EchoPaginationCommandAsync(command);

        // Assert
        pagination.CommandId.ShouldBe(command.CommandId);
        pagination.CommandType.ShouldBe("Pagination");
        pagination.Timestamp.ShouldBeCloseTo(command.Timestamp, TimeSpan.FromSeconds(1));
        pagination.CorrelationId.ShouldBe("pagination-correlation");

        pagination.Skip.ShouldBe(25);
        pagination.Take.ShouldBe(10); // normalized to minimum page size 10
        pagination.PageSize.ShouldBe(10);
        pagination.PageNumber.ShouldBe(3);

        pagination.Value.ShouldNotBeNull();
        pagination.Value!.Skip.ShouldBe(25);
        pagination.Value.Take.ShouldBe(10);

        pagination.Metadata.ShouldNotBeNull();
        pagination.Metadata!.InitiatedBy.ShouldBe("tests");
        pagination.Metadata.Priority.ShouldBe(CommandPriority.Low);
        pagination.Metadata.Tags.ShouldNotBeNull();
        pagination.Metadata.Tags!["scope"].ShouldBe("pagination");
    }

    [Fact]
    public async Task CommandT_WithEnumType_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var command = Command<string>.From("test-data");
        command.CommandType = "CreateUser";
        command.Metadata = new CommandMetadata
        {
            InitiatedBy = "admin",
            TimeToLiveSeconds = 60
        };

        // Act
        var result = await grain.EchoCommandAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe("test-data");
        result.CommandType.ShouldBe("CreateUser");
        result.GetCommandTypeAsEnum<TestCommandType>().Value.ShouldBe(TestCommandType.CreateUser);
        result.Metadata!.InitiatedBy.ShouldBe("admin");
        result.Metadata.TimeToLiveSeconds.ShouldBe(60);
    }

    [Fact]
    public async Task CommandT_WithNullPayload_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        var command = Command.From<string?>(null);

        // Act
        var result = await grain.EchoCommandAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBeNull();
        result.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task CommandMetadata_WithAllFields_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var metadata = new CommandMetadata
        {
            InitiatedBy = "user@example.com",
            Source = "mobile-app",
            Target = "user-service",
            IpAddress = "10.0.0.1",
            UserAgent = "CustomApp/1.0",
            SessionId = Guid.NewGuid().ToString(),
            TraceId = Guid.NewGuid().ToString(),
            SpanId = Guid.NewGuid().ToString(),
            Version = 2,
            Priority = CommandPriority.Critical,
            TimeToLiveSeconds = 600,
            Tags = new Dictionary<string, string>
            {
                ["feature"] = "payments",
                ["client"] = "ios",
                ["version"] = "14.5"
            },
            Extensions = new Dictionary<string, object?>
            {
                ["rateLimitRemaining"] = 50,
                ["beta"] = true,
                ["metadata"] = new Dictionary<string, string>
                {
                    ["nested"] = "value"
                }
            }
        };

        // Act
        var result = await grain.EchoMetadataAsync(metadata);

        // Assert
        result.ShouldNotBeNull();
        result.InitiatedBy.ShouldBe(metadata.InitiatedBy);
        result.Source.ShouldBe(metadata.Source);
        result.Target.ShouldBe(metadata.Target);
        result.IpAddress.ShouldBe(metadata.IpAddress);
        result.UserAgent.ShouldBe(metadata.UserAgent);
        result.SessionId.ShouldBe(metadata.SessionId);
        result.TraceId.ShouldBe(metadata.TraceId);
        result.SpanId.ShouldBe(metadata.SpanId);
        result.Version.ShouldBe(metadata.Version);
        result.Priority.ShouldBe(metadata.Priority);
        result.TimeToLiveSeconds.ShouldBe(metadata.TimeToLiveSeconds);
        
        result.Tags.ShouldNotBeNull();
        result.Tags.ShouldHaveCount(3);
        result.Tags!["feature"].ShouldBe("payments");
        result.Tags!["client"].ShouldBe("ios");
        result.Tags!["version"].ShouldBe("14.5");
        
        result.Extensions.ShouldNotBeNull();
        result.Extensions.ShouldHaveCount(3);
        result.Extensions!["rateLimitRemaining"].ShouldBe(50);
        result.Extensions!["beta"].ShouldBe(true);
        result.Extensions!["metadata"].ShouldNotBeNull();
    }
}

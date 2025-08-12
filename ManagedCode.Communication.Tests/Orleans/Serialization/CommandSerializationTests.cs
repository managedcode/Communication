using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using ManagedCode.Communication.Tests.Orleans.Models;
using Orleans;
using Xunit;

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
        result.Should().NotBeNull();
        result.CommandId.Should().Be(command.CommandId);
        result.CommandType.Should().Be(command.CommandType);
        result.Timestamp.Should().BeCloseTo(command.Timestamp, TimeSpan.FromSeconds(1));
        result.CorrelationId.Should().Be(command.CorrelationId);
        result.CausationId.Should().Be(command.CausationId);
        
        // Verify metadata
        result.Metadata.Should().NotBeNull();
        result.Metadata!.InitiatedBy.Should().Be(metadata.InitiatedBy);
        result.Metadata.Source.Should().Be(metadata.Source);
        result.Metadata.Target.Should().Be(metadata.Target);
        result.Metadata.IpAddress.Should().Be(metadata.IpAddress);
        result.Metadata.UserAgent.Should().Be(metadata.UserAgent);
        result.Metadata.SessionId.Should().Be(metadata.SessionId);
        result.Metadata.TraceId.Should().Be(metadata.TraceId);
        result.Metadata.SpanId.Should().Be(metadata.SpanId);
        result.Metadata.Version.Should().Be(metadata.Version);
        result.Metadata.Priority.Should().Be(metadata.Priority);
        result.Metadata.TimeToLiveSeconds.Should().Be(metadata.TimeToLiveSeconds);
        
        result.Metadata.Tags.Should().NotBeNull();
        result.Metadata.Tags.Should().HaveCount(2);
        result.Metadata.Tags!["environment"].Should().Be("production");
        result.Metadata.Tags!["region"].Should().Be("us-west");
        
        result.Metadata.Extensions.Should().NotBeNull();
        result.Metadata.Extensions.Should().HaveCount(2);
        result.Metadata.Extensions!["customField"].Should().Be("customValue");
        result.Metadata.Extensions!["retryCount"].Should().Be(3);
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
        result.Should().NotBeNull();
        result.CommandId.Should().Be(command.CommandId);
        result.CommandType.Should().Be("SimpleCommand");
        result.Timestamp.Should().BeCloseTo(command.Timestamp, TimeSpan.FromSeconds(1));
        result.CorrelationId.Should().BeNull();
        result.CausationId.Should().BeNull();
        result.Metadata.Should().BeNull();
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

        var command = Command<PaymentRequest>.From(Guid.CreateVersion7(), payload);
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
        result.Should().NotBeNull();
        result.CommandId.Should().Be(command.CommandId);
        result.CommandType.Should().Be("ProcessPayment");
        result.CorrelationId.Should().Be("correlation-789");
        result.CausationId.Should().Be("causation-012");
        
        result.Value.Should().NotBeNull();
        result.Value!.OrderId.Should().Be(payload.OrderId);
        result.Value.Amount.Should().Be(payload.Amount);
        result.Value.Currency.Should().Be(payload.Currency);
        
        result.Value!.Items.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].ProductId.Should().Be("prod-1");
        result.Value.Items[0].Quantity.Should().Be(2);
        result.Value.Items[0].Price.Should().Be(25.50m);
        
        result.Value.Metadata.Should().NotBeNull();
        result.Value.Metadata["customer"].Should().Be("cust-456");
        result.Value.Metadata["promotion"].Should().Be("SUMMER20");
        
        result.Metadata!.InitiatedBy.Should().Be("system");
        result.Metadata.Priority.Should().Be(CommandPriority.Critical);
        result.Metadata.Tags!["urgent"].Should().Be("true");
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
        result.Should().NotBeNull();
        result.Value.Should().Be("test-data");
        result.CommandType.Should().Be("CreateUser");
        result.GetCommandTypeAsEnum<TestCommandType>().Value.Should().Be(TestCommandType.CreateUser);
        result.Metadata!.InitiatedBy.Should().Be("admin");
        result.Metadata.TimeToLiveSeconds.Should().Be(60);
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
        result.Should().NotBeNull();
        result.Value.Should().BeNull();
        result.IsEmpty.Should().BeTrue();
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
        result.Should().NotBeNull();
        result.InitiatedBy.Should().Be(metadata.InitiatedBy);
        result.Source.Should().Be(metadata.Source);
        result.Target.Should().Be(metadata.Target);
        result.IpAddress.Should().Be(metadata.IpAddress);
        result.UserAgent.Should().Be(metadata.UserAgent);
        result.SessionId.Should().Be(metadata.SessionId);
        result.TraceId.Should().Be(metadata.TraceId);
        result.SpanId.Should().Be(metadata.SpanId);
        result.Version.Should().Be(metadata.Version);
        result.Priority.Should().Be(metadata.Priority);
        result.TimeToLiveSeconds.Should().Be(metadata.TimeToLiveSeconds);
        
        result.Tags.Should().NotBeNull();
        result.Tags.Should().HaveCount(3);
        result.Tags!["feature"].Should().Be("payments");
        result.Tags!["client"].Should().Be("ios");
        result.Tags!["version"].Should().Be("14.5");
        
        result.Extensions.Should().NotBeNull();
        result.Extensions.Should().HaveCount(3);
        result.Extensions!["rateLimitRemaining"].Should().Be(50);
        result.Extensions!["beta"].Should().Be(true);
        result.Extensions!["metadata"].Should().NotBeNull();
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using ManagedCode.Communication.Tests.Orleans.Models;
using Orleans;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Orleans;

/// <summary>
/// Integration tests for all Communication types serialization through Orleans
/// </summary>
public class OrleansSerializationTests : IClassFixture<OrleansClusterFixture>
{
    private readonly IGrainFactory _grainFactory;

    public OrleansSerializationTests(OrleansClusterFixture fixture)
    {
        _grainFactory = fixture.Cluster.GrainFactory;
    }

    [Fact]
    public async Task CompleteWorkflow_AllTypes_ShouldSerializeCorrectly()
    {
        // This test simulates a complete workflow using all Communication types
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        // Step 1: Send a command
        var commandId = Guid.CreateVersion7();
        var paymentRequest = new PaymentRequest
        {
            OrderId = "order-999",
            Amount = 150.00m,
            Currency = "USD",
            Items = new List<OrderItem>
            {
                new() { ProductId = "prod-A", Quantity = 1, Price = 150.00m }
            }
        };
        
        var command = Command<PaymentRequest>.From(commandId, paymentRequest);
        command.CommandType = "ProcessPayment";
        command.CorrelationId = "workflow-123";
        command.Metadata = new CommandMetadata
        {
            InitiatedBy = "integration-test",
            Priority = CommandPriority.Normal
        };
        
        var echoedCommand = await grain.EchoCommandAsync(command);
        echoedCommand.ShouldNotBeNull();
        echoedCommand.CommandId.ShouldBe(commandId);
        echoedCommand.Value!.OrderId.ShouldBe("order-999");
        
        // Step 2: Return a successful result
        var paymentResponse = new PaymentResponse
        {
            TransactionId = "txn-999",
            Status = "completed",
            ProcessedAt = DateTime.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["gateway"] = "test-gateway",
                ["fee"] = 4.50m
            }
        };
        
        var successResult = Result<PaymentResponse>.Succeed(paymentResponse);
        var echoedResult = await grain.EchoResultAsync(successResult);
        echoedResult.IsSuccess.ShouldBeTrue();
        echoedResult.Value!.TransactionId.ShouldBe("txn-999");
        
        // Step 3: Handle a failure case
        var failureProblem = Problem.FromStatusCode(System.Net.HttpStatusCode.PaymentRequired, "Insufficient funds");
        failureProblem.Extensions["balance"] = 50.00m;
        failureProblem.Extensions["required"] = 150.00m;
        
        var failureResult = Result<PaymentResponse>.Fail(failureProblem);
        var echoedFailure = await grain.EchoResultAsync(failureResult);
        echoedFailure.IsSuccess.ShouldBeFalse();
        echoedFailure.Problem!.StatusCode.ShouldBe(402);
        echoedFailure.Problem!.Extensions["balance"].ShouldBe(50.00m);
        
        // Step 4: Return a collection of results
        var transactions = new[]
        {
            new PaymentResponse { TransactionId = "txn-1", Status = "completed" },
            new PaymentResponse { TransactionId = "txn-2", Status = "completed" },
            new PaymentResponse { TransactionId = "txn-3", Status = "pending" }
        };
        
        var collectionResult = CollectionResult<PaymentResponse>.Succeed(
            transactions,
            pageNumber: 1,
            pageSize: 10,
            totalItems: 3
        );
        
        var echoedCollection = await grain.EchoCollectionResultAsync(collectionResult);
        echoedCollection.Collection.ShouldHaveCount(3);
        echoedCollection.Collection[2].Status.ShouldBe("pending");
    }

    [Fact]
    public async Task ComplexNestedSerialization_ShouldPreserveAllData()
    {
        // This test ensures deeply nested and complex data structures are preserved
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        // Create a command with complex metadata
        var metadata = new CommandMetadata
        {
            Extensions = new Dictionary<string, object?>
            {
                ["user"] = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    Email = "test@example.com",
                    Attributes = new Dictionary<string, object>
                    {
                        ["permissions"] = new[] { "read", "write", "admin" },
                        ["settings"] = new Dictionary<string, object>
                        {
                            ["notifications"] = true,
                            ["theme"] = "dark"
                        }
                    }
                },
                ["audit"] = new Dictionary<string, object>
                {
                    ["ip"] = "192.168.1.1",
                    ["timestamp"] = DateTime.UtcNow,
                    ["actions"] = new List<Dictionary<string, object>>
                    {
                        new() { ["action"] = "login", ["time"] = DateTime.UtcNow.AddHours(-1).ToString("o") },
                        new() { ["action"] = "payment", ["time"] = DateTime.UtcNow.ToString("o") }
                    }
                }
            }
        };
        
        var echoedMetadata = await grain.EchoMetadataAsync(metadata);
        
        echoedMetadata.ShouldNotBeNull();
        echoedMetadata.Extensions.ShouldNotBeNull();
        
        var user = echoedMetadata.Extensions!["user"] as UserProfile;
        user.ShouldNotBeNull();
        user!.Email.ShouldBe("test@example.com");
        
        var permissions = user.Attributes["permissions"] as string[];
        permissions.ShouldNotBeNull();
        permissions!.ShouldContain("admin");
        
        var audit = echoedMetadata.Extensions["audit"] as Dictionary<string, object>;
        audit.ShouldNotBeNull();
        audit!["ip"].ShouldBe("192.168.1.1");
    }

    [Fact]
    public async Task ErrorHandling_ValidationErrors_ShouldSerializeCorrectly()
    {
        // Test that validation errors are properly serialized and deserialized
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var validationProblem = Problem.Validation(
            ("field1", "Error 1"),
            ("field2", "Error 2"),
            ("field3", "Error 3")
        );
        validationProblem.Extensions["requestId"] = Guid.NewGuid().ToString();
        
        var result = Result.Fail(validationProblem);
        var echoed = await grain.EchoResultAsync(result);

        echoed.Problem.ShouldNotBeNull();

        var errors = echoed.AssertValidationErrors();
        errors.ShouldHaveCount(3);
        errors["field1"].ShouldContain("Error 1");
        errors["field2"].ShouldContain("Error 2");
        errors["field3"].ShouldContain("Error 3");
        
        echoed.Problem!.Extensions["requestId"].ShouldNotBeNull();
    }
}

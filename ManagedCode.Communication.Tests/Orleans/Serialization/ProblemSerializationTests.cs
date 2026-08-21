using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using ManagedCode.Communication.Tests.TestHelpers;
using Orleans;
using Shouldly;

namespace ManagedCode.Communication.Tests.Orleans.Serialization;

/// <summary>
/// Tests for Problem serialization through Orleans grain calls
/// </summary>
[ClassDataSource<OrleansClusterFixture>(Shared = SharedType.PerClass)]
[NotInParallel(nameof(ProblemSerializationTests))]
public class ProblemSerializationTests
{
    private const string ValidationFailedDetail = "Validation failed";
    private const string SupportEmail = "support@example.com";
    private const string ErrorCodeOne = "ERR001";
    private const string ErrorCodeTwo = "ERR002";
    private const string ErrorCodeThree = "ERR003";
    private const string DeepValue = "deep value";
    private const string TraceIdValue = "trace-xyz";

    private static class CustomExtensionKeys
    {
        public const string CorrelationId = "correlationId";
        public const string Timestamp = "timestamp";
        public const string SupportContact = "supportContact";
        public const string ErrorCodes = "errorCodes";
        public const string Nested = "nested";
        public const string LevelOne = "level1";
        public const string LevelTwo = "level2";
    }

    private readonly IGrainFactory _grainFactory;

    public ProblemSerializationTests(OrleansClusterFixture fixture)
    {
        _grainFactory = fixture.Cluster.GrainFactory;
    }

    [Test]
    public async Task Problem_WithAllFields_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());

        var problem = Problem.Create(
            "Payment Processing Failed",
            "Insufficient funds in the account",
            402,
            "https://example.com/errors/payment-failed",
            "/api/payments/123");

        problem.Extensions[ProblemConstants.ExtensionKeys.TraceId] = TraceIdValue;
        problem.Extensions["accountBalance"] = 50.25m;
        problem.Extensions["requiredAmount"] = 100.00m;
        problem.Extensions[ProblemConstants.ExtensionKeys.Errors] = new Dictionary<string, List<string>>
        {
            ["payment"] = new List<string> { "Insufficient funds", "Daily limit exceeded" },
            ["account"] = new List<string> { "Account on hold" }
        };
        problem.Extensions["metadata"] = new Dictionary<string, string>
        {
            ["customerId"] = "cust-789",
            ["attemptNumber"] = "3"
        };

        // Act
        var echoed = await grain.EchoProblemAsync(problem);

        // Assert
        echoed.ShouldNotBeNull();
        echoed.Type.ShouldBe(problem.Type);
        echoed.Title.ShouldBe(problem.Title);
        echoed.StatusCode.ShouldBe(problem.StatusCode);
        echoed.Detail.ShouldBe(problem.Detail);
        echoed.Instance.ShouldBe(problem.Instance);

        echoed.Extensions.ShouldNotBeNull();
        echoed.Extensions[ProblemConstants.ExtensionKeys.TraceId].ShouldBe(TraceIdValue);
        echoed.Extensions["accountBalance"].ShouldBe(50.25m);
        echoed.Extensions["requiredAmount"].ShouldBe(100.00m);

        var errors = echoed.Extensions[ProblemConstants.ExtensionKeys.Errors]
            as Dictionary<string, List<string>>;
        errors.ShouldNotBeNull();
        errors!["payment"].ShouldContain("Insufficient funds");
        errors["payment"].ShouldContain("Daily limit exceeded");
        errors["account"].ShouldContain("Account on hold");

        var metadata = echoed.Extensions["metadata"] as Dictionary<string, string>;
        metadata.ShouldNotBeNull();
        metadata!["customerId"].ShouldBe("cust-789");
        metadata["attemptNumber"].ShouldBe("3");
    }

    [Test]
    public async Task Problem_ValidationErrors_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());

        var problem = Problem.Validation(
            ("firstName", "First name is required"),
            ("lastName", "Last name is required"),
            ("email", "Email format is invalid"),
            ("age", "Age must be between 18 and 120"),
            ("password", "Password must be at least 8 characters")
        );

        // Act
        var echoed = await grain.EchoProblemAsync(problem);

        // Assert
        echoed.ShouldNotBeNull();
        echoed.Type.ShouldBe("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        echoed.Title.ShouldBe("Validation Failed");
        echoed.StatusCode.ShouldBe(400);
        echoed.Detail.ShouldBe("One or more validation errors occurred.");

        var errors = echoed.GetValidationErrors();
        errors.ShouldNotBeNull();
        errors.ShouldHaveCount(5);
        errors!["firstName"].ShouldContain("First name is required");
        errors["lastName"].ShouldContain("Last name is required");
        errors["email"].ShouldContain("Email format is invalid");
        errors["age"].ShouldContain("Age must be between 18 and 120");
        errors["password"].ShouldContain("Password must be at least 8 characters");
    }

    [Test]
    public async Task Problem_StandardTypes_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());

        var testCases = new[]
        {
            Problem.FromStatusCode(HttpStatusCode.BadRequest, "Invalid input"),
            Problem.FromStatusCode(HttpStatusCode.Unauthorized, "Authentication required"),
            Problem.FromStatusCode(HttpStatusCode.Forbidden, "Access denied"),
            Problem.FromStatusCode(HttpStatusCode.NotFound, "Resource not found"),
            Problem.FromStatusCode(HttpStatusCode.Conflict, "Resource conflict"),
            Problem.FromStatusCode(HttpStatusCode.UnprocessableEntity, "Cannot process entity"),
            Problem.FromStatusCode(HttpStatusCode.InternalServerError, "Server error occurred"),
            Problem.FromStatusCode(HttpStatusCode.ServiceUnavailable, "Service temporarily down")
        };

        foreach (var problem in testCases)
        {
            // Act
            var echoed = await grain.EchoProblemAsync(problem);

            // Assert
            echoed.ShouldNotBeNull();
            echoed.Type.ShouldBe(problem.Type);
            echoed.Title.ShouldBe(problem.Title);
            echoed.StatusCode.ShouldBe(problem.StatusCode);
            echoed.Detail.ShouldBe(problem.Detail);
            echoed.Instance.ShouldBe(problem.Instance);
        }
    }

    [Test]
    public async Task Problem_WithCustomExtensions_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());

        var problem = Problem.FromStatusCode(HttpStatusCode.BadRequest, ValidationFailedDetail);
        problem.Extensions[CustomExtensionKeys.CorrelationId] = Guid.NewGuid().ToString();
        problem.Extensions[CustomExtensionKeys.Timestamp] = DateTime.UtcNow;
        problem.Extensions[ProblemConstants.ExtensionKeys.RetryAfter] = 60;
        problem.Extensions[CustomExtensionKeys.SupportContact] = SupportEmail;
        problem.Extensions[CustomExtensionKeys.ErrorCodes] = new[] { ErrorCodeOne, ErrorCodeTwo, ErrorCodeThree };
        problem.Extensions[CustomExtensionKeys.Nested] = new Dictionary<string, object>
        {
            [CustomExtensionKeys.LevelOne] = new Dictionary<string, object>
            {
                [CustomExtensionKeys.LevelTwo] = DeepValue
            }
        };

        // Act
        var echoed = await grain.EchoProblemAsync(problem);

        // Assert
        echoed.ShouldNotBeNull();
        echoed.Extensions.ShouldNotBeNull();
        echoed.Extensions.ShouldHaveCount(6);

        echoed.Extensions[CustomExtensionKeys.CorrelationId].ShouldNotBeNull();
        echoed.Extensions[CustomExtensionKeys.Timestamp].ShouldNotBeNull();
        echoed.Extensions[ProblemConstants.ExtensionKeys.RetryAfter].ShouldBe(60);
        echoed.Extensions[CustomExtensionKeys.SupportContact].ShouldBe(SupportEmail);

        var errorCodes = echoed.Extensions[CustomExtensionKeys.ErrorCodes] as string[];
        errorCodes.ShouldNotBeNull();
        errorCodes.ShouldBeEquivalentTo(new[] { ErrorCodeOne, ErrorCodeTwo, ErrorCodeThree });

        var nested = echoed.Extensions[CustomExtensionKeys.Nested] as Dictionary<string, object>;
        nested.ShouldNotBeNull();
        var level1 = nested![CustomExtensionKeys.LevelOne] as Dictionary<string, object>;
        level1.ShouldNotBeNull();
        level1![CustomExtensionKeys.LevelTwo].ShouldBe(DeepValue);
    }

    [Test]
    public async Task Problem_MinimalFields_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());

        var problem = Problem.Create("Internal Error", "An error occurred", 500);

        // Act
        var echoed = await grain.EchoProblemAsync(problem);

        // Assert
        echoed.ShouldNotBeNull();
        echoed.StatusCode.ShouldBe(500);
        echoed.Title.ShouldBe("Internal Error");
        echoed.Type.ShouldBe("https://httpstatuses.io/500");
        echoed.Detail.ShouldBe("An error occurred");
        echoed.Instance.ShouldBeNull();
        echoed.Extensions.ShouldNotBeNull();
        echoed.Extensions.ShouldBeEmpty();
    }

    [Test]
    public async Task Problem_WithErrorCode_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());

        var problem = Problem.FromStatusCode(HttpStatusCode.BadRequest, "Invalid request");
        problem.ErrorCode = "APP_ERROR_001";

        // Act
        var echoed = await grain.EchoProblemAsync(problem);

        // Assert
        echoed.ShouldNotBeNull();
        echoed.ErrorCode.ShouldBe("APP_ERROR_001");
        echoed.StatusCode.ShouldBe(400);
        echoed.Title.ShouldBe("BadRequest");
        echoed.Detail.ShouldBe("Invalid request");
    }
}

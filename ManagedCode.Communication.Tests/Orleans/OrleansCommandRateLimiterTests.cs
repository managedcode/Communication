using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Orleans.RateLimiting;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Orleans;

public sealed class OrleansCommandRateLimiterTests
{
    [Fact]
    public async Task AcquireAsync_MapsCommandContextAndUsesOrleansOrchestrator()
    {
        var orchestrator = new CapturingOrchestrator();
        var options = new OrleansCommandRateLimiterOptions
        {
            PolicyName = static _ => "commands",
            TenantId = static _ => "tenant-a",
            Role = static _ => "operator",
            Resource = static _ => "payments"
        };
        var limiter = new OrleansCommandRateLimiter(orchestrator, options);
        var command = Command.Create("payment.capture");
        command.UserId = "user-a";
        command.SessionId = "session-a";
        command.Metadata = new CommandMetadata { IpAddress = "127.0.0.1" };
        command.Metadata.Tags["channel"] = "api";

        await using var lease = await limiter.AcquireAsync(command);

        lease.IsAcquired.ShouldBeTrue();
        orchestrator.Context.ShouldNotBeNull();
        orchestrator.Context!.OperationName.ShouldBe("payment.capture");
        orchestrator.Context.PolicyName.ShouldBe("commands");
        orchestrator.Context.TenantId.ShouldBe("tenant-a");
        orchestrator.Context.UserId.ShouldBe("user-a");
        orchestrator.Context.GroupId.ShouldBe("session-a");
        orchestrator.Context.Role.ShouldBe("operator");
        orchestrator.Context.Resource.ShouldBe("payments");
        orchestrator.Context.IpAddress.ShouldBe("127.0.0.1");
        orchestrator.Context.Metadata["channel"].ShouldBe("api");
    }

    private sealed class CapturingOrchestrator : IRateLimitRequestOrchestrator
    {
        public RateLimitRequestContext? Context { get; private set; }

        public ValueTask<GroupLimiterHolder> CreateLimiterGroupAsync(
            RateLimitRequestContext context,
            CancellationToken cancellationToken = default)
        {
            Context = context;
            return ValueTask.FromResult(new GroupLimiterHolder());
        }
    }
}

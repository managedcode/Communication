using System;
using ManagedCode.Communication.Commands;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Commands;

public class CommandExtensionsTests
{
    [Fact]
    public void FluentSetters_ShouldAssignIdentifiers()
    {
        var command = Command<string>.Create("payload");

        command
            .WithCorrelationId("corr")
            .WithCausationId("cause")
            .WithTraceId("trace")
            .WithSpanId("span")
            .WithUserId("user")
            .WithSessionId("session");

        command.CorrelationId.ShouldBe("corr");
        command.CausationId.ShouldBe("cause");
        command.TraceId.ShouldBe("trace");
        command.SpanId.ShouldBe("span");
        command.UserId.ShouldBe("user");
        command.SessionId.ShouldBe("session");
    }

    [Fact]
    public void WithMetadata_Action_CreatesMetadataAndConfigures()
    {
        var command = Command.Create("TestCommand");

        command.Metadata.ShouldBeNull();

        command.WithMetadata(metadata =>
        {
            metadata.Source = "unit-test";
            metadata.Tags["env"] = "test";
        });

        command.Metadata.ShouldNotBeNull();
        command.Metadata!.Source.ShouldBe("unit-test");
        command.Metadata.Tags["env"].ShouldBe("test");
    }

    [Fact]
    public void WithMetadata_AssignsExistingInstance()
    {
        var command = Command.Create(Guid.CreateVersion7(), "TestCommand");
        var metadata = new CommandMetadata { UserAgent = "cli" };

        command.WithMetadata(metadata);

        command.Metadata.ShouldBeSameAs(metadata);
        command.Metadata!.UserAgent.ShouldBe("cli");
    }
}

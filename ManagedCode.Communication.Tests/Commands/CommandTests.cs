using System;
using Shouldly;
using ManagedCode.Communication.Commands;
using Xunit;

namespace ManagedCode.Communication.Tests.Commands;

public class CommandTests
{
    [Fact]
    public void FromValue()
    {
        var command = Command.From(nameof(Command));
        command.Value
            .ShouldBe(nameof(Command));
    }

    [Fact]
    public void FromIdValue()
    {
        var expectedId = Guid.NewGuid();
        var command = Command<string>.From(expectedId, nameof(Command));
        command.CommandId
            .ShouldBe(expectedId);
        command.Value
            .ShouldBe(nameof(Command));
    }
}
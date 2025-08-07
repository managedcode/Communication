using System;
using FluentAssertions;
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
            .Should()
            .Be(nameof(Command));
    }

    [Fact]
    public void FromIdValue()
    {
        var expectedId = Guid.NewGuid();
        var command = Command<string>.From(expectedId, nameof(Command));
        command.CommandId
            .Should()
            .Be(expectedId);
        command.Value
            .Should()
            .Be(nameof(Command));
    }
}
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
        var command = Command<string>.From(nameof(CommandTests), nameof(Command));
        command.CommandId
            .Should()
            .Be(nameof(CommandTests));
        command.Value
            .Should()
            .Be(nameof(Command));
    }
}
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests.Commands;

public class CommandTests
{
    [Fact]
    public void FromValue()
    {
        var command = Command.From(nameof(Command));
        command.Value.Should().Be(nameof(Command));
    }
    
    [Fact]
    public void FromIdValue()
    {
        var command = Command<string>.From(nameof(CommandTests), nameof(Command));
        command.Id.Should().Be(nameof(CommandTests));
        command.Value.Should().Be(nameof(Command));
    }
}
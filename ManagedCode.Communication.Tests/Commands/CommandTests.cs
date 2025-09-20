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

    [Fact]
    public void Create_WithEnumType_ShouldSetCommandType()
    {
        var command = Command.Create(TestCommandType.Delete);

        command.CommandType
            .ShouldBe(TestCommandType.Delete.ToString());
        command.CommandId
            .ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyCommandType_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => Command.Create(Guid.NewGuid(), string.Empty));
    }

    [Fact]
    public void GenericCreate_WithValueFactory_ShouldInvokeFactoryOnce()
    {
        var callCount = 0;

        var command = Command<string>.Create(() =>
        {
            callCount++;
            return "payload";
        });

        callCount
            .ShouldBe(1);
        command.Value
            .ShouldBe("payload");
    }

    [Fact]
    public void GenericCreate_WithEmptyCommandType_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => Command<string>.Create(Guid.NewGuid(), string.Empty, "value"));
    }

    [Fact]
    public void GenericFrom_WithCommandType_ShouldReturnCommand()
    {
        var id = Guid.NewGuid();
        var command = Command<string>.From(id, "custom", "value");

        command.CommandId
            .ShouldBe(id);
        command.CommandType
            .ShouldBe("custom");
        command.Value
            .ShouldBe("value");
    }

    private enum TestCommandType
    {
        Create,
        Update,
        Delete
    }
}

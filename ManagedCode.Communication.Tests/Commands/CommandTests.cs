using System;
using ManagedCode.Communication.Commands;
using Shouldly;

namespace ManagedCode.Communication.Tests.Commands;

public class CommandTests
{
    [Test]
    public void FromValue()
    {
        var command = Command.From(nameof(Command));
        command.Value
            .ShouldBe(nameof(Command));
    }

    [Test]
    public void FromIdValue()
    {
        var expectedId = Guid.NewGuid();
        var command = Command<string>.From(nameof(Command), expectedId);
        command.CommandId
            .ShouldBe(expectedId);
        command.Value
            .ShouldBe(nameof(Command));
    }

    [Test]
    public void Create_WithEnumType_ShouldSetCommandType()
    {
        var command = Command.Create(TestCommandType.Delete);

        command.CommandType
            .ShouldBe(TestCommandType.Delete.ToString());
        command.CommandId
            .ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void Create_WithEmptyCommandType_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => Command.Create(string.Empty, Guid.NewGuid()));
    }

    [Test]
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

    [Test]
    public void GenericCreate_WithEmptyCommandType_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() => Command<string>.Create(string.Empty, "value", Guid.NewGuid()));
    }

    [Test]
    public void GenericFrom_WithCommandType_ShouldReturnCommand()
    {
        var id = Guid.NewGuid();
        var command = Command<string>.From("custom", "value", id);

        command.CommandId
            .ShouldBe(id);
        command.CommandType
            .ShouldBe("custom");
        command.Value
            .ShouldBe("value");
    }

    [Test]
    public void Create_ShouldStampTimestampWithUtcNow()
    {
        var before = DateTime.UtcNow;

        var command = Command.Create("TimestampTest");

        var after = DateTime.UtcNow;
        command.Timestamp.ShouldBeInRange(before, after);
        command.Timestamp.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Test]
    public void Create_ShouldUseVersion7CommandId()
    {
        var command = Command.Create("VersionTest");

        GetGuidVersion(command.CommandId).ShouldBe(7);
    }

    [Test]
    public void GenericCreate_WithDerivedValue_ShouldUseDerivedTypeName()
    {
        var payload = new DerivedPayload();

        var command = Command<BasePayload>.Create(payload);

        command.CommandType.ShouldBe(nameof(DerivedPayload));
    }

    private static int GetGuidVersion(Guid guid)
    {
        var bytes = guid.ToByteArray();
        return (bytes[7] >> 4) & 0x0F;
    }

    private class BasePayload
    {
    }

    private sealed class DerivedPayload : BasePayload
    {
    }

    private enum TestCommandType
    {
        Create,
        Update,
        Delete
    }
}

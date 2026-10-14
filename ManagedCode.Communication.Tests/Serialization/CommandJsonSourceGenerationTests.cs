using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Commands;
using Shouldly;

namespace ManagedCode.Communication.Tests.Serialization;

public class CommandJsonSourceGenerationTests
{
    private const string CommandName = "command.deserialize";
    private const string Correlation = "correlation";
    private const string User = "creator";
    private const string PayloadName = "payload";
    private const string TagName = "source";
    private const string TagValue = "generated";

    [Test]
    public void Command_ShouldRoundTripThroughGeneratedMetadata()
    {
        var command = Command.Create(CommandName).WithCorrelationId(Correlation).WithUserId(User)
            .WithMetadata(metadata => metadata.Tags[TagName] = TagValue);
        var json = JsonSerializer.Serialize(command, CommandJsonContracts.Default.Command);
        var restored = JsonSerializer.Deserialize(json, CommandJsonContracts.Default.Command);
        restored.ShouldNotBeNull();
        AssertEnvelope(command, restored);
        restored.Metadata!.Tags[TagName].ShouldBe(TagValue);
    }

    [Test]
    public void TypedCommand_ShouldRoundTripThroughGeneratedMetadata()
    {
        var command = Command<CommandJsonPayload>.Create(new CommandJsonPayload(PayloadName, 3))
            .WithCorrelationId(Correlation).WithUserId(User);
        var json = JsonSerializer.Serialize(command, CommandJsonContracts.Default.CommandCommandJsonPayload);
        var restored = JsonSerializer.Deserialize(json, CommandJsonContracts.Default.CommandCommandJsonPayload);
        restored.ShouldNotBeNull();
        AssertEnvelope(command, restored);
        restored.Value.ShouldBe(command.Value);
    }

    [Test]
    public void PaginationCommand_ShouldRoundTripThroughGeneratedMetadata()
    {
        var command = PaginationCommand.Create(12, 6);
        var json = JsonSerializer.Serialize(command, CommandJsonContracts.Default.PaginationCommand);
        var restored = JsonSerializer.Deserialize(json, CommandJsonContracts.Default.PaginationCommand);
        restored.ShouldNotBeNull();
        AssertEnvelope(command, restored);
        restored.Skip.ShouldBe(command.Skip);
        restored.Take.ShouldBe(command.Take);
        restored.PageNumber.ShouldBe(command.PageNumber);
    }

    private static void AssertEnvelope(ICommand expected, ICommand actual)
    {
        actual.GetType().GetConstructor(Type.EmptyTypes).ShouldNotBeNull();
        actual.CommandId.ShouldBe(expected.CommandId);
        actual.CommandType.ShouldBe(expected.CommandType);
        actual.Timestamp.ShouldBe(expected.Timestamp);
        actual.CorrelationId.ShouldBe(expected.CorrelationId);
        actual.UserId.ShouldBe(expected.UserId);
    }
}

public sealed record CommandJsonPayload(string Name, int Count);

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Command))]
[JsonSerializable(typeof(Command<CommandJsonPayload>))]
[JsonSerializable(typeof(PaginationCommand))]
internal partial class CommandJsonContracts : JsonSerializerContext;

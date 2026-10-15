using ManagedCode.Communication.Commands;
using Shouldly;

namespace ManagedCode.Communication.Tests.Serialization;

public sealed class CommandJsonContractTests
{
    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void Command_PreservesEveryEnvelopeAndMetadataField(JsonContractMode mode)
    {
        var expected = SerializationContract.WithEnvelope(Command.Create(SerializationContract.Name));
        var actual = SerializationContract.RoundTrip(expected, mode);
        SerializationContract.AssertEnvelope(expected, actual, mode);
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void TypedCommand_PreservesPayloadAndCompleteEnvelope(JsonContractMode mode)
    {
        var expected = SerializationContract.WithEnvelope(Command<ContractPayload>.Create(
            SerializationContract.Name, new ContractPayload(SerializationContract.Name, 42)));
        var actual = SerializationContract.RoundTrip(expected, mode);
        SerializationContract.AssertEnvelope(expected, actual, mode);
        actual.Value.ShouldBe(expected.Value);
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void Commands_PreserveNullAndDefaultValuedPayloads(JsonContractMode mode)
    {
        var empty = Command<string?>.Create((string?)null);
        var restored = SerializationContract.RoundTrip(empty, mode);
        restored.CommandId.ShouldBe(empty.CommandId);
        restored.Value.ShouldBeNull();
        restored.IsEmpty.ShouldBeTrue();
        restored.Metadata.ShouldBeNull();
        var zero = Command<bool>.Create(false);
        var restoredZero = SerializationContract.RoundTrip(zero, mode);
        restoredZero.CommandId.ShouldBe(zero.CommandId);
        restoredZero.Value.ShouldBeFalse();
        restoredZero.IsEmpty.ShouldBeFalse();
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void Pagination_PreservesCustomLimitsAndExactEnvelope(JsonContractMode mode)
    {
        var options = new PaginationOptions(defaultPageSize: 250, maxPageSize: 1000, minPageSize: 1);
        var expected = SerializationContract.WithEnvelope(PaginationCommand.Create(750, 500, options));
        expected.CommandType = SerializationContract.Name;
        var actual = SerializationContract.RoundTrip(expected, mode);
        SerializationContract.AssertEnvelope(expected, actual, mode);
        actual.Value.ShouldBe(expected.Value);
        actual.Skip.ShouldBe(750);
        actual.Take.ShouldBe(500);
        actual.PageNumber.ShouldBe(expected.PageNumber);
        actual.PageSize.ShouldBe(expected.PageSize);
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void CommandContainingResult_PreservesFailureAndPartialPayload(JsonContractMode mode)
    {
        var failure = Result<ContractPayload>.Fail(SerializationContract.Failure()) with
        { Value = new ContractPayload(SerializationContract.Name, 0) };
        var expected = Command<Result<ContractPayload>>.Create(failure);
        var actual = SerializationContract.RoundTrip(expected, mode);
        actual.CommandId.ShouldBe(expected.CommandId);
        actual.Value.IsFailed.ShouldBeTrue();
        actual.Value.Value.ShouldBe(failure.Value);
        SerializationContract.AssertProblem(failure.Problem!, actual.Value.Problem, mode);
    }
}

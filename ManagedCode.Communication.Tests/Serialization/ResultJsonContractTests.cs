using System;
using System.Text.Json;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using Shouldly;

namespace ManagedCode.Communication.Tests.Serialization;

public sealed class ResultJsonContractTests
{
    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void Results_PreserveSuccessesAndLegitimateDefaultValues(JsonContractMode mode)
    {
        SerializationContract.RoundTrip(Result.Succeed(), mode).IsSuccess.ShouldBeTrue();
        var zero = SerializationContract.RoundTrip(Result<int>.Succeed(0), mode);
        zero.IsSuccess.ShouldBeTrue();
        zero.Value.ShouldBe(0);
        zero.Problem.ShouldBeNull();
        var boolean = SerializationContract.RoundTrip(Result<bool>.Succeed(false), mode);
        boolean.IsSuccess.ShouldBeTrue();
        boolean.Value.ShouldBeFalse();
        var nullable = SerializationContract.RoundTrip(Result<int?>.Succeed(0), mode);
        nullable.IsSuccess.ShouldBeTrue();
        nullable.Value.ShouldBe(0);
        var payload = new ContractPayload(SerializationContract.Name, 42);
        SerializationContract.RoundTrip(Result<ContractPayload>.Succeed(payload), mode).Value.ShouldBe(payload);
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void Results_PreserveFailuresAndPartialValues(JsonContractMode mode)
    {
        var problem = SerializationContract.Failure();
        var untyped = SerializationContract.RoundTrip(Result.Fail(problem), mode);
        untyped.IsFailed.ShouldBeTrue();
        SerializationContract.AssertProblem(problem, untyped.Problem, mode);
        var empty = SerializationContract.RoundTrip(Result<ContractPayload>.Fail(problem), mode);
        empty.IsFailed.ShouldBeTrue();
        empty.Value.ShouldBeNull();
        SerializationContract.AssertProblem(problem, empty.Problem, mode);
        var failure = Result<ContractPayload>.Fail(problem) with
        { Value = new ContractPayload(SerializationContract.Name, 7) };
        var partial = SerializationContract.RoundTrip(failure, mode);
        partial.IsFailed.ShouldBeTrue();
        partial.Value.ShouldBe(failure.Value);
        SerializationContract.AssertProblem(problem, partial.Problem, mode);
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void CollectionResults_PreservePaginationEmptyPagesAndFailureDetails(JsonContractMode mode)
    {
        var payload = new ContractPayload(SerializationContract.Name, 7);
        var page = CollectionResult<ContractPayload>.Succeed([payload], 3, 10, 27);
        var actual = SerializationContract.RoundTrip(page, mode);
        actual.IsSuccess.ShouldBeTrue();
        actual.Collection.ShouldBe(page.Collection);
        actual.PageNumber.ShouldBe(page.PageNumber);
        actual.PageSize.ShouldBe(page.PageSize);
        actual.TotalItems.ShouldBe(page.TotalItems);
        actual.TotalPages.ShouldBe(page.TotalPages);
        actual.Problem.ShouldBeNull();
        var empty = SerializationContract.RoundTrip(CollectionResult<ContractPayload>.Empty(), mode);
        empty.IsSuccess.ShouldBeTrue();
        empty.Collection.ShouldNotBeNull();
        empty.Collection.ShouldBeEmpty();
        var failure = CollectionResult<ContractPayload>.Fail(SerializationContract.Failure(), [payload]) with
        { PageNumber = 3, PageSize = 10, TotalItems = 27, TotalPages = 3 };
        var failed = SerializationContract.RoundTrip(failure, mode);
        failed.IsFailed.ShouldBeTrue();
        failed.Collection.ShouldBe(failure.Collection);
        failed.PageNumber.ShouldBe(failure.PageNumber);
        failed.PageSize.ShouldBe(failure.PageSize);
        failed.TotalItems.ShouldBe(failure.TotalItems);
        failed.TotalPages.ShouldBe(failure.TotalPages);
        SerializationContract.AssertProblem(failure.Problem!, failed.Problem, mode);
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void ResultContainingCommand_PreservesTheNestedCommand(JsonContractMode mode)
    {
        var command = SerializationContract.WithEnvelope(Command<ContractPayload>.Create(
            new ContractPayload(SerializationContract.Name, 42)));
        var actual = SerializationContract.RoundTrip(Result<Command<ContractPayload>>.Succeed(command), mode);
        actual.IsSuccess.ShouldBeTrue();
        actual.Value.ShouldNotBeNull();
        SerializationContract.AssertEnvelope(command, actual.Value, mode);
        actual.Value.Value.ShouldBe(command.Value);
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void Results_RejectNonBooleanFlagsAndTruncatedDocuments(JsonContractMode mode)
    {
        var options = SerializationContract.Options(mode);
        foreach (var json in new[] { SerializationContract.NumericFlag, SerializationContract.StringFlag,
            SerializationContract.NullFlag, SerializationContract.ObjectFlag, SerializationContract.ArrayFlag,
            SerializationContract.Truncated })
        {
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Result>(json, options));
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Result<int>>(json, options));
        }
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void Results_RejectNullSuccessValuesAndKeepCollectionsNonNull(JsonContractMode mode)
    {
        var options = SerializationContract.Options(mode);
        foreach (var json in new[] { SerializationContract.NullSuccess, SerializationContract.MissingSuccessValue })
        {
            Should.Throw<ArgumentNullException>(() => JsonSerializer.Deserialize<Result<string>>(json, options));
            Should.Throw<ArgumentNullException>(() => JsonSerializer.Deserialize<Result<int?>>(json, options));
        }
        var empty = JsonSerializer.Deserialize<CollectionResult<ContractPayload>>(SerializationContract.NullCollection, options);
        empty.IsSuccess.ShouldBeTrue();
        empty.Collection.ShouldNotBeNull();
        empty.Collection.ShouldBeEmpty();
    }
}

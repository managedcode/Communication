using System;
using System.Text.Json;
using ManagedCode.Communication.CollectionResultT;
using Shouldly;

namespace ManagedCode.Communication.Tests.Results;

public class ResultSuccessValueContractTests
{
    private const string Payload = "payload";
    private const string Failure = "missing";
    private const string ValueParameter = "value";
    private const string NullSuccessJson = "{\"isSuccess\":true,\"value\":null}";

    [Test]
    public void Succeed_WithNullReference_RejectsNullPayload()
    {
        Should.Throw<ArgumentNullException>(() => Result<string>.Succeed((string)null!))
            .ParamName.ShouldBe(ValueParameter);
    }

    [Test]
    public void Succeed_WithNullableNull_RejectsNullPayload()
    {
        Should.Throw<ArgumentNullException>(() => Result<string?>.Succeed((string?)null));
        Should.Throw<ArgumentNullException>(() => Result<int?>.Succeed((int?)null));
    }

    [Test]
    public void Succeed_WithDefaultReference_RejectsNullPayload()
    {
        Should.Throw<ArgumentNullException>(() => Result<string>.Succeed());
    }

    [Test]
    public void Succeed_WithDefaultValueType_PreservesValidDefault()
    {
        var result = Result<int>.Succeed();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0);
    }

    [Test]
    public void Initializer_WithNullSuccessPayload_RejectsOverride()
    {
        var success = Result<string>.Succeed(Payload);
        Should.Throw<ArgumentNullException>(() => { _ = success with { Value = null }; });
        success.IsSuccess.ShouldBeTrue();
        success.Value.ShouldBe(Payload);
    }

    [Test]
    public void Failure_WithNullPayload_PreservesFailure()
    {
        var result = Result<string>.FailNotFound(Failure) with { Value = null };
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
    }

    [Test]
    public void CollectionSuccess_WithNullInitializer_PreservesEmptyNonNullCollection()
    {
        var result = CollectionResult<string>.Succeed(Array.Empty<string>()) with { Collection = null! };
        result.IsSuccess.ShouldBeTrue();
        result.Collection.ShouldBeEmpty();
        result.Value.ShouldBeSameAs(result.Collection);
    }

    [Test]
    public void CollectionSuccess_FromDefaultStruct_PreservesNonNullCollection()
    {
        var result = default(CollectionResult<string>) with { IsSuccess = true };
        result.Collection.ShouldBeEmpty();
        result.Value.ShouldBeSameAs(result.Collection);
    }

    [Test]
    public void Deserialize_WithNullSuccessPayload_RejectsInvalidResult()
    {
        Should.Throw<ArgumentNullException>(() => JsonSerializer.Deserialize<Result<string>>(NullSuccessJson));
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using ManagedCode.Communication.Orleans.Converters;
using ManagedCode.Communication.Orleans.Surrogates;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Serialization;
using Orleans.Storage;
using Orleans.TestingHost;
using Shouldly;

namespace ManagedCode.Communication.Tests.Orleans;

[ClassDataSource<OrleansClusterFixture>(Shared = SharedType.PerClass)]
[NotInParallel(nameof(OrleansNativeStorageSerializationTests))]
public sealed class OrleansNativeStorageSerializationTests(OrleansClusterFixture fixture)
{
    private const string FailureTitle = "native-storage-failure";
    private const string FailureDetail = "Preserve stored failure";
    private const string ExtensionKey = "owner";
    private const string ExtensionValue = "creator";

    private OrleansGrainStorageSerializer Serializer() => new(
        ((InProcessSiloHandle)fixture.Cluster.Primary!).ServiceProvider.GetRequiredService<Serializer>());

    [Test]
    [Arguments(0)]
    [Arguments(42)]
    public void NativeStoragePreservesSuccessfulResultFlagAndValue(int value)
    {
        var serializer = Serializer();
        var expected = Result<int>.Succeed(value);
        var restored = serializer.Deserialize<Result<int>>(serializer.Serialize(expected));
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.ShouldBe(value);
    }

    [Test]
    [Arguments(0)]
    [Arguments(42)]
    public void NativeStoragePreservesFailedPayloadProblemAndNestedResults(int value)
    {
        var problem = Problem.Create(FailureTitle, FailureDetail, (int)HttpStatusCode.Conflict);
        problem.Extensions[ExtensionKey] = ExtensionValue;
        var failure = new ResultTSurrogateConverter<int>().ConvertFromSurrogate(
            new ResultTSurrogate<int>(false, value, problem));
        var expected = new StoredState
        {
            Update = failure,
            History = new List<Result<int>> { Result<int>.Succeed(0), failure }
        };
        var serializer = Serializer();
        var restored = serializer.Deserialize<StoredState>(serializer.Serialize(expected))!;
        restored.Update.IsFailed.ShouldBeTrue();
        restored.Update.Value.ShouldBe(value);
        restored.Update.Problem!.Title.ShouldBe(problem.Title);
        restored.Update.Problem.Detail.ShouldBe(problem.Detail);
        restored.Update.Problem.StatusCode.ShouldBe(problem.StatusCode);
        restored.Update.Problem.Extensions[ExtensionKey].ShouldBe(ExtensionValue);
        restored.History.Count.ShouldBe(expected.History.Count);
        restored.History[0].IsSuccess.ShouldBeTrue();
        restored.History[0].Value.ShouldBe(0);
        restored.History[1].IsFailed.ShouldBeTrue();
        restored.History[1].Problem!.Title.ShouldBe(problem.Title);
    }

    [Test]
    public void NativeStoragePreservesUntypedAndDefaultValuedResults()
    {
        var serializer = Serializer();
        serializer.Deserialize<Result>(serializer.Serialize(Result.Succeed())).IsSuccess.ShouldBeTrue();
        var failure = Result.Fail(Problem.Create(FailureTitle, FailureDetail, (int)HttpStatusCode.Conflict));
        serializer.Deserialize<Result>(serializer.Serialize(failure)).Problem!.Detail.ShouldBe(FailureDetail);
        var restored = serializer.Deserialize<Result<bool>>(serializer.Serialize(Result<bool>.Succeed(false)));
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.ShouldBeFalse();
    }

    [Test]
    public void NativeStorageRejectsInvalidBinaryState()
    {
        var serializer = Serializer();
        Should.Throw<Exception>(() => serializer.Deserialize<StoredState>(
            new BinaryData(new byte[] { 255, 255, 255 })));
    }

    [GenerateSerializer]
    internal sealed class StoredState
    {
        [Id(0)] public Result<int> Update { get; init; }
        [Id(1)] public List<Result<int>> History { get; init; } = new();
    }
}

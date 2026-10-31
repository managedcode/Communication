using System;
using System.Collections.Generic;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Orleans.Converters;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Shouldly;

namespace ManagedCode.Communication.Tests.Orleans.Serialization;

public sealed class NativeDeepCopyTests
{
    private const string CommandType = "owned-command";
    private const string Tag = "owner";
    private const string Original = "original";
    private const string Changed = "changed";
    private const string Failure = "owned-failure";

    [Test]
    public void CommandPayloadAndMetadataRemainOwnedAfterCallerMutation()
    {
        using var services = CreateServices();
        var input = new byte[] { 1, 2, 3 };
        var command = Command<byte[]>.Create(CommandType, input);
        command.Metadata = Metadata();
        var copy = services.GetRequiredService<DeepCopier>().Copy(command);
        input[0] = 99;
        command.Metadata.Tags[Tag] = Changed;
        ((byte[])command.Metadata.Properties[Tag]!)[0] = 99;
        copy.Value.ShouldBe(new byte[] { 1, 2, 3 });
        copy.Metadata!.Tags[Tag].ShouldBe(Original);
        ((byte[])copy.Metadata.Properties[Tag]!)[0].ShouldBe((byte)1);
        copy.CommandId.ShouldBe(command.CommandId);
        copy.Timestamp.ShouldBe(command.Timestamp);
        var following = services.GetRequiredService<DeepCopier>().Copy(copy);
        copy.Value![1] = 77;
        following.Value.ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Test]
    public void UntypedCommandRetainsIndependentNestedMetadata()
    {
        using var services = CreateServices();
        var command = Command.Create(CommandType);
        command.Metadata = Metadata();
        var copy = services.GetRequiredService<DeepCopier>().Copy(command);
        command.Metadata.Tags.Clear();
        copy.Metadata!.Tags[Tag].ShouldBe(Original);
        ((byte[])command.Metadata.Extensions[Tag]!)[0] = 99;
        ((byte[])copy.Metadata.Extensions[Tag]!)[0].ShouldBe((byte)1);
    }

    [Test]
    public void MetadataCopiesOwnEveryMutableDictionaryAndNestedPayload()
    {
        using var services = CreateServices();
        var metadata = Metadata();
        var copy = services.GetRequiredService<DeepCopier>().Copy(metadata);
        metadata.Tags[Tag] = Changed;
        ((byte[])metadata.Properties[Tag]!)[0] = 99;
        ((byte[])metadata.Extensions[Tag]!)[0] = 99;
        copy.Tags[Tag].ShouldBe(Original);
        ((byte[])copy.Properties[Tag]!)[0].ShouldBe((byte)1);
        ((byte[])copy.Extensions[Tag]!)[0].ShouldBe((byte)1);
    }

    [Test]
    public void SuccessfulResultOwnsItsMutablePayload()
    {
        using var services = CreateServices();
        var input = new byte[] { 1, 2, 3 };
        var result = Result<byte[]>.Succeed(input);
        var copy = services.GetRequiredService<DeepCopier>().Copy(result);
        input[0] = 99;
        copy.IsSuccess.ShouldBeTrue();
        copy.Value.ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Test]
    public void ProblemAndFailedResultsOwnNestedExtensionPayloads()
    {
        using var services = CreateServices();
        var problem = OwnedProblem();
        var copier = services.GetRequiredService<DeepCopier>();
        var copy = copier.Copy(problem);
        var untyped = copier.Copy(Result.Fail(problem));
        var typed = copier.Copy(Result<byte[]>.Fail(problem));
        ((byte[])problem.Extensions[Tag]!)[0] = 99;
        ((byte[])copy.Extensions[Tag]!)[0].ShouldBe((byte)1);
        ((byte[])untyped.Problem!.Extensions[Tag]!)[0].ShouldBe((byte)1);
        ((byte[])typed.Problem!.Extensions[Tag]!)[0].ShouldBe((byte)1);
        untyped.IsFailed.ShouldBeTrue();
        typed.IsFailed.ShouldBeTrue();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public void CollectionOwnsItemsAndFailureExtensions(bool failed)
    {
        using var services = CreateServices();
        var item = new byte[] { 1, 2, 3 };
        var problem = OwnedProblem();
        var source = failed ? CollectionResult<byte[]>.Fail(problem, new[] { item })
            : CollectionResult<byte[]>.Succeed(new[] { item });
        var copy = services.GetRequiredService<DeepCopier>().Copy(source);
        item[0] = 99;
        copy.Collection![0].ShouldBe(new byte[] { 1, 2, 3 });
        copy.IsFailed.ShouldBe(failed);
        if (failed)
        {
            ((byte[])problem.Extensions[Tag]!)[0] = 99;
            ((byte[])copy.Problem!.Extensions[Tag]!)[0].ShouldBe((byte)1);
        }
    }

    [Test]
    public void CqrsProgressAndFinalChunksOwnTheirPayloads()
    {
        using var services = CreateServices();
        var input = new byte[] { 1, 2, 3 };
        var progress = CqrsStreamChunk<byte[], byte[]>.Progress(input);
        var final = CqrsStreamChunk<byte[], byte[]>.FromResult(Result<byte[]>.Succeed(input));
        var copier = services.GetRequiredService<DeepCopier>();
        var progressCopy = copier.Copy(progress);
        var finalCopy = copier.Copy(final);
        input[0] = 99;
        progressCopy.ProgressResult!.Value.Value.ShouldBe(new byte[] { 1, 2, 3 });
        finalCopy.Final!.Value.Value.ShouldBe(new byte[] { 1, 2, 3 });
        progressCopy.Kind.ShouldBe(progress.Kind);
        finalCopy.Kind.ShouldBe(final.Kind);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSerializer(builder => builder.AddAssembly(typeof(CommandTSurrogateConverter<>).Assembly));
        return services.BuildServiceProvider();
    }

    private static CommandMetadata Metadata() => new()
    {
        Tags = new Dictionary<string, string> { [Tag] = Original },
        Properties = new Dictionary<string, object?> { [Tag] = new byte[] { 1, 2, 3 } },
        Extensions = new Dictionary<string, object?> { [Tag] = new byte[] { 1, 2, 3 } }
    };

    private static Problem OwnedProblem()
    {
        var problem = Problem.Create(Failure, Original, 409);
        problem.Extensions[Tag] = new byte[] { 1, 2, 3 };
        return problem;
    }
}

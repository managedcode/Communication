using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using Shouldly;

namespace ManagedCode.Communication.Tests.Serialization;

public enum JsonContractMode { Reflection, Metadata, GeneratedDefault }

internal static class SerializationContract
{
    internal const string Name = "serialization.contract";
    internal const string Detail = "Preserve failure details";
    internal const string Tag = "scope";
    internal const string Correlation = "correlation";
    internal const string Causation = "causation";
    internal const string Trace = "trace";
    internal const string Span = "span";
    internal const string User = "creator";
    internal const string Session = "session";
    internal const string State = "trace=state";
    internal const string Address = "127.0.0.1";
    internal const string Instance = "/operations/42";
    internal const string NullSuccess = "{\"isSuccess\":true,\"value\":null}";
    internal const string MissingSuccessValue = "{\"isSuccess\":true}";
    internal const string NullCollection = "{\"isSuccess\":true,\"collection\":null}";
    internal const string NumericFlag = "{\"isSuccess\":1}";
    internal const string StringFlag = "{\"isSuccess\":\"true\"}";
    internal const string NullFlag = "{\"isSuccess\":null}";
    internal const string ObjectFlag = "{\"isSuccess\":{}}";
    internal const string ArrayFlag = "{\"isSuccess\":[]}";
    internal const string Truncated = "{\"isSuccess\":true,";
    internal static readonly DateTime Timestamp = new(2026, 10, 15, 4, 3, 2, DateTimeKind.Utc);

    internal static JsonSerializerOptions Options(JsonContractMode mode) => mode switch
    {
        JsonContractMode.Reflection => new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        },
        JsonContractMode.Metadata => MetadataContractContext.Default.Options,
        JsonContractMode.GeneratedDefault => DefaultContractContext.Default.Options,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    internal static T RoundTrip<T>(T expected, JsonContractMode mode) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(expected, Options(mode)), Options(mode))!;

    internal static T WithEnvelope<T>(T command) where T : ICommand
    {
        command.Timestamp = Timestamp;
        command.CorrelationId = Correlation;
        command.CausationId = Causation;
        command.TraceId = Trace;
        command.SpanId = Span;
        command.UserId = User;
        command.SessionId = Session;
        command.Metadata = new CommandMetadata
        {
            InitiatedBy = User,
            Source = Name,
            Target = Name,
            IpAddress = Address,
            UserAgent = Name,
            SessionId = Session,
            TraceId = Trace,
            SpanId = Span,
            TraceState = State,
            TraceRecorded = true,
            Version = 2,
            Priority = CommandPriority.Critical,
            RetryCount = 2,
            MaxRetries = 7,
            TimeoutSeconds = 45,
            ExecutionTime = TimeSpan.FromTicks(1234567),
            TimeToLiveSeconds = 120,
            Properties = { [Tag] = 7 },
            Tags = { [Tag] = Name },
            Extensions = { [Tag] = true }
        };
        return command;
    }

    internal static void AssertEnvelope(ICommand expected, ICommand actual, JsonContractMode mode)
    {
        actual.CommandId.ShouldBe(expected.CommandId);
        actual.CommandType.ShouldBe(expected.CommandType);
        actual.Timestamp.ShouldBe(expected.Timestamp);
        actual.Timestamp.Kind.ShouldBe(DateTimeKind.Utc);
        actual.CorrelationId.ShouldBe(expected.CorrelationId);
        actual.CausationId.ShouldBe(expected.CausationId);
        actual.TraceId.ShouldBe(expected.TraceId);
        actual.SpanId.ShouldBe(expected.SpanId);
        actual.UserId.ShouldBe(expected.UserId);
        actual.SessionId.ShouldBe(expected.SessionId);
        JsonSerializer.Serialize(actual.Metadata, Options(mode)).ShouldBe(JsonSerializer.Serialize(expected.Metadata, Options(mode)));
    }

    internal static Problem Failure()
    {
        var problem = Problem.Create(Name, Detail, 409);
        problem.Instance = Instance;
        problem.Extensions[Tag] = Name;
        return problem;
    }

    internal static void AssertProblem(Problem expected, Problem? actual, JsonContractMode mode)
    {
        actual.ShouldNotBeNull();
        actual.Type.ShouldBe(expected.Type);
        actual.Title.ShouldBe(expected.Title);
        actual.Detail.ShouldBe(expected.Detail);
        actual.StatusCode.ShouldBe(expected.StatusCode);
        actual.Instance.ShouldBe(expected.Instance);
        JsonSerializer.Serialize(actual.Extensions, Options(mode)).ShouldBe(JsonSerializer.Serialize(expected.Extensions, Options(mode)));
    }
}

public sealed record ContractPayload(string Name, int Count);

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Command))]
[JsonSerializable(typeof(Command<ContractPayload>))]
[JsonSerializable(typeof(Command<string>))]
[JsonSerializable(typeof(Command<bool>))]
[JsonSerializable(typeof(Command<Result<ContractPayload>>))]
[JsonSerializable(typeof(PaginationCommand))]
[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(Result<ContractPayload>))]
[JsonSerializable(typeof(Result<Command<ContractPayload>>))]
[JsonSerializable(typeof(Result<int>))]
[JsonSerializable(typeof(Result<bool>))]
[JsonSerializable(typeof(Result<int?>))]
[JsonSerializable(typeof(Result<string>))]
[JsonSerializable(typeof(CollectionResult<ContractPayload>))]
[JsonSerializable(typeof(Problem))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(IDictionary<string, object?>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(bool))]
internal partial class MetadataContractContext : JsonSerializerContext;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Command))]
[JsonSerializable(typeof(Command<ContractPayload>))]
[JsonSerializable(typeof(Command<string>))]
[JsonSerializable(typeof(Command<bool>))]
[JsonSerializable(typeof(Command<Result<ContractPayload>>))]
[JsonSerializable(typeof(PaginationCommand))]
[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(Result<ContractPayload>))]
[JsonSerializable(typeof(Result<Command<ContractPayload>>))]
[JsonSerializable(typeof(Result<int>))]
[JsonSerializable(typeof(Result<bool>))]
[JsonSerializable(typeof(Result<int?>))]
[JsonSerializable(typeof(Result<string>))]
[JsonSerializable(typeof(CollectionResult<ContractPayload>))]
[JsonSerializable(typeof(Problem))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(IDictionary<string, object?>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(bool))]
internal partial class DefaultContractContext : JsonSerializerContext;

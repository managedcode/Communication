using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ManagedCode.Communication.Orleans.Converters;
using ManagedCode.Communication.Orleans.Extensions;
using ManagedCode.Communication.Orleans.Surrogates;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans.Serialization;
using Orleans.Storage;
using Orleans.TestingHost;
using Shouldly;

namespace ManagedCode.Communication.Tests.Orleans;

[ClassDataSource<OrleansClusterFixture>(Shared = SharedType.PerClass)]
[NotInParallel(nameof(OrleansJsonStorageSerializationTests))]
public sealed class OrleansJsonStorageSerializationTests(OrleansClusterFixture fixture)
{
    private const string FailureTitle = "native-storage-failure";
    private const string FailureDetail = "Preserve native stored failure";
    private const string ExtensionKey = "owner";
    private const string ExtensionValue = "creator";
    private const string LegacySuccess = "{\"IsSuccess\":true,\"Value\":42,\"Problem\":null}";
    private const string MissingFlag = "{\"Value\":42}";
    private const string ConflictingFlags = "{\"IsSuccess\":true,\"IsFailed\":true,\"Value\":42}";
    private const string InvalidFlag = "{\"IsSuccess\":\"true\",\"Value\":42}";

    private JsonGrainStorageSerializer Serializer() => new(
        ((InProcessSiloHandle)fixture.Cluster.Primary!).ServiceProvider.GetRequiredService<OrleansJsonSerializer>());

    [Test]
    [Arguments(0)]
    [Arguments(42)]
    public void JsonStorageMustPreserveSuccessfulResultFlagAndValue(int value)
    {
        var serializer = Serializer();
        var expected = Result<int>.Succeed(value);
        var restored = serializer.Deserialize<Result<int>>(serializer.Serialize(expected));
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.ShouldBe(expected.Value);
    }

    [Test]
    public void JsonStorageMustPreserveFailedProblemValueAndNestedResults()
    {
        var problem = Problem.Create(FailureTitle, FailureDetail, (int)HttpStatusCode.Conflict);
        problem.Extensions[ExtensionKey] = ExtensionValue;
        var expected = new ResultTSurrogateConverter<int>().ConvertFromSurrogate(new ResultTSurrogate<int>(false, 42, problem));
        var serializer = Serializer();
        var restored = serializer.Deserialize<StoredState>(serializer.Serialize(new StoredState { Update = expected }));
        restored!.Update.IsFailed.ShouldBeTrue();
        restored.Update.Value.ShouldBe(42);
        restored.Update.Problem!.Title.ShouldBe(FailureTitle);
        restored.Update.Problem.Detail.ShouldBe(FailureDetail);
        restored.Update.Problem.StatusCode.ShouldBe(problem.StatusCode);
        restored.Update.Problem.Extensions[ExtensionKey]?.ToString().ShouldBe(ExtensionValue);
    }

    [Test]
    public void JsonStorageMustPreserveUntypedAndDefaultValuedResults()
    {
        var serializer = Serializer();
        serializer.Deserialize<Result>(serializer.Serialize(Result.Succeed())).IsSuccess.ShouldBeTrue();
        serializer.Deserialize<Result>(serializer.Serialize(Result.Fail(Problem.Create(FailureTitle, FailureDetail, 409)))).IsFailed.ShouldBeTrue();
        serializer.Deserialize<Result<bool>>(serializer.Serialize(Result<bool>.Succeed(false))).IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void NativeLegacyPascalCaseSuccessMustRemainSuccessful()
    {
        var restored = Serializer().Deserialize<Result<int>>(new BinaryData(Encoding.UTF8.GetBytes(LegacySuccess)));
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.ShouldBe(42);
    }

    [Test]
    [Arguments(0)]
    [Arguments(42)]
    public void NativeLegacyFailedProblemMustPreserveStatusAndExtensions(int value)
    {
        var problem = Problem.Create(FailureTitle, FailureDetail, (int)HttpStatusCode.Conflict);
        problem.Extensions[ExtensionKey] = ExtensionValue;
        var legacySerializer = new JsonGrainStorageSerializer(new OrleansJsonSerializer(
            Options.Create(new OrleansJsonSerializerOptions())));
        var expected = new ResultTSurrogateConverter<int>().ConvertFromSurrogate(new ResultTSurrogate<int>(false, value, problem));
        var data = legacySerializer.Serialize(expected);
        var restored = Serializer().Deserialize<Result<int>>(data);
        restored.IsFailed.ShouldBeTrue();
        restored.Value.ShouldBe(value);
        restored.Problem.Title.ShouldBe(FailureTitle);
        restored.Problem.Detail.ShouldBe(FailureDetail);
        restored.Problem.StatusCode.ShouldBe(problem.StatusCode);
        restored.Problem.Extensions[ExtensionKey]?.ToString().ShouldBe(ExtensionValue);
    }

    [Test]
    [Arguments(MissingFlag)]
    [Arguments(InvalidFlag)]
    [Arguments(ConflictingFlags)]
    public void InvalidStoredFlagsMustFailClosed(string content) =>
        Should.Throw<JsonSerializationException>(() => Serializer().Deserialize<Result<int>>(new BinaryData(Encoding.UTF8.GetBytes(content))));

    [Test]
    public void StandaloneRegistrationMustBeIdempotentAndPreserveNativeJson()
    {
        using var services = new ServiceCollection().AddCommunicationOrleansJsonStorage().AddCommunicationOrleansJsonStorage().BuildServiceProvider();
        var settings = services.GetRequiredService<IOptions<OrleansJsonSerializerOptions>>().Value;
        settings.JsonSerializerSettings.Converters.OfType<CommunicationResultJsonConverter>().Count().ShouldBe(1);
        var serializer = new JsonGrainStorageSerializer(new OrleansJsonSerializer(Options.Create(settings)));
        serializer.Deserialize<Result<int>>(serializer.Serialize(Result<int>.Succeed(42))).IsSuccess.ShouldBeTrue();
    }

    private const string NestedJson = "{\"value\":[17,{\"label\":\"雪\",\"decimal\":1.00}],\"enabled\":true}";
    private const string JsonString = "\"text\"";
    private const string JsonNumber = "1.00";
    private const string JsonNull = "null";
    private const string JsonBoolean = "true";
    private const string ElementKey = "element";
    private const string InvalidElementJson = "{\"json\":\"{\"}";
    private const string MissingElementJson = "{}";
    private const string NullElementJson = "{\"json\":null}";

    [Test]
    [Arguments(NestedJson)]
    [Arguments(JsonString)]
    [Arguments(JsonNumber)]
    [Arguments(JsonNull)]
    [Arguments(JsonBoolean)]
    public void JsonElementsMustRetainTypedAndObjectValuedStorageFingerprints(string json)
    {
        System.Text.Json.JsonElement expected;
        using (var document = JsonDocument.Parse(json))
        {
            expected = document.RootElement.Clone();
        }
        var payload = new JsonElementStoredState
        {
            Element = expected,
            Values = new Dictionary<string, object?> { [ElementKey] = expected },
            Update = Result<object>.Succeed(expected)
        };
        var serializer = Serializer();
        var restored = serializer.Deserialize<JsonElementStoredState>(serializer.Serialize(payload))!;
        var dictionaryValue = restored.Values[ElementKey].ShouldBeOfType<JsonElement>();
        var resultValue = restored.Update.Value.ShouldBeOfType<JsonElement>();
        restored.Update.IsSuccess.ShouldBeTrue();
        foreach (var actual in new[] { restored.Element, dictionaryValue, resultValue })
        {
            actual.ValueKind.ShouldBe(expected.ValueKind);
            Fingerprint(actual).ShouldBe(Fingerprint(expected));
            actual.GetRawText().ShouldBe(System.Text.Json.JsonSerializer.Serialize(expected));
        }
        var result = Result<JsonElementStoredState>.Succeed(payload);
        var restoredResult = serializer.Deserialize<Result<JsonElementStoredState>>(serializer.Serialize(result));
        restoredResult.IsSuccess.ShouldBeTrue();
        Fingerprint(restoredResult.Value.Element).ShouldBe(Fingerprint(expected));
    }

    [Test]
    [Arguments(InvalidElementJson)]
    [Arguments(MissingElementJson)]
    [Arguments(NullElementJson)]
    public void MalformedStoredJsonElementsMustFailClosed(string json) =>
        Should.Throw<JsonSerializationException>(() => Serializer().Deserialize<JsonElement>(new BinaryData(json)));

    [Test]
    public void JsonElementRegistrationMustPreserveConfiguredContractsAndRemainIdempotent()
    {
        var configured = new CamelCasePropertyNamesContractResolver();
        var services = new ServiceCollection();
        services.Configure<OrleansJsonSerializerOptions>(options => options.JsonSerializerSettings.ContractResolver = configured);
        using var provider = services.AddCommunicationOrleansJsonStorage().AddCommunicationOrleansJsonStorage().BuildServiceProvider();
        var settings = provider.GetRequiredService<IOptions<OrleansJsonSerializerOptions>>().Value;
        var resolver = settings.JsonSerializerSettings.ContractResolver.ShouldBeOfType<CommunicationJsonElementContractResolver>();
        resolver.ResolveContract(typeof(StoredState)).ShouldBeSameAs(configured.ResolveContract(typeof(StoredState)));
        settings.JsonSerializerSettings.Converters.OfType<CommunicationResultJsonConverter>().Count().ShouldBe(1);
    }

    private static string Fingerprint(JsonElement element) => Convert.ToHexString(
        SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(element)));

    [global::Orleans.GenerateSerializer]
    internal sealed class JsonElementStoredState
    {
        [global::Orleans.Id(0)] public JsonElement Element { get; init; }
        [global::Orleans.Id(1)] public Dictionary<string, object?> Values { get; init; } = new();
        [global::Orleans.Id(2)] public Result<object> Update { get; init; }
    }

    [global::Orleans.GenerateSerializer]
    internal sealed class StoredState
    {
        [global::Orleans.Id(0)]
        public Result<int> Update { get; init; }
    }
}

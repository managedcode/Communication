using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using N = ManagedCode.Communication.CommunicationJsonNames;
using ManagedCode.Communication.CQRS;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

/// <summary>
///     How the hand-written result converters behave under someone else's <see cref="JsonSerializerOptions" />.
/// </summary>
/// <remarks>
///     These converters are attached to the types themselves, so they apply to every application that serializes
///     a result — including ones configured very differently from this library's defaults. That reach is the
///     reason for this file: a converter that quietly ignored a naming policy, or that could not be replaced,
///     would be a problem everywhere at once rather than in one place.
/// </remarks>
public class ResultJsonInteropTests
{
    private sealed class Payload
    {
        public string? UserName { get; set; }
    }

    private static JsonSerializerOptions With(JsonNamingPolicy? policy)
    {
        return new JsonSerializerOptions { PropertyNamingPolicy = policy };
    }

    // ---------------------------------------------------------------------------------------------------
    // The documented wire format
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public void TheDefaultWireFormatIsExactlyAsDocumented()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        JsonSerializer.Serialize(Result.Succeed(), options)
            .ShouldBe($$"""{"{{N.IsSuccess}}":true}""");

        JsonSerializer.Serialize(Result<int>.Succeed(0), options)
            .ShouldBe($$"""{"{{N.IsSuccess}}":true,"{{N.Value}}":0}""");

        JsonSerializer.Serialize(Result.Fail(Problem.Create("boom", "detail", 409)), options)
            .ShouldContain($$"""{"{{N.IsSuccess}}":false,"{{N.Problem}}":{""");
    }

    [Fact]
    public void OptionsWithNoConfigurationAtAllStillProduceTheDocumentedNames()
    {
        // A plain `new JsonSerializerOptions()` leaves PropertyNamingPolicy null, which for ordinary types means
        // PascalCase. The result members stay camelCase, because that is the wire contract, not a policy default.
        JsonSerializer.Serialize(Result<int>.Succeed(7), new JsonSerializerOptions())
            .ShouldBe($$"""{"{{N.IsSuccess}}":true,"{{N.Value}}":7}""");
    }

    // ---------------------------------------------------------------------------------------------------
    // Naming policies
    // ---------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("camel")]
    [InlineData("snake")]
    [InlineData("kebab")]
    [InlineData("upper_snake")]
    public void TheEnvelopeIgnoresTheNamingPolicy(string? policyName)
    {
        // Every member of Result, Result<T> and CollectionResult<T> carries an explicit [JsonPropertyName],
        // which outranks a naming policy. So the envelope is a fixed wire contract: two services configured
        // differently still understand each other, and the payload alone follows the policy.
        var options = With(Policy(policyName));

        var json = JsonSerializer.Serialize(Result<Payload>.Succeed(new Payload { UserName = "bob" }), options);

        json.ShouldContain($"\"{N.IsSuccess}\":true");
        json.ShouldContain($"\"{N.Value}\":");
    }

    [Fact]
    public void ThePayloadStillFollowsThePolicy()
    {
        var json = JsonSerializer.Serialize(
            Result<Payload>.Succeed(new Payload { UserName = "bob" }), With(JsonNamingPolicy.SnakeCaseLower));

        // The envelope comes from the contract constants; "user_name" is the policy at work on the payload.
        json.ShouldBe($$$"""{"{{{N.IsSuccess}}}":true,"{{{N.Value}}}":{"user_name":"bob"}}""");
    }

    [Fact]
    public void TheEnvelopeMatchesCollectionResultWhichHasNoConverter()
    {
        // CollectionResult<T> goes through the reflection path. If the converters applied a policy and it could
        // not follow, the two would disagree on the wire under any non-camelCase configuration.
        var options = With(JsonNamingPolicy.SnakeCaseLower);

        JsonSerializer.Serialize(Result<int>.Succeed(1), options).ShouldContain($"\"{N.IsSuccess}\":");
        JsonSerializer.Serialize(CollectionResult<int>.Succeed([1], 1, 10, 1), options)
            .ShouldContain($"\"{N.IsSuccess}\":");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("camel")]
    [InlineData("snake")]
    [InlineData("kebab")]
    [InlineData("upper_snake")]
    public void EveryPolicyRoundTrips(string? policyName)
    {
        var options = With(Policy(policyName));

        var success = JsonSerializer.Deserialize<Result<Payload>>(
            JsonSerializer.Serialize(Result<Payload>.Succeed(new Payload { UserName = "bob" }), options), options);

        success.IsSuccess.ShouldBeTrue();
        success.Value!.UserName.ShouldBe("bob");

        var failure = JsonSerializer.Deserialize<Result<Payload>>(
            JsonSerializer.Serialize(Result<Payload>.Fail(Problem.Create("boom", "d", 409)), options), options);

        failure.IsFailed.ShouldBeTrue();
        failure.Problem!.StatusCode.ShouldBe(409);
    }

    [Fact]
    public void ADocumentIsReadableWhateverCasingItsProducerChose()
    {
        // Reading is deliberately more permissive than writing: a payload may come from an older version, a
        // differently configured service, or a client that chose its own casing.
        foreach (var json in new[] { """{"isSuccess":true,"value":7}""", """{"IsSuccess":true,"Value":7}""",
                                     """{"ISSUCCESS":true,"VALUE":7}""" })
        {
            JsonSerializer.Deserialize<Result<int>>(json, With(JsonNamingPolicy.SnakeCaseLower)).Value.ShouldBe(7);
            JsonSerializer.Deserialize<Result<int>>(json, With(null)).Value.ShouldBe(7);
        }
    }

    private static JsonNamingPolicy? Policy(string? name)
    {
        return name switch
        {
            "camel" => JsonNamingPolicy.CamelCase,
            "snake" => JsonNamingPolicy.SnakeCaseLower,
            "kebab" => JsonNamingPolicy.KebabCaseLower,
            "upper_snake" => JsonNamingPolicy.SnakeCaseUpper,
            _ => null
        };
    }

    // ---------------------------------------------------------------------------------------------------
    // Being overridable
    // ---------------------------------------------------------------------------------------------------

    private sealed class ShoutingResultConverter : JsonConverter<Result<int>>
    {
        public override Result<int> Read(ref Utf8JsonReader reader, System.Type t, JsonSerializerOptions o)
        {
            reader.Skip();
            return Result<int>.Succeed(42);
        }

        public override void Write(Utf8JsonWriter writer, Result<int> value, JsonSerializerOptions o)
        {
            writer.WriteStringValue("MINE");
        }
    }

    [Fact]
    public void AConverterRegisteredInTheOptionsReplacesOurs()
    {
        // System.Text.Json ranks Options.Converters above a [JsonConverter] attribute on the type, so an
        // application is never stuck with this library's format.
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ShoutingResultConverter());

        JsonSerializer.Serialize(Result<int>.Succeed(1), options).ShouldBe("\"MINE\"");
        JsonSerializer.Deserialize<Result<int>>("\"anything\"", options).Value.ShouldBe(42);

        // And only for the type it was registered for.
        JsonSerializer.Serialize(Result<string>.Succeed("x"), options)
            .ShouldBe($$"""{"{{N.IsSuccess}}":true,"{{N.Value}}":"x"}""");
    }

    [Fact]
    public void AConverterForThePayloadTypeIsHonoured()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PayloadConverter());

        JsonSerializer.Serialize(Result<Payload>.Succeed(new Payload { UserName = "bob" }), options)
            .ShouldBe($$"""{"{{N.IsSuccess}}":true,"{{N.Value}}":"bob"}""");

        JsonSerializer.Deserialize<Result<Payload>>($$"""{"{{N.IsSuccess}}":true,"{{N.Value}}":"bob"}""", options)
            .Value!.UserName.ShouldBe("bob");
    }

    private sealed class PayloadConverter : JsonConverter<Payload>
    {
        public override Payload Read(ref Utf8JsonReader reader, System.Type t, JsonSerializerOptions o)
        {
            return new Payload { UserName = reader.GetString() };
        }

        public override void Write(Utf8JsonWriter writer, Payload value, JsonSerializerOptions o)
        {
            writer.WriteStringValue(value.UserName);
        }
    }

    // ---------------------------------------------------------------------------------------------------
    // Other options that applications actually set
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public void IndentingAndRelaxedEscapingDoNotBreakTheConverters()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var json = JsonSerializer.Serialize(Result<string>.Succeed("naïve & bold"), options);

        json.ShouldContain(N.IsSuccess);
        JsonSerializer.Deserialize<Result<string>>(json, options).Value.ShouldBe("naïve & bold");
    }

    [Fact]
    public void IgnoringNullsDoesNotRemoveTheEnvelope()
    {
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        JsonSerializer.Serialize(Result.Succeed(), options).ShouldBe($$"""{"{{N.IsSuccess}}":true}""");
        JsonSerializer.Deserialize<Result<int>>(
            JsonSerializer.Serialize(Result<int>.Succeed(0), options), options).Value.ShouldBe(0);
    }

    [Fact]
    public void CaseInsensitivityBeingOffDoesNotBreakReading()
    {
        // The converters match member names themselves, so PropertyNameCaseInsensitive does not govern them.
        var strict = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };

        JsonSerializer.Deserialize<Result<int>>("""{"IsSuccess":true,"Value":7}""", strict).Value.ShouldBe(7);
    }

    [Fact]
    public void MembersTheConvertersDoNotKnowAreSkippedWhateverTheirShape()
    {
        var json = """
                   {"isSuccess":true,"value":7,"extra":{"nested":[1,2,{"deep":null}]},"trailing":"x"}
                   """;

        JsonSerializer.Deserialize<Result<int>>(json, new JsonSerializerOptions()).Value.ShouldBe(7);
    }

    // ---------------------------------------------------------------------------------------------------
    // The neighbouring types
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public void CollectionResultRoundTripsUnderANonDefaultPolicy()
    {
        // It has no converter of its own, so it goes through the reflection path — and still emits fixed member
        // names, because of the [JsonPropertyName] on each of them.
        var options = With(JsonNamingPolicy.SnakeCaseLower);
        var source = CollectionResult<int>.Succeed([1, 2, 3], 1, 10, 3);

        var json = JsonSerializer.Serialize(source, options);
        var read = JsonSerializer.Deserialize<CollectionResult<int>>(json, options);

        json.ShouldContain($"\"{N.PageNumber}\":");
        read.IsSuccess.ShouldBeTrue();
        read.Collection.ShouldBe([1, 2, 3]);
        read.TotalItems.ShouldBe(3);
    }

    [Fact]
    public void AStreamChunkRoundTripsUnderANonDefaultPolicy()
    {
        var options = With(JsonNamingPolicy.SnakeCaseLower);
        var chunk = CqrsStreamChunk<Payload, Payload>.Progress(new Payload { UserName = "bob" }, sequence: 3);

        var read = JsonSerializer.Deserialize<CqrsStreamChunk<Payload, Payload>>(
            JsonSerializer.Serialize(chunk, options), options);

        read.ShouldNotBeNull();
        read.Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        read.Sequence.ShouldBe(3);
        read.TryGetProgress(out var progress).ShouldBeTrue();
        progress.UserName.ShouldBe("bob");
    }

    [Fact]
    public void ResultsNestedInsideAnotherObjectAreUnaffectedByTheOuterShape()
    {
        var options = With(JsonNamingPolicy.SnakeCaseLower);
        var envelope = new Dictionary<string, Result<int>>
        {
            ["first"] = Result<int>.Succeed(1),
            ["second"] = Result<int>.Fail(Problem.Create("boom", "d", 500))
        };

        var read = JsonSerializer.Deserialize<Dictionary<string, Result<int>>>(
            JsonSerializer.Serialize(envelope, options), options);

        read!["first"].Value.ShouldBe(1);
        read["second"].Problem!.StatusCode.ShouldBe(500);
    }

    // ---------------------------------------------------------------------------------------------------
    // The wire format must not depend on how either end is configured
    // ---------------------------------------------------------------------------------------------------

    public static TheoryData<string> WireValues() => new()
    {
        "result", "resultT", "resultFailed", "collection", "problem", "chunk", "chunkFailed", "pagination"
    };

    private static object Sample(string key) => key switch
    {
        "result" => Result.Succeed(),
        "resultT" => Result<Payload>.Succeed(new Payload { UserName = "bob" }),
        "resultFailed" => Result<Payload>.Fail(Problem.Create("boom", "detail", 409)),
        "collection" => CollectionResult<int>.Succeed([1, 2, 3], 1, 10, 3),
        "problem" => Problem.Create("boom", "detail", 409),
        "chunk" => CqrsStreamChunk<Payload, Payload>.Progress(new Payload { UserName = "bob" }, sequence: 3),
        "chunkFailed" => CqrsStreamChunk<Payload, Payload>.Failed(Problem.Create("boom", "d", 500), "broke"),
        _ => new PaginationRequest(10, 25)
    };

    [Theory]
    [MemberData(nameof(WireValues))]
    public void TheEnvelopeMemberNamesAreIdenticalUnderEveryNamingPolicy(string key)
    {
        // A member without an explicit [JsonPropertyName] silently follows the policy. Two services configured
        // differently then exchange documents that parse but lose data — a chunk's `kind` read as `Kind` came
        // back as Started rather than Progress, which would make a terminal failure look like a live stream.
        //
        // Only the envelope is asserted: a caller's payload is expected to follow the policy, and does.
        var value = Sample(key);
        var baseline = TopLevelMembers(JsonSerializer.Serialize(value, With(JsonNamingPolicy.CamelCase)));

        foreach (var policyName in new string?[] { null, "camel", "snake", "kebab", "upper_snake" })
        {
            TopLevelMembers(JsonSerializer.Serialize(value, With(Policy(policyName))))
                .ShouldBe(baseline, $"the {policyName ?? "default"} policy renamed a member of {key}");
        }
    }

    private static IReadOnlyList<string> TopLevelMembers(string json)
    {
        using var document = JsonDocument.Parse(json);
        var names = new List<string>();
        foreach (var member in document.RootElement.EnumerateObject())
        {
            names.Add(member.Name);
        }

        return names;
    }

    [Fact]
    public void AChunkWrittenByOneServiceIsReadByAnotherWhateverItsPolicy()
    {
        var chunk = CqrsStreamChunk<Payload, Payload>.Failed(Problem.Create("boom", "d", 500), "broke");
        var wire = JsonSerializer.Serialize(chunk, With(JsonNamingPolicy.CamelCase));

        foreach (var policyName in new string?[] { null, "camel", "snake", "kebab", "upper_snake" })
        {
            var read = JsonSerializer.Deserialize<CqrsStreamChunk<Payload, Payload>>(wire, With(Policy(policyName)));

            read.ShouldNotBeNull();
            read.Kind.ShouldBe(CqrsStreamChunkKind.Failed);      // the member that used to be lost
            read.IsTerminal.ShouldBeTrue();
            read.Message.ShouldBe("broke");
            read.Problem!.StatusCode.ShouldBe(500);
        }
    }

    [Fact]
    public void PaginationCarriesOnlyWhatCanBeReadBack()
    {
        // PageNumber, PageSize, Offset, Limit and HasExplicitPageSize are computed from Skip and Take and have
        // no setter, so writing them was 60 bytes per request that no reader could ever use.
        var json = JsonSerializer.Serialize(new PaginationRequest(10, 25), With(JsonNamingPolicy.CamelCase));

        json.ShouldBe($$"""{"{{N.Skip}}":10,"{{N.Take}}":25}""");

        var read = JsonSerializer.Deserialize<PaginationRequest>(json, With(JsonNamingPolicy.CamelCase))!;
        read.Skip.ShouldBe(10);
        read.Take.ShouldBe(25);
        read.PageNumber.ShouldBe(1);
        read.HasExplicitPageSize.ShouldBeTrue();
    }
}

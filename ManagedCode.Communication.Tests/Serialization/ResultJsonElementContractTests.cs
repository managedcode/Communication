using System;
using System.Text.Json;
using Shouldly;

namespace ManagedCode.Communication.Tests.Serialization;

public sealed class ResultJsonElementContractTests
{
    private const string ObjectValue = "{\"status\":\"partial\",\"count\":0}";
    private const string ArrayValue = "[0,false,null]";
    private const string StringValue = "\"partial\"";
    private const string NumberValue = "0";
    private const string BooleanValue = "false";
    private const string NullValue = "null";

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void FailedJsonElement_WithoutPayload_PreservesAbsenceAndProblem(JsonContractMode mode)
    {
        var problem = SerializationContract.Failure();
        var actual = Absent(Result<JsonElement>.Fail(problem), mode);
        actual.Value.ValueKind.ShouldBe(JsonValueKind.Undefined);
        SerializationContract.AssertProblem(problem, actual.Problem, mode);
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void FailedNullableJsonElement_WithoutDefinedPayload_PreservesAbsence(JsonContractMode mode)
    {
        var problem = SerializationContract.Failure();
        var empty = Result<JsonElement?>.Fail(problem);
        foreach (var failure in new[] { empty, empty with { Value = default(JsonElement) } })
        {
            var actual = Absent(failure, mode);
            actual.Value.ShouldBeNull();
            SerializationContract.AssertProblem(problem, actual.Problem, mode);
        }
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void DefinedJsonElements_PreserveSuccessfulAndPartialFailureValues(JsonContractMode mode)
    {
        var problem = SerializationContract.Failure();
        foreach (var json in new[] { ObjectValue, ArrayValue, StringValue, NumberValue, BooleanValue, NullValue })
        {
            using var document = JsonDocument.Parse(json);
            var payload = document.RootElement.Clone();
            var success = SerializationContract.RoundTrip(Result<JsonElement>.Succeed(payload), mode);
            success.IsSuccess.ShouldBeTrue();
            JsonElement.DeepEquals(payload, success.Value).ShouldBeTrue();
            var partial = SerializationContract.RoundTrip(Result<JsonElement>.Fail(problem) with { Value = payload }, mode);
            partial.IsFailed.ShouldBeTrue();
            JsonElement.DeepEquals(payload, partial.Value).ShouldBeTrue();
            SerializationContract.AssertProblem(problem, partial.Problem, mode);
            var nullable = SerializationContract.RoundTrip(Result<JsonElement?>.Fail(problem) with { Value = payload }, mode);
            nullable.IsFailed.ShouldBeTrue();
            if (payload.ValueKind == JsonValueKind.Null)
            {
                nullable.Value.ShouldBeNull();
            }
            else
            {
                nullable.Value.HasValue.ShouldBeTrue();
                JsonElement.DeepEquals(payload, nullable.Value.Value).ShouldBeTrue();
            }
            SerializationContract.AssertProblem(problem, nullable.Problem, mode);
        }
    }

    [Test]
    [Arguments(JsonContractMode.Reflection)]
    [Arguments(JsonContractMode.Metadata)]
    [Arguments(JsonContractMode.GeneratedDefault)]
    public void SuccessfulUndefinedJsonElement_RejectsInvalidJsonPayload(JsonContractMode mode)
    {
        var result = Result<JsonElement>.Succeed(default(JsonElement));
        Should.Throw<InvalidOperationException>(() => JsonSerializer.Serialize(result, SerializationContract.Options(mode)));
    }

    private static Result<T> Absent<T>(Result<T> failure, JsonContractMode mode)
    {
        var options = SerializationContract.Options(mode);
        var json = JsonSerializer.Serialize(failure, options);
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty(CommunicationJsonNames.IsSuccess).GetBoolean().ShouldBeFalse();
        document.RootElement.TryGetProperty(CommunicationJsonNames.Value, out _).ShouldBeFalse();
        var actual = JsonSerializer.Deserialize<Result<T>>(json, options);
        actual.IsFailed.ShouldBeTrue();
        JsonSerializer.Serialize(actual, options).ShouldBe(json);
        return actual;
    }
}

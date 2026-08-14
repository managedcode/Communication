using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ManagedCode.Communication.Constants;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.ProblemTests;

/// <summary>
///     Regressions for defects found while auditing <see cref="Problem" />. Each test names the behaviour that
///     used to be wrong so a re-introduction is obvious from the failure message alone.
/// </summary>
public class ProblemRegressionTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    // ---------- validation errors survive a round trip ----------

    [Fact]
    public void AddValidationError_KeepsErrorsThatArrivedOverTheWire()
    {
        var original = new Problem { Title = "Validation", StatusCode = 400 };
        original.AddValidationError("email", "required");
        original.AddValidationError("name", "too short");

        var restored = JsonSerializer.Deserialize<Problem>(JsonSerializer.Serialize(original, Web), Web)!;

        restored.AddValidationError("phone", "invalid");

        // Previously the deserialized errors were a JsonElement, which AddValidationError replaced outright —
        // silently discarding every error that came with the response.
        var errors = restored.GetValidationErrors()!;
        errors.Keys.OrderBy(k => k).ShouldBe(["email", "name", "phone"]);
        errors["email"].ShouldBe(["required"]);
        errors["name"].ShouldBe(["too short"]);
        errors["phone"].ShouldBe(["invalid"]);
    }

    [Fact]
    public void GetValidationErrors_ReturnsTheSameInstanceOnRepeatedReads()
    {
        var original = new Problem();
        original.AddValidationError("email", "required");
        var restored = JsonSerializer.Deserialize<Problem>(JsonSerializer.Serialize(original, Web), Web)!;

        var first = restored.GetValidationErrors();
        var second = restored.GetValidationErrors();

        // Rebuilding the dictionary on every read made InvalidField/InvalidFieldError O(n) allocations each.
        first.ShouldNotBeNull();
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    public void InvalidFieldHelpers_WorkOnADeserializedProblem()
    {
        var original = new Problem();
        original.AddValidationError("email", "required");
        original.AddValidationError("email", "must be unique");

        var restored = JsonSerializer.Deserialize<Problem>(JsonSerializer.Serialize(original, Web), Web)!;

        restored.InvalidField("email").ShouldBeTrue();
        restored.InvalidField("missing").ShouldBeFalse();
        restored.InvalidFieldError("email").ShouldBe("required, must be unique");
    }

    // ---------- JSON converter interop ----------

    [Fact]
    public void Deserialize_AcceptsPascalCaseMembers()
    {
        // Producers using default System.Text.Json options, Newtonsoft, or a non-.NET stack send PascalCase.
        // Matching case-sensitively left every standard member unset and dumped them into Extensions.
        var problem = JsonSerializer.Deserialize<Problem>(
            """{"Type":"https://example/x","Title":"Boom","Status":418,"Detail":"d","Instance":"/i"}""", Web)!;

        problem.Type.ShouldBe("https://example/x");
        problem.Title.ShouldBe("Boom");
        problem.StatusCode.ShouldBe(418);
        problem.Detail.ShouldBe("d");
        problem.Instance.ShouldBe("/i");
        problem.Extensions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{"type":"x","status":null}""", 0)]
    [InlineData("""{"type":"x","status":"503"}""", 503)]
    [InlineData("""{"type":"x","status":404}""", 404)]
    public void Deserialize_ToleratesTheStatusShapesSeenInTheWild(string payload, int expected)
    {
        var problem = JsonSerializer.Deserialize<Problem>(payload, Web)!;

        problem.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public void Deserialize_AcceptsNullTitleAndDetail()
    {
        var problem = JsonSerializer.Deserialize<Problem>("""{"type":"x","title":null,"detail":null}""", Web)!;

        problem.Title.ShouldBeNull();
        problem.Detail.ShouldBeNull();
    }

    [Fact]
    public void Deserialize_NullPayloadBecomesNull()
    {
        JsonSerializer.Deserialize<Problem>("null", Web).ShouldBeNull();
    }

    [Fact]
    public void Serialize_DoesNotEmitADuplicateKeyWhenAnExtensionShadowsAStandardMember()
    {
        var problem = Problem.Create("real title", "d", 400);
        problem.Extensions["title"] = "shadow";
        problem.Extensions["Status"] = 999;

        var json = JsonSerializer.Serialize(problem, Web);

        // Duplicate JSON keys are ambiguous: parsers disagree on which wins, and some reject the document.
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("title").GetString().ShouldBe("real title");
        document.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        json.Split("\"title\"").Length.ShouldBe(2, "the title key must appear exactly once");
    }

    [Fact]
    public void Serialize_StillWritesGenuineExtensions()
    {
        var problem = Problem.Create("t", "d", 400);
        problem.ErrorCode = "E42";

        var json = JsonSerializer.Serialize(problem, Web);
        var restored = JsonSerializer.Deserialize<Problem>(json, Web)!;

        restored.ErrorCode.ShouldBe("E42");
    }

    // ---------- allocation ----------

    [Fact]
    public void Constructor_AllocatesASingleExtensionsDictionary()
    {
        // Warm up so JIT and type initialization are not counted.
        for (var i = 0; i < 100; i++)
        {
            _ = new Problem();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            _ = new Problem();
        }
        var perProblem = (GC.GetAllocatedBytesForCurrentThread() - before) / 1000.0;

        // The constructor used to assign Extensions on top of its property initializer, allocating and
        // immediately discarding a second dictionary for every Problem ever created.
        perProblem.ShouldBeLessThan(160);
    }

    [Fact]
    public void Extensions_IsUsableImmediatelyAfterConstruction()
    {
        var problem = new Problem();

        problem.Extensions.ShouldNotBeNull();
        problem.Extensions["k"] = "v";
        problem.Extensions["k"].ShouldBe("v");
    }

    [Fact]
    public void ValidationProblemType_MatchesTheInvalidCheck()
    {
        var problem = Problem.Validation(("field", "message"));

        problem.Type.ShouldBe(ProblemConstants.Types.ValidationFailed);
        Result.Fail(problem).IsInvalid.ShouldBeTrue();
    }
}

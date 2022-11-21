using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ManagedCode.Communication.Tests;

public class DeserializationTests
{
    [Theory]
    [MemberData(nameof(GetResults))]
    public void DeserializeResult_WithNewtonsoftJson(Result result)
    {
        // Act 
        var serialized = JsonConvert.SerializeObject(result);
        var deserialized = JsonConvert.DeserializeObject<Result>(serialized);

        // Assert
        deserialized.Should().BeEquivalentTo(result);
    }

    [Theory]
    [MemberData(nameof(GetValueResults))]
    public void DeserializeValueResult_WithNewtonsoftJson<T>(Result<T> result)
    {
        // Act 
        var serialized = JsonConvert.SerializeObject(result);
        var deserialized = JsonConvert.DeserializeObject<Result<T>>(serialized);

        // Assert
        deserialized.Should().BeEquivalentTo(result);
    }

    [Theory]
    [MemberData(nameof(GetResults))]
    public void DeserializeResult_WithTextJson(Result result)
    {
        // Act 
        var serialized = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<Result>(serialized);

        // Assert
        deserialized.Should().BeEquivalentTo(result);
    }

    [Theory]
    [MemberData(nameof(GetValueResults))]
    public void DeserializeValueResult_WithTextJson<T>(Result<T> result)
    {
        // Act 
        var serialized = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<Result<T>>(serialized);

        // Assert
        deserialized.Should().BeEquivalentTo(result);
    }

    public static IEnumerable<object[]> GetResults()
    {
        yield return new object[] { Result.Succeed() };
        yield return new object[] { Result.Fail() };
        yield return new object[] { Result.Fail(new Exception("Test exception")) };
        yield return new object[] { Result.Fail(new Error("Test error", HttpStatusCode.Found)) };
    }

    public static IEnumerable<object[]> GetValueResults()
    {
        yield return new object[] { Result<int>.Succeed(2) };
        yield return new object[] { Result<string>.Succeed("Test string") };
        yield return new object[] { Result<int>.Fail() };
        yield return new object[] { Result<int>.Fail(new Exception("Test exception")) };
        yield return new object[] { Result<int>.Fail(new Error("Test error", HttpStatusCode.Found)) };
    }
}
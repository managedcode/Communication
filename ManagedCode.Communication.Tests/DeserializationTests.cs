using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ManagedCode.Communication.Tests;

public class DeserializationTests
{
    [Theory]
    [MemberData(nameof(GetResults))]
    public void DeserializeResult_WithNewtonsoftJson(IResult result)
    {
        // Act 
        var serialized = JsonConvert.SerializeObject(result);
        var deserialized = JsonConvert.DeserializeObject<Result>(serialized);

        // Assert
        deserialized.Should().BeEquivalentTo(result);
    }

    [Theory]
    [MemberData(nameof(GetValueResults))]
    public void DeserializeValueResult_WithNewtonsoftJson<T>(IResult result)
    {
        // Act 
        var serialized = JsonConvert.SerializeObject(result);
        var deserialized = JsonConvert.DeserializeObject<Result<T>>(serialized);

        // Assert
        deserialized.Should().BeEquivalentTo(result);
    }

    [Theory]
    [MemberData(nameof(GetResults))]
    public void DeserializeResult_WithTextJson(IResult result)
    {
        // Act 
        var serialized = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<Result>(serialized);

        // Assert
        deserialized.Should().BeEquivalentTo(result);
    }

    [Theory]
    [MemberData(nameof(GetValueResults))]
    public void DeserializeValueResultT_WithTextJson<T>(IResult result)
    {
        // Act 
        var serialized = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<Result<T>>(serialized);

        // Assert
        deserialized.Should().BeEquivalentTo(result);
    }
    
    [Theory]
    [MemberData(nameof(GetValueResults))]
    public void DeserializeValueResult_WithTextJson<T>(IResult result)
    {
        // Act 
        var serialized = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<Result>(serialized, new JsonSerializerOptions());

        // Assert
        deserialized.IsSuccess.Should().Be(result.IsSuccess);
    }

    public static IEnumerable<object[]> GetResults()
    {
        yield return new object[] { Result.Succeed() };
        yield return new object[] { Result.Fail() };
        yield return new object[] { Result.Fail(new ValidationException("Test exception")) };
        yield return new object[] { Result.Fail(new Exception("Test exception")) };
        yield return new object[] { Result.Fail(Error.Create("Test error", HttpStatusCode.Found)) };
    }

    public static IEnumerable<object[]> GetValueResults()
    {
        yield return new object[] { Result<int>.Succeed(2) };
        yield return new object[] { Result<string>.Succeed("Test string") };
        yield return new object[] { Result<int>.Fail() };
        yield return new object[] { Result.Fail<int>(new ValidationException("Test exception")) };
        yield return new object[] { Result<int>.Fail(new Exception("Test exception")) };
        yield return new object[] { Result<int>.Fail(Error.Create("Test error", HttpStatusCode.Found)) };
    }
}
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests;

public class ResultInvalidTests
{
    [Fact]
    public void Invalid()
    {
        var invalid = Result.Invalid();
        invalid.IsSuccess.Should().BeFalse();
        invalid.IsInvalid.Should().BeTrue();
    }
    
    [Fact]
    public void InvalidMessage()
    {
        var invalid = Result.Invalid("message");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "message", "message" } });
    }
    
    [Fact]
    public void InvalidKeyValue()
    {
        var invalid = Result.Invalid("key", "value");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "key", "value" } });
    }
    
    [Fact]
    public void InvalidDictionary()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var invalid = Result.Invalid(dictionary);
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(dictionary);
    }
    
    [Fact]
    public void InvalidGenericMethod()
    {
        var invalid = Result.Invalid<MyResultObj>();
        invalid.IsSuccess.Should().BeFalse();
        invalid.IsInvalid.Should().BeTrue();
    }
    
    [Fact]
    public void InvalidGenericMethodMessage()
    {
        var invalid = Result.Invalid<MyResultObj>("message");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "message", "message" } });
    }
    
    [Fact]
    public void InvalidGenericMethodKeyValue()
    {
        var invalid = Result.Invalid<MyResultObj>("key", "value");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "key", "value" } });
    }
    
    [Fact]
    public void InvalidGenericMethodDictionary()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var invalid = Result.Invalid<MyResultObj>(dictionary);
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(dictionary);
    }
    
    [Fact]
    public void InvalidGeneric()
    {
        var invalid = Result<MyResultObj>.Invalid();
        invalid.IsSuccess.Should().BeFalse();
        invalid.IsInvalid.Should().BeTrue();
    }
    
    [Fact]
    public void InvalidGenericMessage()
    {
        var invalid = Result<MyResultObj>.Invalid("message");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "message", "message" } });
    }
    
    [Fact]
    public void InvalidGenericeyValue()
    {
        var invalid = Result<MyResultObj>.Invalid("key", "value");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "key", "value" } });
    }
    
    [Fact]
    public void InvalidGenericDictionary()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var invalid = Result<MyResultObj>.Invalid(dictionary);
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(dictionary);
    }
}
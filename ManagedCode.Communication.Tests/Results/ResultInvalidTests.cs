using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

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
    public void InvalidEnum()
    {
        var invalid = Result.Invalid(MyTestEnum.Option2);
        invalid.IsSuccess.Should().BeFalse();
        invalid.IsInvalid.Should().BeTrue();
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
    }

    [Fact]
    public void InvalidMessage()
    {
        var invalid = Result.Invalid("message");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "message", "message" } });
    }

    [Fact]
    public void InvalidMessageEnum()
    {
        var invalid = Result.Invalid(MyTestEnum.Option2,"message");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "message", "message" } });
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
    }
    
    
    [Fact]
    public void InvalidKeyValue()
    {
        var invalid = Result.Invalid("key", "value");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "key", "value" } });
    }
    
    [Fact]
    public void InvalidKeyValueEnum()
    {
        var invalid = Result.Invalid(MyTestEnum.Option2,"key", "value");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "key", "value" } });
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
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
    public void InvalidDictionaryEnum()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var invalid = Result.Invalid(MyTestEnum.Option2, dictionary);
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(dictionary);
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
    }

    [Fact]
    public void InvalidGenericMethod()
    {
        var invalid = Result.Invalid<MyResultObj>();
        invalid.IsSuccess.Should().BeFalse();
        invalid.IsInvalid.Should().BeTrue();
    }
    
    [Fact]
    public void InvalidGenericMethodEnum()
    {
        var invalid = Result.Invalid<MyResultObj,MyTestEnum>(MyTestEnum.Option2);
        invalid.IsSuccess.Should().BeFalse();
        invalid.IsInvalid.Should().BeTrue();
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
    }

    [Fact]
    public void InvalidGenericMethodMessage()
    {
        var invalid = Result.Invalid<MyResultObj>("message");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "message", "message" } });
    }
    
    [Fact]
    public void InvalidGenericMethodMessageEnum()
    {
        var invalid = Result.Invalid<MyResultObj, MyTestEnum>(MyTestEnum.Option2, "message");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "message", "message" } });
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
    }

    [Fact]
    public void InvalidGenericMethodKeyValue()
    {
        var invalid = Result.Invalid<MyResultObj>("key", "value");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "key", "value" } });
    }
    
    [Fact]
    public void InvalidGenericMethodKeyValueEnum()
    {
        var invalid = Result.Invalid<MyResultObj, MyTestEnum>(MyTestEnum.Option2,"key", "value");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "key", "value" } });
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
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
    public void InvalidGenericMethodDictionaryEnum()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var invalid = Result.Invalid<MyResultObj, MyTestEnum>(MyTestEnum.Option2,dictionary);
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(dictionary);
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
    }

    [Fact]
    public void InvalidGeneric()
    {
        var invalid = Result<MyResultObj>.Invalid();
        invalid.IsSuccess.Should().BeFalse();
        invalid.IsInvalid.Should().BeTrue();
    }
    
    
    [Fact]
    public void InvalidGenericEnum()
    {
        var invalid = Result<MyResultObj>.Invalid(MyTestEnum.Option2);
        invalid.IsSuccess.Should().BeFalse();
        invalid.IsInvalid.Should().BeTrue();
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
    }

    [Fact]
    public void InvalidGenericMessage()
    {
        var invalid = Result<MyResultObj>.Invalid("message");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "message", "message" } });
    }
    
    [Fact]
    public void InvalidGenericMessageEnum()
    {
        var invalid = Result<MyResultObj>.Invalid(MyTestEnum.Option2, "message");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "message", "message" } });
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
    }

    [Fact]
    public void InvalidGenericValue()
    {
        var invalid = Result<MyResultObj>.Invalid("key", "value");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "key", "value" } });
    }
    
    [Fact]
    public void InvalidGenericValueEnum()
    {
        var invalid = Result<MyResultObj>.Invalid(MyTestEnum.Option2, "key", "value");
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(new Dictionary<string, string> { { "key", "value" } });
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
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
    
    [Fact]
    public void InvalidGenericDictionaryEnum()
    {
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var invalid = Result<MyResultObj>.Invalid(MyTestEnum.Option2, dictionary);
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().BeEquivalentTo(dictionary);
        invalid.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);
    }
}
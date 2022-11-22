using System.Net;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests;

public enum MyTestEnum
{
    Option1,
    Option2
}

class MyResultObj
{
    public string Message;
    public int Number;
}

public class ResultTests
{
    [Fact]
    public void Succeed()
    {
        var ok = Result.Succeed();
        ok.IsSuccess.Should().BeTrue();
        ok.IsFail.Should().BeFalse();

        Assert.True(ok == true);
        Assert.True(ok);

    }
    
    
    [Fact]
    public void SucceedT()
    {
        var ok = Result<MyResultObj>.Succeed(new MyResultObj()
        {
            Message = "msg"
        });
        ok.IsSuccess.Should().BeTrue();
        ok.IsFail.Should().BeFalse();
        ok.Value.Message.Should().Be("msg");
        
        Assert.True(ok == true);
        Assert.True(ok);

    }
    
    [Fact]
    public void SucceedTEnum()
    {
        var ok = Result<MyResultObj>.Succeed(new MyResultObj()
        {
            Message = "msg"
        }, MyTestEnum.Option1);
        ok.IsSuccess.Should().BeTrue();
        ok.IsFail.Should().BeFalse();
        ok.Value.Message.Should().Be("msg");

        Assert.True(ok == true);
        Assert.True(ok);
    }
    
    [Fact]
    public void Fail()
    {
        var ok = Result.Fail();
        ok.IsSuccess.Should().BeFalse();
        ok.IsFail.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);

    }
    
    [Fact]
    public void FailEnum()
    {
        var ok = Result.Fail(HttpStatusCode.Unauthorized);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFail.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }
    
    [Fact]
    public void FailT()
    {
        var ok = Result<MyResultObj>.Fail();
        ok.IsSuccess.Should().BeFalse();
        ok.IsFail.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);

    }
    
    [Fact]
    public void FailTEnum()
    {
        var ok = Result<MyResultObj>.Fail(HttpStatusCode.Unauthorized);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFail.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }
}


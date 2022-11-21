using System.Net;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests;

public enum MyTestEnum
{
    Option1,
    Option2
}

public class ResultTests
{
    [Fact]
    public void Succeed()
    {
        var ok = Result.Succeed();
        ok.IsSuccess.Should().BeTrue();
        ok.IsFail.Should().BeFalse();
        ok.ResultCode<HttpStatusCode>().Should().Be(HttpStatusCode.OK);
        
        Assert.True(ok == true);
        Assert.True(ok);

    }
    
    [Fact]
    public void SucceedEnum()
    {
        var ok = Result.Succeed(MyTestEnum.Option1);
        ok.IsSuccess.Should().BeTrue();
        ok.IsFail.Should().BeFalse();
        ok.ResultCode<MyTestEnum>().Should().Be(MyTestEnum.Option1);

        Assert.True(ok == true);
        Assert.True(ok);
    }
    
    [Fact]
    public void Fail()
    {
        var ok = Result.Fail();
        ok.IsSuccess.Should().BeFalse();
        ok.IsFail.Should().BeTrue();
        ok.ResultCode<HttpStatusCode>().Should().Be(HttpStatusCode.InternalServerError);
        
        Assert.True(ok == false);
        Assert.False(ok);

    }
    
    [Fact]
    public void FailEnum()
    {
        var ok = Result.Fail(HttpStatusCode.Unauthorized);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFail.Should().BeTrue();
        ok.ResultCode<HttpStatusCode>().Should().Be(HttpStatusCode.Unauthorized);

        Assert.True(ok == false);
        Assert.False(ok);
    }
}


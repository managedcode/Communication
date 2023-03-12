using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests;

public enum MyTestEnum
{
    Option1,
    Option2
}

internal class MyResultObj
{
    public string Message;
    public int Number;
}

public class ResultSucceedTests
{
    [Fact]
    public void Succeed()
    {
        var ok = Result.Succeed();
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.GetError().Should().BeNull();
        ok.ThrowIfFail();
        Assert.True(ok == true);
        Assert.True(ok);
        ok.AsTask().Result.IsSuccess.Should().BeTrue();
        ok.AsValueTask().Result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SucceedT()
    {
        var ok = Result<MyResultObj>.Succeed(new MyResultObj
        {
            Message = "msg"
        });
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.GetError().Should().BeNull();
        ok.ThrowIfFail();
        ok.Value.Message.Should().Be("msg");

        Assert.True(ok == true);
        Assert.True(ok);
        
        ok.AsTask().Result.IsSuccess.Should().BeTrue();
        ok.AsValueTask().Result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SucceedGeneric()
    {
        var ok = Result.Succeed(new MyResultObj
        {
            Message = "msg"
        });
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.Value.Message.Should().Be("msg");

        Assert.True(ok == true);
        Assert.True(ok);
    }

    [Fact]
    public void SucceedGenericAction()
    {
        var ok = Result.Succeed<MyResultObj>(a => a.Message = "msg");
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.Value.Message.Should().Be("msg");

        Assert.True(ok == true);
        Assert.True(ok);
    }

    [Fact]
    public void SucceedTAction()
    {
        var ok = Result<MyResultObj>.Succeed(a => a.Message = "msg");
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.Value.Message.Should().Be("msg");

        Assert.True(ok == true);
        Assert.True(ok);
    }

    [Fact]
    public void SucceedFrom()
    {
        var ok = Result<MyResultObj>.From(() => new MyResultObj
        {
            Message = "msg"
        });
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.Value.Message.Should().Be("msg");

        Assert.True(ok == true);
        Assert.True(ok);
    }

    [Fact]
    public async Task SucceedFromTask()
    {
        var ok = await Result<MyResultObj>.From(() => Task.Run(() => new MyResultObj
        {
            Message = "msg"
        }));
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.Value.Message.Should().Be("msg");

        Assert.True(ok == true);
        Assert.True(ok);
    }

    [Fact]
    public void SucceedTFrom()
    {
        var ok = Result<MyResultObj>.From(() => new MyResultObj
        {
            Message = "msg"
        });
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.Value.Message.Should().Be("msg");

        Assert.True(ok == true);
        Assert.True(ok);
    }
    
    [Fact]
    public void SucceedTFromResult()
    {
        var ok = Result<MyResultObj>.From(() => new MyResultObj
        {
            Message = "msg"
        });


        Result result = Result.From(ok);
        
        Assert.True(ok == true);
        Assert.True(result);
    }
    
    [Fact]
    public void SucceedFromResult()
    {
        var ok = Result<MyResultObj>.From(() => new MyResultObj
        {
            Message = "msg"
        });
        
        Result<MyResultObj> result = Result<MyResultObj>.From(ok);
        
        Assert.True(ok == true);
        Assert.True(result);
    }
    
    [Fact]
    public void SucceedResultFromResult()
    {
        var obj = new MyResultObj
        {
            Message = "msg"
        };
        var ok = Result<MyResultObj>.From(() => obj);
        
        Result result1 = Result.From(Result.Succeed());
        Result<MyResultObj> result2 = Result<MyResultObj>.From(Result<MyResultObj>.From(Result<MyResultObj>.Succeed(obj)));

        result2.Value.Message.Should().Be(obj.Message);
        
        Assert.True(ok == true);
        Assert.True(result1);
        Assert.True(result2);
    }
}
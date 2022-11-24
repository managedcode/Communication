using System.Net;
using System.Threading.Tasks;
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

public class ResultSucceedTests
{
    [Fact]
    public void Succeed()
    {
        var ok = Result.Succeed();
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();

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
        ok.IsFailed.Should().BeFalse();
        ok.Value.Message.Should().Be("msg");
        
        Assert.True(ok == true);
        Assert.True(ok);

    }
    
    [Fact]
    public void SucceedFrom()
    {
        var ok = Result<MyResultObj>.From(() => new MyResultObj()
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
        var ok = await Result<MyResultObj>.From(() => Task.Run(() => new MyResultObj()
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
        var ok = Result<MyResultObj>.From(()=>new MyResultObj()
        {
            Message = "msg"
        });
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.Value.Message.Should().Be("msg");
        
        Assert.True(ok == true);
        Assert.True(ok);

    }
    
    
}


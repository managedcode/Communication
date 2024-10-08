using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

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
        ok.ThrowIfFailWithStackPreserved();
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
        ok.ThrowIfFailWithStackPreserved();
        ok.Value.Message.Should().Be("msg");

        Assert.True(ok == true);
        Assert.True(ok);

        ok.AsTask().Result.IsSuccess.Should().BeTrue();
        ok.AsValueTask().Result.IsSuccess.Should().BeTrue();
    }
    
    [Fact]
    public void SucceedTNullCondition()
    {
        var ok = Result<MyResultObj>.Succeed(new MyResultObj
        {
            Message = "msg"
        });

        if (ok.IsSuccess)
        {
            ok.Value.Message.Should().Be("msg");
        }
    }

    [Fact]
    public void SucceedTEnum()
    {
        var ok = Result<MyTestEnum>.Succeed(MyTestEnum.Option1);
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.GetError().Should().BeNull();
        ok.ThrowIfFail();
        ok.ThrowIfFailWithStackPreserved();

        Assert.True(ok == true);
        Assert.True(ok);

        ok.Value.Should().Be(MyTestEnum.Option1);

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
    public void SucceedTFromButNull()
    {
        var ok = Result<MyResultObj>.From(()=> null as  MyResultObj);
        ok.IsSuccess.Should().BeTrue();
        ok.IsEmpty.Should().BeTrue();

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


        var result = Result.From(ok);

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
        
        var result = Result<MyResultObj>.From(ok);

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

        var result1 = Result.From(Result.Succeed());
        var result2 = Result<MyResultObj>.From(Result<MyResultObj>.From(Result<MyResultObj>.Succeed(obj)));

        result2.Value.Message.Should().Be(obj.Message);

        Assert.True(ok == true);
        Assert.True(result1);
        Assert.True(result2);
    }
}
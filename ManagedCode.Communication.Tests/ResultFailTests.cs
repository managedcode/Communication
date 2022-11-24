using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests;

public class ResultFailTests
{
    [Fact]
    public void Fail()
    {
        var ok = Result.Fail();
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailEnum()
    {
        var ok = Result.Fail(HttpStatusCode.Unauthorized);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailString()
    {
        var ok = Result.Fail("Oops");
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailStringEnum()
    {
        var ok = Result.Fail("Oops", MyTestEnum.Option1);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        ok.GetError().Value.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option1);

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailEnumString()
    {
        var ok = Result.Fail(MyTestEnum.Option2, "Oops");
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        ok.GetError().Value.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailThrow()
    {
        var ok = Result.Fail("Oops");
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(Exception), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailExceptionThrow()
    {
        var ok = Result.Fail(new ArithmeticException());
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArithmeticException), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailFrom()
    {
        var ok = Result.From(new Action(() => throw new ArgumentException()));
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArgumentException), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    // T

    [Fact]
    public void FailT()
    {
        var ok = Result<MyResultObj>.Fail();
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailTEnum()
    {
        var ok = Result<MyResultObj>.Fail(HttpStatusCode.Unauthorized);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailTString()
    {
        var ok = Result<MyResultObj>.Fail("Oops");
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailTStringEnum()
    {
        var ok = Result<MyResultObj>.Fail("Oops", MyTestEnum.Option1);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        ok.GetError().Value.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option1);

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailTEnumString()
    {
        var ok = Result<MyResultObj>.Fail(MyTestEnum.Option2, "Oops");
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        ok.GetError().Value.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailTThrow()
    {
        var ok = Result<MyResultObj>.Fail("Oops");
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(Exception), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailTExceptionThrow()
    {
        var ok = Result<MyResultObj>.Fail(new ArithmeticException());
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArithmeticException), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void SucceedTFrom()
    {
        var ok = Result<MyResultObj>.From(() =>
        {
            throw new ArgumentException();
            return new MyResultObj();
        });
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArgumentException), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public async Task SucceedFromTask()
    {
        var ok = await Result<MyResultObj>.From(() => Task.Run(() =>
        {
            throw new ArgumentException();
            return new MyResultObj();
        }));
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArgumentException), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }
    
    // T Generic

    [Fact]
    public void FailGeneric()
    {
        var ok = Result.Fail<MyResultObj>();
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailGenericEnum()
    {
        var ok = Result.Fail<MyResultObj,HttpStatusCode>(HttpStatusCode.Unauthorized);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailGenericString()
    {
        var ok = Result.Fail<MyResultObj>("Oops");
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailGenericStringEnum()
    {
        var ok = Result.Fail<MyResultObj,MyTestEnum>("Oops", MyTestEnum.Option1);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        ok.GetError().Value.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option1);

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailGenericEnumString()
    {
        var ok = Result.Fail<MyResultObj,MyTestEnum>(MyTestEnum.Option2, "Oops");
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        ok.GetError().Value.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option2);

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailGenericThrow()
    {
        var ok = Result.Fail<MyResultObj>("Oops");
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(Exception), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailGenericExceptionThrow()
    {
        var ok = Result.Fail<MyResultObj>(new ArithmeticException());
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArithmeticException), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void SucceedGenericFrom()
    {
        var ok = Result.From<MyResultObj>(() =>
        {
            throw new ArgumentException();
            return new MyResultObj();
        });
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArgumentException), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public async Task SucceedFromGenericTask()
    {
        var ok = await Result.From<MyResultObj>(() => Task.Run(() =>
        {
            throw new ArgumentException();
            return new MyResultObj();
        }));
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArgumentException), () => ok.ThrowExceptionIfFailed());

        Assert.True(ok == false);
        Assert.False(ok);
    }
}
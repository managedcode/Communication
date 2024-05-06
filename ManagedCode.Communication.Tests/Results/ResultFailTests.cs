using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ResultFailTests
{
    [Fact]
    public void Fail()
    {
        var fail = Result.Fail();
        fail.IsSuccess.Should().BeFalse();
        fail.IsFailed.Should().BeTrue();
        Assert.Throws<Exception>(() => fail.ThrowIfFail());
        Assert.True(fail == false);
        Assert.False(fail);
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
        var fail = Result.Fail("Oops");
        fail.IsSuccess.Should().BeFalse();
        fail.IsFailed.Should().BeTrue();
        Assert.True(fail == false);
        Assert.False(fail);
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

        Assert.Throws(typeof(Exception), () => ok.ThrowIfFail());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailExceptionThrow()
    {
        var ok = Result.Fail(new ArithmeticException());
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArithmeticException), () => ok.ThrowIfFail());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailFrom()
    {
        var ok = Result.From(new Action(() => throw new ArgumentException()));
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArgumentException), () => ok.ThrowIfFail());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    // T

    [Fact]
    public void FailT()
    {
        var fail = Result<MyResultObj>.Fail();
        fail.IsSuccess.Should().BeFalse();
        fail.IsFailed.Should().BeTrue();
        Assert.Throws<Exception>(() => fail.ThrowIfFail());
        Assert.True(fail == false);
        Assert.False(fail);
    }

    [Fact]
    public void FailTEnum()
    {
        var ok = Result<MyResultObj>.Fail(HttpStatusCode.Unauthorized);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();
        ok.IsEmpty.Should().BeTrue();

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

        Assert.Throws(typeof(Exception), () => ok.ThrowIfFail());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailTExceptionThrow()
    {
        var ok = Result<MyResultObj>.Fail(new ArithmeticException());
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArithmeticException), () => ok.ThrowIfFail());

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

        Assert.Throws(typeof(ArgumentException), () => ok.ThrowIfFail());

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

        Assert.Throws(typeof(ArgumentException), () => ok.ThrowIfFail());

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
        var ok = Result.Fail<MyResultObj, HttpStatusCode>(HttpStatusCode.Unauthorized);
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
        var ok = Result.Fail<MyResultObj, MyTestEnum>("Oops", MyTestEnum.Option1);
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        ok.GetError().Value.ErrorCodeAs<MyTestEnum>().Should().Be(MyTestEnum.Option1);

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailGenericEnumString()
    {
        var ok = Result.Fail<MyResultObj, MyTestEnum>(MyTestEnum.Option2, "Oops");
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

        Assert.Throws(typeof(Exception), () => ok.ThrowIfFail());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void FailGenericExceptionThrow()
    {
        var ok = Result.Fail<MyResultObj>(new ArithmeticException());
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArithmeticException), () => ok.ThrowIfFail());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public void SucceedGenericFrom()
    {
        var ok = Result.From(() =>
        {
            throw new ArgumentException();
            return new MyResultObj();
        });
        ok.IsSuccess.Should().BeFalse();
        ok.IsFailed.Should().BeTrue();

        Assert.Throws(typeof(ArgumentException), () => ok.ThrowIfFail());

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

        Assert.Throws(typeof(ArgumentException), () => ok.ThrowIfFail());

        Assert.True(ok == false);
        Assert.False(ok);
    }

    [Fact]
    public async Task SeveralErrors()
    {
        var result = Result.Fail();
        result.AddError(Error.Create("oops1"));
        result.AddError(Error.Create("oops2"));
        result.AddError(Error.Create("oops3"));

        try
        {
            result.ThrowIfFail();
        }
        catch (AggregateException e)
        {
            e.Message.Should().Be("One or more errors occurred. (oops1) (oops2) (oops3)");
            e.InnerExceptions.Count.Should().Be(3);
        }
    }

    [Fact]
    public void ExceptionParse()
    {
        var exception = Error.FromException(new Exception("oops"));
        var argumentException = Error.FromException(new ArgumentException("oops"));

        exception.Exception().Message.Should().Be("oops");
        argumentException.Exception().Message.Should().Be("oops");

        exception.Exception().GetType().Should().Be(typeof(Exception));
        argumentException.Exception().GetType().Should().Be(typeof(ArgumentException));

        exception.Exception<ArgumentException>().Should().BeNull();
        argumentException.Exception<Exception>().Should().NotBeNull();
    }
}
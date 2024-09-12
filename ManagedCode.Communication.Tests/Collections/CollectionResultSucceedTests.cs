using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests.Collections;

public class CollectionResultSucceedTests
{
    [Fact]
    public void Succeed()
    {
        var ok = CollectionResult<int>.Succeed(Enumerable.Repeat(4, 100));
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.GetError().Should().BeNull();
        ok.ThrowIfFail();
        ok.ThrowIfFailWithStackPreserved();
        Assert.True(ok == true);
        Assert.True(ok);
        ok.AsTask().Result.IsSuccess.Should().BeTrue();
        ok.AsValueTask().Result.IsSuccess.Should().BeTrue();

        ok.Collection.Length.Should().Be(100);

        ok.PageNumber.Should().Be(1);
        ok.TotalItems.Should().Be(100);
        ok.TotalPages.Should().Be(1);
        ok.PageSize.Should().Be(100);
    }

    [Fact]
    public void SucceedPaged()
    {
        var ok = CollectionResult<int>.Succeed(Enumerable.Repeat(4, 100), 5, 100, 15000);
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailed.Should().BeFalse();
        ok.GetError().Should().BeNull();
        ok.ThrowIfFail();
        ok.ThrowIfFailWithStackPreserved();
        Assert.True(ok == true);
        Assert.True(ok);
        ok.AsTask().Result.IsSuccess.Should().BeTrue();
        ok.AsValueTask().Result.IsSuccess.Should().BeTrue();

        ok.Collection.Length.Should().Be(100);

        ok.PageNumber.Should().Be(5);
        ok.TotalItems.Should().Be(15000);
        ok.TotalPages.Should().Be(15000 / 100);
        ok.PageSize.Should().Be(100);
    }
    
    [Fact]
    public void FailWithoutError()
    {
        var fail = CollectionResult<int>.Fail();
        fail.IsSuccess.Should().BeFalse();
        fail.IsFailed.Should().BeTrue();
        fail.GetError().Should().BeNull();
        Assert.Throws<Exception>(() => fail.ThrowIfFail());
        Assert.Throws<Exception>(() => fail.ThrowIfFailWithStackPreserved());
        Assert.True(fail == false);
        Assert.False(fail);
        fail.AsTask().Result.IsSuccess.Should().BeFalse();
        fail.AsValueTask().Result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void FailWithError()
    {
        var fail = CollectionResult<int>.Fail("Test Error");
        fail.IsSuccess.Should().BeFalse();
        fail.IsFailed.Should().BeTrue();
        fail.GetError().Should().NotBeNull();
        fail.GetError()?.Message.Should().Be("Test Error");
        Assert.Throws<Exception>(() => fail.ThrowIfFail());
        Assert.Throws<Exception>(() => fail.ThrowIfFailWithStackPreserved());
        Assert.True(fail == false);
        Assert.False(fail);
        fail.AsTask().Result.IsSuccess.Should().BeFalse();
        fail.AsValueTask().Result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void InvalidWithoutMessage()
    {
        var invalid = CollectionResult<int>.Invalid();
        invalid.IsSuccess.Should().BeFalse();
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().NotBeNull();
        invalid.InvalidObject?.ContainsKey("message").Should().BeTrue();
        invalid.InvalidObject?["message"].Should().Be(nameof(CollectionResult<int>.Invalid));
    }

    [Fact]
    public void InvalidWithMessage()
    {
        var invalid = CollectionResult<int>.Invalid("Test Invalid");
        invalid.IsSuccess.Should().BeFalse();
        invalid.IsInvalid.Should().BeTrue();
        invalid.InvalidObject.Should().NotBeNull();
        invalid.InvalidObject?.ContainsKey("message").Should().BeTrue();
        invalid.InvalidObject?["message"].Should().Be("Test Invalid");
    }
    
    [Fact]
    public void SucceedWithEmptyCollection()
    {
        var ok = CollectionResult<int>.Succeed(new int[0]);
        ok.IsSuccess.Should().BeTrue();
        ok.Collection.Length.Should().Be(0);
    }

    [Fact]
    public void FailWithException()
    {
        var fail = CollectionResult<int>.Fail(new Exception("Test Exception"));
        fail.IsSuccess.Should().BeFalse();
        fail.GetError()?.Message.Should().Be("Test Exception");
    }

    [Fact]
    public void InvalidWithKeyValue()
    {
        var invalid = CollectionResult<int>.Invalid("TestKey", "TestValue");
        invalid.IsSuccess.Should().BeFalse();
        invalid.InvalidObject?.ContainsKey("TestKey").Should().BeTrue();
        invalid.InvalidObject?["TestKey"].Should().Be("TestValue");
    }

    [Fact]
    public async Task FromTaskWithException()
    {
        var task = Task.FromException<IEnumerable<int>>(new Exception("Test Exception"));
        var result = await CollectionResult<int>.From(task);
        result.IsSuccess.Should().BeFalse();
        result.GetError()?.Message.Should().Be("Test Exception");
    }

    [Fact]
    public async Task FromValueTaskWithException()
    {
        var valueTask = new ValueTask<IEnumerable<int>>(Task.FromException<IEnumerable<int>>(new Exception("Test Exception")));
        var result = await CollectionResult<int>.From(valueTask);
        result.IsSuccess.Should().BeFalse();
        result.GetError()?.Message.Should().Be("Test Exception");
    }
}
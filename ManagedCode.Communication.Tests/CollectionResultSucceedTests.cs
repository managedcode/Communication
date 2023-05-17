using System.Linq;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests;

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
}
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests;

public class ResultTests
{
    [Fact]
    public void Succeed()
    {
        var ok = Result.Succeed();
        ok.IsSuccess.Should().BeTrue();
        ok.IsFail.Should().BeFalse();
        ok.ResultCode.Should().Be(ResultCodes.Ok);
        
        Assert.True(ok == true);
        Assert.True(ok);

    }
    
    [Fact]
    public void SucceedEnum()
    {
        var ok = Result.Succeed(ResultCodes.NoResult);
        ok.IsSuccess.Should().BeTrue();
        ok.IsFail.Should().BeFalse();
        ok.ResultCode.Should().Be(ResultCodes.NoResult);

        Assert.True(ok == true);
        Assert.True(ok);
    }
}


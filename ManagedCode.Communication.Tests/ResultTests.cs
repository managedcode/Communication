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
        
        Assert.True(ok == true);
        Assert.True(ok);

    }
    
    [Fact]
    public void SucceedEnum()
    {
        var ok = Result.Succeed(ErrorCodes.AlreadyExists);
        ok.IsSuccess.Should().BeTrue();
        ok.IsFail.Should().BeFalse();

        Assert.True(ok == true);
        Assert.True(ok);
    }
}


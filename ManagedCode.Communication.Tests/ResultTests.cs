using System;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ManagedCode.Communication.Tests;

public class ResultTests
{
    [Fact]
    public void Compare()
    {
        var ok = Result.Succeed();
        var error = Result.Fail();

        Assert.True(ok == true);
        Assert.False(error == false);
        

    }

   
}
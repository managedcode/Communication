using System;
using System.Collections.Generic;
using System.Reflection;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AspNetResult = Microsoft.AspNetCore.Http.IResult;
using AspNetActionResult = Microsoft.AspNetCore.Mvc.IActionResult;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.CQRS;

public class CqrsStreamResultFactoryTests
{
    [Fact]
    public void TryCreateServerSentEventsResult_WithNullResultReturnsFalse()
    {
        InvokeTryCreateServerSentEventsResult(null, out var converted).ShouldBeFalse();
        converted.ShouldBeNull();
    }

    [Fact]
    public void TryCreateServerSentEventsResult_WithExistingAspNetResultReturnsFalse()
    {
        AspNetResult? expected = TypedResults.Ok("already-converted");

        InvokeTryCreateServerSentEventsResult(expected, out var converted).ShouldBeFalse();
        converted.ShouldBeSameAs(expected);
    }

    [Fact]
    public void TryCreateServerSentEventsResult_WithNonChunkAsyncEnumerableReturnsFalse()
    {
        InvokeTryCreateServerSentEventsResult(RunNonChunkAsync(), out var converted).ShouldBeFalse();
        converted.ShouldBeNull();
    }

    [Fact]
    public void TryCreateServerSentEventsResult_WithChunkStreamReturnsConvertedResult()
    {
        InvokeTryCreateServerSentEventsResult(RunChunkStream(), out var converted).ShouldBeTrue();
        converted.ShouldNotBeNull();
    }

    [Fact]
    public void TryCreateServerSentEventsActionResult_WithNullResultReturnsFalse()
    {
        InvokeTryCreateServerSentEventsActionResult(null, out var converted).ShouldBeFalse();
        converted.ShouldBeNull();
    }

    [Fact]
    public void TryCreateServerSentEventsActionResult_WithChunkStreamReturnsActionResult()
    {
        InvokeTryCreateServerSentEventsActionResult(RunChunkStream(), out var converted).ShouldBeTrue();
        converted.ShouldNotBeNull();
        converted!.GetType().Name.ShouldBe("CqrsServerSentEventsActionResult");
    }

    private static bool InvokeTryCreateServerSentEventsResult(object? value, out AspNetResult? result)
    {
        var factoryType = GetFactoryType();
        var method = factoryType.GetMethod(
            "TryCreateServerSentEventsResult",
            BindingFlags.Public | BindingFlags.Static);
        var args = new object?[2] { value, null };

        var converted = (bool)method!.Invoke(null, args)!;
        result = args[1] as AspNetResult;
        return converted;
    }

    private static bool InvokeTryCreateServerSentEventsActionResult(object? value, out AspNetActionResult? result)
    {
        var factoryType = GetFactoryType();
        var method = factoryType.GetMethod(
            "TryCreateServerSentEventsActionResult",
            BindingFlags.Public | BindingFlags.Static);
        var args = new object?[2] { value, null };

        var converted = (bool)method!.Invoke(null, args)!;
        result = args[1] as AspNetActionResult;
        return converted;
    }

    private static Type GetFactoryType()
    {
        var communicationCqrsAspNetCoreAssembly = typeof(ManagedCode.Communication.CQRS.AspNetCore.Filters.CqrsResultActionFilter).Assembly;

        return communicationCqrsAspNetCoreAssembly.GetType(
            "ManagedCode.Communication.CQRS.AspNetCore.CqrsStreamResultFactory")!;
    }

    private static async IAsyncEnumerable<int> RunNonChunkAsync()
    {
        yield return 1;
        await Task.Delay(1);
        yield return 2;
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<string, string>> RunChunkStream()
    {
        yield return CqrsStreamChunk<string, string>.Started(
            Result<string>.Succeed("started"),
            sequence: 1);

        await Task.Delay(1);

        yield return CqrsStreamChunk<string, string>.Completed(
            Result<string>.Succeed("done"),
            sequence: 2);
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.AspNetCore.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.CQRS;

public class CqrsResultActionFilterTests
{
    [Fact]
    public async Task OnResultExecutionAsync_WithNullContext_Throws()
    {
        var filter = new CqrsResultActionFilter();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await filter.OnResultExecutionAsync(null!, () => Task.FromResult<ResultExecutedContext>(null!)));
    }

    [Fact]
    public async Task OnResultExecutionAsync_WithNullNext_Throws()
    {
        var actionContext = CreateActionContext();
        var context = new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new ObjectResult("value"),
            new object());

        var filter = new CqrsResultActionFilter();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await filter.OnResultExecutionAsync(context, null!));
    }

    [Fact]
    public async Task OnResultExecutionAsync_ConvertsChunkResultToActionResult()
    {
        var actionContext = CreateActionContext();
        var stream = TestChunkStream(default);
        var sourceResult = new ObjectResult(stream);
        var context = new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            sourceResult,
            new object());

        var filter = new CqrsResultActionFilter();
        await filter.OnResultExecutionAsync(context, () => Task.FromResult<ResultExecutedContext>(null!));

        context.Result.ShouldNotBeOfType<ObjectResult>();
        context.Result!.GetType().Name.ShouldBe("CqrsServerSentEventsActionResult");
    }

    [Fact]
    public async Task OnResultExecutionAsync_LeavesNonChunkResultUnchanged()
    {
        var actionContext = CreateActionContext();
        var sourceResult = new ObjectResult("plain");
        var context = new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            sourceResult,
            new object());

        var filter = new CqrsResultActionFilter();
        await filter.OnResultExecutionAsync(context, () => Task.FromResult<ResultExecutedContext>(null!));

        context.Result.ShouldBe(sourceResult);
    }

    private static ActionContext CreateActionContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        return new ActionContext(
            new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider()
            },
            new RouteData(),
            new ActionDescriptor());
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<int, int>> TestChunkStream(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return CqrsStreamChunk<int, int>.Started(Result<int>.Succeed(1), sequence: 1);
        await Task.Delay(1, cancellationToken);
        yield return CqrsStreamChunk<int, int>.Completed(
            Result<int>.Succeed(2),
            sequence: 2);
    }
}

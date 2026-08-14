using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Shouldly;
using Xunit;
using ResultFilter = ManagedCode.Communication.AspNetCore.Filters.ResultToActionResultFilter;

namespace ManagedCode.Communication.Tests.AspNetCore.Filters;

/// <summary>
///     The MVC filter that aligns the HTTP status code with a returned <see cref="Result" />. It is registered
///     globally by <c>AddCommunicationFilters()</c>, so it runs on every action result in the application.
/// </summary>
public class ResultToActionResultFilterTests
{
    [Fact]
    public async Task FailedResult_SetsTheStatusCodeFromTheProblem()
    {
        var context = CreateContext(new ObjectResult(Result.Fail(Problem.Create("nope", "d", 409))));

        await new ResultFilter().OnResultExecutionAsync(context, Next);

        ((ObjectResult)context.Result).StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task FailedResultOfT_SetsTheStatusCodeFromTheProblem()
    {
        var context = CreateContext(new ObjectResult(Result<int>.Fail(Problem.Create("nope", "d", 422))));

        await new ResultFilter().OnResultExecutionAsync(context, Next);

        ((ObjectResult)context.Result).StatusCode.ShouldBe(422);
    }

    [Fact]
    public async Task SuccessfulResultWithoutAStatusCode_DefaultsTo200()
    {
        var context = CreateContext(new ObjectResult(Result.Succeed()));

        await new ResultFilter().OnResultExecutionAsync(context, Next);

        ((ObjectResult)context.Result).StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Theory]
    [InlineData(StatusCodes.Status201Created)]
    [InlineData(StatusCodes.Status202Accepted)]
    [InlineData(StatusCodes.Status204NoContent)]
    public async Task SuccessfulResult_KeepsAStatusCodeTheActionChose(int statusCode)
    {
        // The filter used to overwrite this with 200, silently turning a deliberate 201/202/204 into an OK.
        var context = CreateContext(new ObjectResult(Result<string>.Succeed("v")) { StatusCode = statusCode });

        await new ResultFilter().OnResultExecutionAsync(context, Next);

        ((ObjectResult)context.Result).StatusCode.ShouldBe(statusCode);
    }

    [Fact]
    public async Task NonResultValues_AreLeftAlone()
    {
        var objectResult = new ObjectResult(new { Name = "plain" }) { StatusCode = StatusCodes.Status418ImATeapot };
        var context = CreateContext(objectResult);

        await new ResultFilter().OnResultExecutionAsync(context, Next);

        ((ObjectResult)context.Result).StatusCode.ShouldBe(StatusCodes.Status418ImATeapot);
    }

    [Fact]
    public async Task NonObjectResults_AreLeftAlone()
    {
        var original = new NoContentResult();
        var context = CreateContext(original);

        await new ResultFilter().OnResultExecutionAsync(context, Next);

        context.Result.ShouldBeSameAs(original);
    }

    [Fact]
    public async Task TheRestOfThePipelineStillRuns()
    {
        var context = CreateContext(new ObjectResult(Result.Succeed()));
        var ranNext = false;

        await new ResultFilter().OnResultExecutionAsync(context, () =>
        {
            ranNext = true;
            return Task.FromResult<ResultExecutedContext>(null!);
        });

        ranNext.ShouldBeTrue();
    }

    private static ResultExecutingContext CreateContext(IActionResult result)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), result, new object());
    }

    private static Task<ResultExecutedContext> Next()
    {
        return Task.FromResult<ResultExecutedContext>(null!);
    }
}

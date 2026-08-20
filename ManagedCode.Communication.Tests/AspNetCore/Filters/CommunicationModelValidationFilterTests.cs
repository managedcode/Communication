using System.Collections.Generic;
using ManagedCode.Communication;
using ManagedCode.Communication.AspNetCore.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ManagedCode.Communication.Tests.AspNetCore.Filters;

public class CommunicationModelValidationFilterTests
{
    [Test]
    public void OnActionExecuting_WithInvalidModelState_ReturnsBadRequestResult()
    {
        var actionDescriptor = new ActionDescriptor
        {
            DisplayName = "Create"
        };
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            actionDescriptor);
        actionContext.ModelState.AddModelError("name", "name is required");

        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());

        var filter = new CommunicationModelValidationFilter(NullLogger<CommunicationModelValidationFilter>.Instance);
        filter.OnActionExecuting(context);

        var result = context.Result.ShouldBeOfType<BadRequestObjectResult>();
        var failed = result.Value.ShouldBeOfType<Result>();
        failed.IsFailed.ShouldBeTrue();
        failed.Problem.ShouldNotBeNull();
        failed.Problem!.StatusCode.ShouldBe(400);
    }

    [Test]
    public void OnActionExecuting_WithValidModelState_LeavesResultNull()
    {
        var actionDescriptor = new ActionDescriptor
        {
            DisplayName = "Create"
        };
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            actionDescriptor);
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());

        var filter = new CommunicationModelValidationFilter(NullLogger<CommunicationModelValidationFilter>.Instance);
        filter.OnActionExecuting(context);

        context.Result.ShouldBeNull();
    }

    [Test]
    public void OnActionExecuted_DoesNothing()
    {
        var actionDescriptor = new ActionDescriptor
        {
            DisplayName = "Create"
        };
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            actionDescriptor);
        var context = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            new object());

        var filter = new CommunicationModelValidationFilter(NullLogger<CommunicationModelValidationFilter>.Instance);
        filter.OnActionExecuted(context);

        context.Exception.ShouldBeNull();
        context.Result.ShouldBeNull();
    }
}

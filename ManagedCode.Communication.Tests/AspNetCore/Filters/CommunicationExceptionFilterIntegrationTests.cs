using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Filters;

public class CommunicationExceptionFilterIntegrationTests
{
    [Fact]
    public async Task CommunicationExceptionFilter_ConvertsControllerExceptionsToFailedResult()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/communication-exceptions/throw");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        result.Problem!.Title.ShouldBe("ArgumentException");
        result.Problem!.Detail.ShouldBe("boom");
    }

    [Fact]
    public void UseCommunication_ReturnsSameBuilder()
    {
        var app = WebApplication.CreateSlimBuilder().Build();
        var returned = app.UseCommunication();

        returned.ShouldBeSameAs(app);
    }

    [Fact]
    public void UseCommunication_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() => CommunicationAppBuilderExtensions.UseCommunication(null!));
    }

    [Fact]
    public void CommunicationExceptionFilter_ExceptionsInHandler_ReturnsFallbackResult()
    {
        var filter = new CommunicationExceptionFilter(NullLogger<CommunicationExceptionFilter>.Instance);
        var actionDescriptor = new ActionDescriptor { RouteValues = null! };
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            actionDescriptor);
        var context = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new InvalidOperationException("boom")
        };

        filter.OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        var result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(500);
        var failure = result.Value.ShouldBeOfType<Result>();
        failure.IsFailed.ShouldBeTrue();
        failure.Problem.ShouldNotBeNull();
        failure.Problem!.StatusCode.ShouldBe(500);
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddControllers(options =>
            {
                options.AddCommunicationFilters();
            })
            .AddApplicationPart(typeof(CommunicationExceptionFilterController).Assembly);

        var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }
}

[ApiController]
[Route("api/communication-exceptions")]
public sealed class CommunicationExceptionFilterController : ControllerBase
{
    [HttpGet("throw")]
    public IActionResult Throw()
    {
        throw new ArgumentException("boom");
    }
}

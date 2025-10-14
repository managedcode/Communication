using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Extensions.MinimalApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using HttpResults = Microsoft.AspNetCore.Http.Results;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class ResultEndpointFilterTests
{
    [Fact]
    public async Task WithCommunicationResults_SuccessResult_ReturnsOkResponse()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/success", () => Result<string>.Succeed("pong")).WithCommunicationResults();
        });

        var response = await app.GetTestClient().GetAsync("/success");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<string>();
        payload.ShouldBe("pong");
    }

    [Fact]
    public async Task WithCommunicationResults_FailedResult_ReturnsProblem()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/failed", () => Result.Fail()).WithCommunicationResults();
        });

        var response = await app.GetTestClient().GetAsync("/failed");
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        var problem = await response.Content.ReadFromJsonAsync<Problem>();
        problem.ShouldNotBeNull();
        problem!.StatusCode.ShouldBe(500);
        problem.Title.ShouldBe("Operation failed");
    }

    [Fact]
    public async Task WithCommunicationResults_GroupBuilder_AppliesFilterToAllEndpoints()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            var group = app.MapGroup("/api").WithCommunicationResults();
            group.MapGet("/value", () => Result<int>.Succeed(42));
            group.MapGet("/error", () => Result<int>.Fail(Problem.Create("Not Found", "missing", 404)));
        });

        var client = app.GetTestClient();

        var success = await client.GetAsync("/api/value");
        success.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await success.Content.ReadFromJsonAsync<int>()).ShouldBe(42);

        var failure = await client.GetAsync("/api/error");
        failure.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var error = await failure.Content.ReadFromJsonAsync<Problem>();
        error.ShouldNotBeNull();
        error!.StatusCode.ShouldBe(404);
        error.Title.ShouldBe("Not Found");
    }

    [Fact]
    public async Task WithCommunicationResults_PassesThroughExistingIResult()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/native", () => HttpResults.Created("/resource", new { Value = 1 }))
                .WithCommunicationResults();
        });

        var response = await app.GetTestClient().GetAsync("/native");
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static async Task<WebApplication> CreateAppAsync(Action<WebApplication> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        configure(app);
        await app.StartAsync();
        return app;
    }
}

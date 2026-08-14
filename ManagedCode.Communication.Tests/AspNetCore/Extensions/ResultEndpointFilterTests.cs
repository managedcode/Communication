using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.AspNetCore.MinimalApi;
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

    [Fact]
    public async Task WithCommunicationResults_GenericFailedResultIsNormalizedToDefaultProblem()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/generic-failed", static () => Result<int>.Fail())
                .WithCommunicationResults();
        });

        var response = await app.GetTestClient().GetAsync("/generic-failed");
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        var problem = await response.Content.ReadFromJsonAsync<Problem>();
        problem.ShouldNotBeNull();
        problem.StatusCode.ShouldBe(500);
        problem.Title.ShouldBe("Operation failed");
        problem.Detail.ShouldBe("Unknown error occurred");
    }

    [Fact]
    public async Task WithCommunicationResults_NullResultReturnsNull()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/null", () => (object?)null)
                .WithCommunicationResults();
        });

        var response = await app.GetTestClient().GetAsync("/null");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("null");
    }

    [Fact]
    public async Task WithCommunicationResults_PlainObjectPassesThroughFilter()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/plain", static () => new { status = "ok" })
                .WithCommunicationResults();
        });

        var response = await app.GetTestClient().GetAsync("/plain");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<PlainPayload>();
        payload.ShouldNotBeNull();
        payload.Status.ShouldBe("ok");
    }

    [Fact]
    public async Task WithCommunicationResults_CustomIResultWithoutValueProperty_ReturnsNoContent()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/custom-iresult-novalue", static () => new NoValueCustomResult(true))
                .WithCommunicationResults();
        });

        var response = await app.GetTestClient().GetAsync("/custom-iresult-novalue");
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).ShouldBe("");
    }

    [Fact]
    public async Task WithCommunicationResults_CustomIResultWithoutValuePropertyFailure_ConvertsToProblem()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/custom-iresult-novalue-failed", static () =>
                new NoValueCustomResult(false, Problem.Create("boom", "custom iresult failed")))
                .WithCommunicationResults();
        });

        var response = await app.GetTestClient().GetAsync("/custom-iresult-novalue-failed");
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        var problem = await response.Content.ReadFromJsonAsync<Problem>();
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe("boom");
        problem.Detail.ShouldBe("custom iresult failed");
    }

    public sealed record PlainPayload(string Status);

    private sealed class NoValueCustomResult : IResult
    {
        public NoValueCustomResult(bool isSuccess, Problem? problem = null)
        {
            IsSuccess = isSuccess;
            Problem = problem;
        }

        public bool IsSuccess { get; }
        public bool IsFailed => !IsSuccess;
        public bool HasProblem => Problem is not null;
        public Dictionary<string, List<string>> InvalidObject => Problem?.GetValidationErrors() ?? new();
        public Problem? Problem { get; }
        public bool IsInvalid => Problem?.Type == "https://tools.ietf.org/html/rfc7231#section-6.5.1";

        public string InvalidFieldError(string fieldName)
        {
            return IsSuccess
                ? string.Empty
                : Problem?.InvalidFieldError(fieldName) ?? string.Empty;
        }

        public bool InvalidField(string fieldName)
        {
            return !IsSuccess && Problem?.InvalidField(fieldName) == true;
        }

        public bool ThrowIfFail()
        {
            if (Problem is not null)
            {
                throw Problem;
            }

            return false;
        }

        public bool TryGetProblem([MaybeNullWhen(false)] out Problem problem)
        {
            if (Problem is null)
            {
                problem = null;
                return false;
            }

            problem = Problem;
            return true;
        }

        [Obsolete("Use Problem.AddValidationError instead")]
        public void AddInvalidMessage(string message)
        {
            Problem?.AddValidationError(message);
        }

        [Obsolete("Use Problem.AddValidationError instead")]
        public void AddInvalidMessage(string key, string value)
        {
            Problem?.AddValidationError(key, value);
        }
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

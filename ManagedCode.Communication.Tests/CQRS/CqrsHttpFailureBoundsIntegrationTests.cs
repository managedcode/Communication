using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

public class CqrsHttpFailureBoundsIntegrationTests
{
    private const string RawFailureSecret = "private response body https://host.example/secret?token=credential";
    private const string ProblemTitle = "service_unavailable";
    private const string ProblemDetail = "dependency failed";

    [Test]
    public async Task BoundedProblemJson_PreservesProblemFieldsAtTheInclusiveBodyLimit()
    {
        var expected = Problem.Create(ProblemTitle, ProblemDetail, (int)HttpStatusCode.ServiceUnavailable);
        var body = JsonSerializer.SerializeToUtf8Bytes(expected, CqrsStreamSerialization.Default);
        await using var app = await CqrsHttpBoundsTestSupport.StartAsync(context => WriteFailureAsync(
            context,
            HttpStatusCode.ServiceUnavailable,
            body,
            contentLength: body.Length));
        using var client = app.GetTestClient();

        var chunks = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.StreamPath,
            new CqrsStreamClientOptions { MaximumFailureBodyBytes = body.Length });

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.StatusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
        chunks[0].Problem!.Title.ShouldBe(ProblemTitle);
        chunks[0].Problem!.Detail.ShouldBe(ProblemDetail);
    }

    [Test]
    public async Task PlainTextAndInvalidUtf8FailureBodies_UseStatusOnlySafeDetails()
    {
        var responses = new[]
        {
            Encoding.UTF8.GetBytes(RawFailureSecret),
            CqrsHttpBoundsTestSupport.InvalidJson(),
            new byte[] { 0xFF, 0xFE, 0xFD },
            Array.Empty<byte>()
        };

        foreach (var body in responses)
        {
            await using var app = await CqrsHttpBoundsTestSupport.StartAsync(context => WriteFailureAsync(
                context,
                HttpStatusCode.BadGateway,
                body,
                contentLength: body.Length));
            using var client = app.GetTestClient();

            var chunks = await CqrsHttpBoundsTestSupport.ReadAsync(
                client,
                CqrsHttpBoundsTestSupport.StreamPath,
                new CqrsStreamClientOptions { MaximumFailureBodyBytes = 1024 });

            chunks.Count.ShouldBe(1);
            chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
            chunks[0].Problem!.StatusCode.ShouldBe((int)HttpStatusCode.BadGateway);
            chunks[0].Problem!.Detail.ShouldBe(CqrsStreamProblems.NonSuccessDetail);
            JsonSerializer.Serialize(chunks[0], CqrsStreamSerialization.Default).ShouldNotContain(RawFailureSecret);
        }
    }

    [Test]
    public async Task KnownExcessiveLength_IsRejectedWithoutDrainingAndNextRequestSucceeds()
    {
        var responseAborted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var app = await CqrsHttpBoundsTestSupport.StartAsync(async context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
            context.Response.ContentLength = 64;
            context.Response.ContentType = CqrsHttpBoundsTestSupport.PlainTextContentType;
            await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
            await CqrsHttpBoundsTestSupport.WaitForAbortAsync(context, responseAborted).ConfigureAwait(false);
        }, addHealthyRoute: true);
        using var client = app.GetTestClient();

        var chunks = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.StreamPath,
            new CqrsStreamClientOptions { MaximumFailureBodyBytes = 8 });

        chunks.Count.ShouldBe(1);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.FailureBodyLimitExceeded);
        chunks[0].Problem!.Detail.ShouldBe(CqrsStreamProblems.FailureBodyLimitDetail);
        (await responseAborted.Task.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds)))
            .ShouldBeTrue();

        var healthy = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.HealthyPath,
            CqrsStreamClientOptions.Default);
        healthy[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    [Test]
    public async Task UnknownLengthExcessiveBody_IsReadOnlyThroughTheDetectionByte()
    {
        var responseAborted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secretBody = Encoding.UTF8.GetBytes(RawFailureSecret);
        await using var app = await CqrsHttpBoundsTestSupport.StartAsync(async context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
            context.Response.ContentType = CqrsHttpBoundsTestSupport.PlainTextContentType;
            context.Response.ContentLength = null;
            await context.Response.Body.WriteAsync(secretBody, context.RequestAborted).ConfigureAwait(false);
            await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
            await CqrsHttpBoundsTestSupport.WaitForAbortAsync(context, responseAborted).ConfigureAwait(false);
        });
        using var client = app.GetTestClient();

        var chunks = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.StreamPath,
            new CqrsStreamClientOptions { MaximumFailureBodyBytes = 8 });

        chunks.Count.ShouldBe(1);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.FailureBodyLimitExceeded);
        chunks[0].Problem!.Detail.ShouldBe(CqrsStreamProblems.FailureBodyLimitDetail);
        chunks[0].Problem!.StatusCode.ShouldBe((int)HttpStatusCode.BadGateway);
        JsonSerializer.Serialize(chunks[0], CqrsStreamSerialization.Default).ShouldNotContain(RawFailureSecret);
        (await responseAborted.Task.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds)))
            .ShouldBeTrue();
    }

    private static async Task WriteFailureAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        byte[] body,
        long? contentLength)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = CqrsHttpBoundsTestSupport.PlainTextContentType;
        context.Response.ContentLength = contentLength;
        await context.Response.Body.WriteAsync(body, context.RequestAborted).ConfigureAwait(false);
    }
}

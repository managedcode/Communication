using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Extensions.Http;
using Polly;
using Polly.Retry;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

public class ResultHttpClientExtensionsTests
{
    [Fact]
    public async Task SendForResultAsync_WithSuccessResponse_ReturnsSuccessResult()
    {
        using var client = new HttpClient(new StubHandler(static (_, _) =>
        {
            var payload = JsonSerializer.Serialize(Result<string>.Succeed("pong"));
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }));

        var result = await client.SendForResultAsync<string>(
            static () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("pong");
    }

    [Fact]
    public async Task SendForResultAsync_WithFailureResponse_ReturnsFailedResult()
    {
        using var client = new HttpClient(new StubHandler(static (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("invalid request", Encoding.UTF8, "text/plain")
            };
            return Task.FromResult(response);
        }));

        var result = await client.SendForResultAsync<string>(
            static () => new HttpRequestMessage(HttpMethod.Post, "https://example.com"));

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendForResultAsync_WithRetryPipeline_RetriesUntilSuccess()
    {
        var attempt = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            attempt++;

            if (attempt == 1)
            {
                var failure = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("down", Encoding.UTF8, "text/plain")
                };
                return Task.FromResult(failure);
            }

            var payload = JsonSerializer.Serialize(Result<int>.Succeed(42));
            var success = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(success);
        }));

        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(static response => !response.IsSuccessStatusCode)
            })
            .Build();

        var result = await client.SendForResultAsync<int>(
            static () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"),
            pipeline);

        attempt.ShouldBe(2);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}

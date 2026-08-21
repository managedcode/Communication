using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Extensions.Http;
using Shouldly;

namespace ManagedCode.Communication.Tests.Extensions;

public class ResultHttpClientExtensionsTests
{
    [Test]
    public async Task SendForResultAsync_NullClient_ThrowsArgumentNullException()
    {
        HttpClient client = null!;
        var act = async () => await client.SendForResultAsync<string>(static () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"));

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Test]
    public async Task SendForResultAsync_NullRequestFactory_ThrowsArgumentNullException()
    {
        using var client = new HttpClient();
        Func<HttpRequestMessage> requestFactory = null!;
        var act = async () => await client.SendForResultAsync<string>(requestFactory);

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Test]
    public async Task SendForResultAsync_WithCommandExecution_SucceedsAfterRetry()
    {
        var attempt = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            attempt++;

            if (attempt == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("cold", Encoding.UTF8, "text/plain")
                });
            }

            var payload = JsonSerializer.Serialize(21);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        }));

        var result = await client.SendForResultAsync<int, Command>(
            Command.Create("http.get"),
            static _ => new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/retry"),
            CreateRetryRuntime());

        attempt.ShouldBe(2);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(21);
    }

    [Test]
    public async Task GetAsResultAsync_NullClient_ThrowsArgumentNullException()
    {
        HttpClient client = null!;

        var act = async () => await client.GetAsResultAsync<int>("https://example.com");

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Test]
    public async Task GetAsResultAsync_NullOrEmptyUri_ThrowsArgumentException()
    {
        using var client = new HttpClient();

        var act = async () => await client.GetAsResultAsync<int>(string.Empty);

        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Test]
    public async Task GetAsResultAsync_WithoutPayload_ReturnsSuccessResult()
    {
        using var client = new HttpClient(new StubHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)
            {
                Content = new StringContent(string.Empty, Encoding.UTF8, "text/plain")
            })));

        var result = await client.GetAsResultAsync("https://example.com/api/ping");

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task SendForResultAsync_WithSuccessResponse_ReturnsSuccessResult()
    {
        using var client = new HttpClient(new StubHandler(static (_, _) =>
        {
            var payload = JsonSerializer.Serialize("pong");
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

    [Test]
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

    [Test]
    public async Task SendForResultAsync_WithSuccessProjection_ReturnsProjectedValue()
    {
        using var client = new HttpClient(new StubHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("payload", Encoding.UTF8, "text/plain")
            })));

        var result = await client.SendForResultAsync(
            static () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"),
            static async (response, cancellationToken) =>
                await response.Content.ReadAsStringAsync(cancellationToken));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("payload");
    }

    [Test]
    public async Task SendForResultAsync_WithSuccessProjectionAndProblem_DoesNotInvokeProjection()
    {
        var projectionInvoked = false;
        using var client = new HttpClient(new StubHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(
                    "{\"title\":\"Conflict\",\"status\":409,\"detail\":\"stale\",\"errorCode\":\"stale_revision\"}",
                    Encoding.UTF8,
                    "application/problem+json")
            })));

        var result = await client.SendForResultAsync<string>(
            static () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"),
            (_, _) =>
            {
                projectionInvoked = true;
                return Task.FromResult<string?>("unexpected");
            });

        result.IsFailed.ShouldBeTrue();
        result.Problem!.ErrorCode.ShouldBe("stale_revision");
        projectionInvoked.ShouldBeFalse();
    }

    [Test]
    public async Task SendForResultAsync_WithNullSuccessProjection_ReturnsInvalidResponseProblem()
    {
        using var client = new HttpClient(new StubHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

        var result = await client.SendForResultAsync<string>(
            static () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"),
            static (_, _) => Task.FromResult<string?>(null));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe("Invalid response body");
    }

    [Test]
    public async Task SendForResultAsync_WithTransportFailure_ReturnsServiceUnavailableResult()
    {
        using var client = new HttpClient(new StubHandler(static (_, _) =>
            throw new HttpRequestException("network unavailable")));

        var result = await client.SendForResultAsync<string>(
            static () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"));

        result.IsFailed.ShouldBeTrue();
        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
        result.Problem.Detail.ShouldBe("network unavailable");
    }

    [Test]
    public async Task SendForResultAsync_WithCallerCancellation_PropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        using var client = new HttpClient(new StubHandler(static (_, cancellationToken) =>
            Task.FromCanceled<HttpResponseMessage>(cancellationToken)));

        var act = async () => await client.SendForResultAsync<string>(
            static () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"),
            cancellationToken: cancellationSource.Token);

        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Test]
    public async Task SendForResultAsync_WithNativeRetry_RetriesUntilSuccess()
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

            var payload = JsonSerializer.Serialize(42);
            var success = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(success);
        }));

        var result = await client.SendForResultAsync<int, Command>(
            Command.Create("http.get"),
            static _ => new HttpRequestMessage(HttpMethod.Get, "https://example.com"),
            CreateRetryRuntime());

        attempt.ShouldBe(2);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Test]
    public async Task SendForResultAsync_WithRetryAfterHeader_UsesAuthoritativeServerDelay()
    {
        var attempt = 0;
        var observedDelay = TimeSpan.Zero;
        var retryAfter = TimeSpan.FromMilliseconds(15);
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            if (Interlocked.Increment(ref attempt) == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent(
                        CommandExecutionTestConstants.BusyProblemJson,
                        Encoding.UTF8,
                        CommandExecutionTestConstants.ProblemJsonMediaType)
                };
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(retryAfter);
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    CommandExecutionTestConstants.JsonValue,
                    Encoding.UTF8,
                    CommandExecutionTestConstants.JsonMediaType)
            });
        }));
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        options.Retry.Enabled = true;
        options.Retry.MaxRetries = 1;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.UseJitter = false;
        options.Retry.OnRetry = (retryEvent, _) =>
        {
            observedDelay = retryEvent.Delay;
            return ValueTask.CompletedTask;
        };

        var result = await client.SendForResultAsync<int, Command>(
            Command.Create(CommandExecutionTestConstants.HttpRetryAfter),
            static _ => new HttpRequestMessage(HttpMethod.Get, CommandExecutionTestConstants.ExampleUrl),
            new CommandExecutionRuntime(options));

        result.Value.ShouldBe(42);
        observedDelay.ShouldBe(retryAfter);
    }

    private static CommandExecutionRuntime CreateRetryRuntime()
    {
        var options = new CommandExecutionOptions();
        options.Retry.Enabled = true;
        options.Retry.MaxRetries = 2;
        options.Retry.Delay = TimeSpan.Zero;
        options.Retry.UseJitter = false;
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        return new CommandExecutionRuntime(options);
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

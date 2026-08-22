using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ManagedCode.Communication.Tests.Extensions;

public class HttpClientBuilderExtensionsTests
{
    private const string ResilientClientName = "resilient-client";
    private const string RemovedClientName = "removed-client";
    private const string ExampleRequestUrl = "https://example.com/api/items";
    private const string OtherRequestUrl = "https://other.example.com/api/items";
    private const string SuccessPayload = "ok";
    private const string TransportFailureMessage = "transport failed";

    [Test]
    public async Task AddCommunicationResilienceHandler_RetriesSafeRequestWithFreshMessagesAsync()
    {
        var handler = new SequenceHandler(static (attempt, _) => Task.FromResult(
            attempt == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SuccessPayload)
                }));
        await using var services = CreateProvider(ResilientClientName, handler, ConfigureSingleRetry);
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(ResilientClientName);

        using var response = await client.GetAsync(ExampleRequestUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe(SuccessPayload);
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].ShouldNotBeSameAs(handler.Requests[1]);
    }

    [Test]
    public async Task AddCommunicationResilienceHandler_DoesNotRetryUnsafeRequestAsync()
    {
        var handler = new SequenceHandler(static (_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        await using var services = CreateProvider(ResilientClientName, handler, ConfigureSingleRetry);
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(ResilientClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, ExampleRequestUrl);

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        handler.Requests.Count.ShouldBe(1);
    }

    [Test]
    public async Task AddCommunicationResilienceHandler_DoesNotRetryRequestContentAsync()
    {
        var handler = new SequenceHandler(static (attempt, _) => Task.FromResult(
            new HttpResponseMessage(attempt == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)));
        await using var services = CreateProvider(ResilientClientName, handler, ConfigureSingleRetry);
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(ResilientClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, ExampleRequestUrl)
        {
            Content = new StringContent(SuccessPayload)
        };

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        handler.Requests.Count.ShouldBe(1);
    }

    [Test]
    public async Task AddCommunicationResilienceHandler_ReturnsFinalRawFailureResponseAsync()
    {
        var handler = new SequenceHandler(static (attempt, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(attempt == 1 ? string.Empty : SuccessPayload)
            }));
        await using var services = CreateProvider(ResilientClientName, handler, ConfigureSingleRetry);
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(ResilientClientName);

        using var response = await client.GetAsync(ExampleRequestUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).ShouldBe(SuccessPayload);
        handler.Requests.Count.ShouldBe(2);
    }

    [Test]
    public async Task AddCommunicationResilienceHandler_RethrowsOriginalTransportExceptionAsync()
    {
        var expected = new HttpRequestException(TransportFailureMessage);
        var handler = new SequenceHandler((_, _) => throw expected);
        await using var services = CreateProvider(ResilientClientName, handler, ConfigureNoRetry);
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(ResilientClientName);

        var thrown = await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync(ExampleRequestUrl));

        thrown.ShouldBeSameAs(expected);
        handler.Requests.Count.ShouldBe(1);
    }

    [Test]
    public async Task AddCommunicationResilienceHandler_UsesRetryAfterMaximumAsync()
    {
        var handler = new SequenceHandler(static (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
            return Task.FromResult(response);
        });
        await using var services = CreateProvider(ResilientClientName, handler, static options =>
        {
            ConfigureSingleRetry(options);
            options.Execution.Retry.MaxRetryAfter = TimeSpan.FromSeconds(1);
        });
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(ResilientClientName);

        using var response = await client.GetAsync(ExampleRequestUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        handler.Requests.Count.ShouldBe(1);
    }

    [Test]
    public async Task AddCommunicationResilienceHandler_PartitionsCircuitByAuthorityAsync()
    {
        var handler = new SequenceHandler(static (_, request) => Task.FromResult(
            new HttpResponseMessage(request.RequestUri == new Uri(ExampleRequestUrl)
                ? HttpStatusCode.ServiceUnavailable
                : HttpStatusCode.OK)));
        await using var services = CreateProvider(ResilientClientName, handler, static options =>
        {
            options.Execution.Retry.Enabled = false;
            options.Execution.CircuitBreaker.FailureRatio = 1D;
            options.Execution.CircuitBreaker.MinimumThroughput = 1;
            options.Execution.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(1);
            options.Execution.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
        });
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(ResilientClientName);

        using var firstResponse = await client.GetAsync(ExampleRequestUrl);
        using var otherResponse = await client.GetAsync(OtherRequestUrl);
        await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync(ExampleRequestUrl));

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        otherResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        handler.Requests.Count.ShouldBe(2);
    }

    [Test]
    public async Task RemoveCommunicationResilienceHandler_RemovesInheritedHandlerAsync()
    {
        var handler = new SequenceHandler(static (attempt, _) => Task.FromResult(
            new HttpResponseMessage(attempt == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)));
        var serviceCollection = new ServiceCollection().AddLogging();
        serviceCollection.ConfigureHttpClientDefaults(builder =>
            builder.AddCommunicationResilienceHandler(ConfigureSingleRetry));
        serviceCollection
            .AddHttpClient(RemovedClientName)
            .RemoveCommunicationResilienceHandler()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var services = serviceCollection.BuildServiceProvider();
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(RemovedClientName);

        using var response = await client.GetAsync(ExampleRequestUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        handler.Requests.Count.ShouldBe(1);
    }

    private static ServiceProvider CreateProvider(
        string clientName,
        HttpMessageHandler handler,
        Action<CommandHttpClientOptions> configure)
    {
        var serviceCollection = new ServiceCollection().AddLogging();
        serviceCollection
            .AddHttpClient(clientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddCommunicationResilienceHandler(configure);
        return serviceCollection.BuildServiceProvider();
    }

    private static void ConfigureSingleRetry(CommandHttpClientOptions options)
    {
        options.Execution.Retry.MaxRetries = 1;
        options.Execution.Retry.Delay = TimeSpan.Zero;
        options.Execution.Retry.UseJitter = false;
        options.Execution.CircuitBreaker.Enabled = false;
    }

    private static void ConfigureNoRetry(CommandHttpClientOptions options)
    {
        ConfigureSingleRetry(options);
        options.Execution.Retry.Enabled = false;
    }

    private sealed class SequenceHandler(
        Func<int, HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return send(Requests.Count, request);
        }
    }
}

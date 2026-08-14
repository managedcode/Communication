using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.AspNetCore.Filters;
using ManagedCode.Communication.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class CommunicationCqrsFacadeIntegrationTests
{
    [Fact]
    public void AddCommunicationCqrs_AddsCqrsResultActionFilter()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        ManagedCode.Communication.AspNetCore.Extensions.CommunicationCqrsServiceCollectionExtensions.AddCommunicationCqrs(services);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>();
        options.Value.Filters.OfType<TypeFilterAttribute>().ShouldContain(filter =>
            filter.ImplementationType == typeof(CqrsResultActionFilter));
    }

    [Fact]
    public void AddCommunicationCqrsFilters_AliasesToFacadeAndRegistersFilter()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        ManagedCode.Communication.AspNetCore.Extensions.CommunicationCqrsServiceCollectionExtensions.AddCommunicationCqrsFilters(services);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>();
        options.Value.Filters.OfType<TypeFilterAttribute>().ShouldContain(filter =>
            filter.ImplementationType == typeof(CqrsResultActionFilter));
    }

    [Fact]
    public void AddCommunicationCqrs_AddsCqrsResultActionFilterInCoreExtension()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        ManagedCode.Communication.CQRS.AspNetCore.Extensions.CommunicationServiceCollectionExtensions.AddCommunicationCqrs(
            services);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>();
        options.Value.Filters.OfType<TypeFilterAttribute>().ShouldContain(filter =>
            filter.ImplementationType == typeof(CqrsResultActionFilter));
    }

    [Fact]
    public void AddCommunicationCqrsFiltersToMvcOptionsWrapper_AddsFilter()
    {
        var options = new MvcOptions();
        CommunicationCqrsMvcOptionsExtensions.AddCommunicationCqrsFilters(options);

        options.Filters.OfType<TypeFilterAttribute>().ShouldContain(filter =>
            filter.ImplementationType == typeof(CqrsResultActionFilter));
    }

    [Fact]
    public async Task CommunicationCqrsEndpointFacade_ConvertsCompletedIAsyncEnumerableToSse()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            var endpoint = app.MapGet("/cqrs-facade-stream", static () => RunCompletedStream());
            ManagedCode.Communication.AspNetCore.Extensions.CommunicationCqrsEndpointExtensions.WithCommunicationCqrsResults(endpoint);
        });

        using var response = await app.GetTestClient().GetAsync("/cqrs-facade-stream");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<FacadeProgressUpdate, FacadeFinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        var chunks = new List<CqrsStreamChunk<FacadeProgressUpdate, FacadeFinalResult>>();
        await foreach (var item in parser.EnumerateAsync())
        {
            chunks.Add(item.Data);
        }

        chunks.Count.ShouldBe(2);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[1].Final!.Value.Value!.Status.ShouldBe("done");
    }

    [Fact]
    public async Task CommunicationCqrsEndpointFacade_GroupAlias_ConvertsChunkStream()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            var group = ManagedCode.Communication.AspNetCore.Extensions.CommunicationCqrsEndpointExtensions.WithCommunicationCqrsResults(app.MapGroup("/api"));
            group.MapGet("/cqrs-facade-group", static () => RunCompletedStream());
        });

        using var response = await app.GetTestClient().GetAsync("/api/cqrs-facade-group");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<FacadeProgressUpdate, FacadeFinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        var chunks = new List<CqrsStreamChunk<FacadeProgressUpdate, FacadeFinalResult>>();
        await foreach (var item in parser.EnumerateAsync())
        {
            chunks.Add(item.Data);
        }

        chunks.Count.ShouldBe(2);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
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

    private static async IAsyncEnumerable<CqrsStreamChunk<FacadeProgressUpdate, FacadeFinalResult>> RunCompletedStream()
    {
        yield return CqrsStreamChunk<FacadeProgressUpdate, FacadeFinalResult>.Started(
            Result<FacadeProgressUpdate>.Succeed(new FacadeProgressUpdate("started")),
            sequence: 1);

        await Task.Delay(1);
        yield return CqrsStreamChunk<FacadeProgressUpdate, FacadeFinalResult>.Completed(
            Result<FacadeFinalResult>.Succeed(new FacadeFinalResult("done")),
            sequence: 2);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public sealed record FacadeProgressUpdate(string State);
public sealed record FacadeFinalResult(string Status);

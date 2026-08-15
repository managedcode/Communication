using System;
using System.Threading.Tasks;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Extensions;
using ManagedCode.Communication.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests;

/// <summary>
///     The registration snippets from the README, compiled and executed.
/// </summary>
/// <remarks>
///     Setup instructions are the first thing a reader copies and the easiest thing to let rot. Keeping them here
///     means a renamed or removed entry point breaks the build rather than someone's afternoon.
/// </remarks>
[Collection(ManagedCode.Communication.Tests.Logging.GlobalLoggerCollection.Name)]
public class RegistrationReferenceTests
{
    [Fact]
    public async Task TheTypicalWebApplicationSnippetRuns()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddCommunication();
        builder.Services.AddCommunicationCqrs();
        builder.Services.AddCommandIdempotency();

        await using var app = builder.Build();
        app.UseCommunication();

        await app.StartAsync();

        app.Services.GetService<ICommandIdempotencyStore>().ShouldNotBeNull();
    }

    [Fact]
    public void EveryDocumentedServiceRegistrationResolves()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();

        services.AddCommunicationAspNetCore();
        services.AddCommunicationFilters();
        services.AddCommunicationCqrs();
        services.AddCommandIdempotency();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICommandIdempotencyStore>().ShouldNotBeNull();
    }

    [Fact]
    public void TheStoreOnlyRegistrationsResolve()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        services.AddCommandIdempotencyStore<ManagedCode.Communication.Commands.Stores.MemoryCacheCommandIdempotencyStore>();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICommandIdempotencyStore>().ShouldNotBeNull();
    }

    [Fact]
    public void TheSignalRHubFilterRegistrationCompiles()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSignalR(options => options.AddCommunicationHubFilter());

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HubOptions>>()
            .Value.ShouldNotBeNull();
    }

    [Fact]
    public void TheTelemetrySourceNameIsWhatCallersRegister()
    {
        // The snippet passes this constant to AddSource/AddMeter; if it drifts, the wiring silently collects
        // nothing, which is exactly the kind of failure nobody notices.
        CommunicationTelemetry.SourceName.ShouldBe("ManagedCode.Communication");
        CommunicationTelemetry.ActivitySource.Name.ShouldBe(CommunicationTelemetry.SourceName);
        CommunicationTelemetry.Meter.Name.ShouldBe(CommunicationTelemetry.SourceName);
    }

    [Fact]
    public void TheCommandMetadataSnippetCompiles()
    {
        var command = Command<string>.From("payload")
            .WithCorrelationId("correlation")
            .WithCausationId("parent")
            .WithTraceId("trace")
            .WithSpanId("span")
            .WithUserId("user")
            .WithSessionId("session")
            .WithMetadata(metadata => metadata.Priority = CommandPriority.High);

        // Correlation, causation and user live on the command; the rest on its metadata.
        command.CorrelationId.ShouldBe("correlation");
        command.CausationId.ShouldBe("parent");
        command.UserId.ShouldBe("user");
        command.SessionId.ShouldBe("session");
        command.TraceId.ShouldBe("trace");
        command.SpanId.ShouldBe("span");
        command.Metadata.ShouldNotBeNull();
        command.Metadata!.Priority.ShouldBe(CommandPriority.High);
    }
}

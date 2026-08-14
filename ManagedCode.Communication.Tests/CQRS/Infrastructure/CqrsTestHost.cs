using System;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS.AspNetCore;
using ManagedCode.Communication.CQRS.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     Spins up in-memory hosts for the CQRS transport tests.
/// </summary>
public static class CqrsTestHost
{
    /// <summary>
    ///     Starts a Minimal API host. <paramref name="configureServices" /> runs before the app is built.
    /// </summary>
    public static async Task<WebApplication> StartMinimalApiAsync(
        Action<WebApplication> configureEndpoints,
        Action<IServiceCollection>? configureServices = null)
    {
        ArgumentNullException.ThrowIfNull(configureEndpoints);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();
        configureEndpoints(app);

        await app.StartAsync();
        return app;
    }

    /// <summary>
    ///     Starts an MVC host with the CQRS action filter registered and controllers from this test assembly mapped.
    /// </summary>
    public static async Task<WebApplication> StartMvcAsync(
        Action<CqrsStreamServerOptions>? configureCqrs = null,
        Action<WebApplication>? configureEndpoints = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddCommunicationCqrs(configureCqrs);
        builder.Services
            .AddControllers(static options => options.AddCommunicationCqrsFilters())
            .AddApplicationPart(typeof(CqrsTestController).Assembly);

        var app = builder.Build();
        app.MapControllers();
        configureEndpoints?.Invoke(app);

        await app.StartAsync();
        return app;
    }
}

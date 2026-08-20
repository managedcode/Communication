using System.Linq;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Tests.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class CommandIdempotencyServiceCollectionExtensionsTests
{
    [Test]
    public void AddCommandIdempotency_RegistersStoreAndBackgroundCleanup()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddCommandIdempotency<TestCommandIdempotencyStore>(options =>
        {
            options.CleanupInterval = System.TimeSpan.FromMinutes(15);
            options.LogHealthMetrics = false;
        });

        using var provider = services.BuildServiceProvider();
        var store = provider.GetService<ICommandIdempotencyStore>();
        store.ShouldBeOfType<TestCommandIdempotencyStore>();

        var options = provider.GetRequiredService<CommandCleanupOptions>();
        options.CleanupInterval.ShouldBe(System.TimeSpan.FromMinutes(15));
        options.LogHealthMetrics.ShouldBeFalse();

        var hosted = provider.GetServices<IHostedService>()
            .OfType<CommandCleanupBackgroundService>()
            .ToList();

        hosted.Count.ShouldBe(1);
    }

    [Test]
    public void AddCommandIdempotency_WithInstance_RegistersSameStoreInstance()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var instance = new TestCommandIdempotencyStore();

        services.AddCommandIdempotency(instance);

        using var provider = services.BuildServiceProvider();
        var store = provider.GetService<ICommandIdempotencyStore>();
        store.ShouldBeSameAs(instance);
        var hosted = provider.GetServices<IHostedService>()
            .OfType<CommandCleanupBackgroundService>()
            .ToList();
        hosted.Count.ShouldBe(1);
    }

    [Test]
    public void AddCommandIdempotencyStore_DoesNotRegisterBackgroundCleanup()
    {
        var services = new ServiceCollection();

        services.AddCommandIdempotencyStore<TestCommandIdempotencyStore>();

        using var provider = services.BuildServiceProvider();
        provider.GetService<ICommandIdempotencyStore>().ShouldBeOfType<TestCommandIdempotencyStore>();
        var hosted = provider.GetServices<IHostedService>()
            .OfType<CommandCleanupBackgroundService>()
            .ToList();

        hosted.ShouldBeEmpty();
    }

    [Test]
    public void AddCommandIdempotencyWithManualCleanup_DoesNotRegisterHostedServiceAndPreservesOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var options = new CommandCleanupOptions
        {
            CleanupInterval = System.TimeSpan.FromMinutes(30),
            CompletedCommandMaxAge = System.TimeSpan.FromHours(2)
        };

        services.AddCommandIdempotencyWithManualCleanup<TestCommandIdempotencyStore>(options);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<CommandCleanupOptions>().ShouldBeSameAs(options);
        provider.GetService<ICommandIdempotencyStore>().ShouldBeOfType<TestCommandIdempotencyStore>();
        provider.GetServices<IHostedService>()
            .OfType<CommandCleanupBackgroundService>()
            .ShouldBeEmpty();
    }
}

using System;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Commands.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ManagedCode.Communication.Tests.Commands;

public sealed class CommandExecutionRegistrationTests
{
    [Test]
    public void AddCommandExecution_RepeatedConfiguration_ComposesInRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommandExecution(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 1;
        });
        services.AddCommandExecution(options =>
        {
            options.Retry.MaxRetries = 4;
            options.Timeout.TotalTimeout = TimeSpan.FromSeconds(8);
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<CommandExecutionOptions>();
        var runtime = provider.GetRequiredService<CommandExecutionRuntime>();

        options.Retry.Enabled.ShouldBeTrue();
        options.Retry.MaxRetries.ShouldBe(4);
        options.Timeout.TotalTimeout.ShouldBe(TimeSpan.FromSeconds(8));
        runtime.Options.Retry.MaxRetries.ShouldBe(4);
    }

    [Test]
    public void CommandExecutionRuntime_SnapshotsMutableOptions()
    {
        var options = new CommandExecutionOptions();
        options.Retry.MaxRetries = 2;
        var runtime = new CommandExecutionRuntime(options);

        options.Retry.MaxRetries = 99;
        options.Timeout.TotalTimeout = TimeSpan.FromDays(1);
        var detachedSnapshot = runtime.Options;
        detachedSnapshot.Retry.MaxRetries = 100;

        runtime.Options.Retry.MaxRetries.ShouldBe(2);
        runtime.Options.Timeout.TotalTimeout.ShouldBe(TimeSpan.FromSeconds(30));
    }
}

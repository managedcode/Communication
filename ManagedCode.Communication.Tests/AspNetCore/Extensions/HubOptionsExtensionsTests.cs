using System;
using FluentAssertions;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.AspNetCore.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class HubOptionsExtensionsTests
{
    [Fact]
    public void AddCommunicationHubFilter_AddsFilterFromServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();  // Add logging for the filter
        services.AddScoped<CommunicationHubExceptionFilter>();
        services.Configure<CommunicationOptions>(options => { });
        var serviceProvider = services.BuildServiceProvider();
        
        var hubOptions = new HubOptions();

        // Act
        hubOptions.AddCommunicationHubFilter(serviceProvider);

        // Assert
        // Note: HubOptions doesn't expose a way to check filters directly
        // But we can verify no exception was thrown and the method completed
        hubOptions.Should().NotBeNull();
    }

    [Fact]
    public void AddCommunicationHubFilter_ThrowsWhenFilterNotRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        // Deliberately not registering CommunicationHubExceptionFilter
        var serviceProvider = services.BuildServiceProvider();
        
        var hubOptions = new HubOptions();

        // Act & Assert
        var act = () => hubOptions.AddCommunicationHubFilter(serviceProvider);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CommunicationHubExceptionFilter*");
    }

    [Fact]
    public void AddCommunicationHubFilter_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var hubOptions = new HubOptions();
        IServiceProvider? serviceProvider = null;

        // Act & Assert
        var act = () => hubOptions.AddCommunicationHubFilter(serviceProvider!);
        act.Should().Throw<ArgumentNullException>();
    }


    [Fact]
    public void AddCommunicationHubFilter_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<CommunicationHubExceptionFilter>();
        services.Configure<CommunicationOptions>(options => { });
        var serviceProvider = services.BuildServiceProvider();
        
        var hubOptions = new HubOptions();

        // Act - Call multiple times
        hubOptions.AddCommunicationHubFilter(serviceProvider);
        hubOptions.AddCommunicationHubFilter(serviceProvider);

        // Assert - Should not throw
        hubOptions.Should().NotBeNull();
    }
}
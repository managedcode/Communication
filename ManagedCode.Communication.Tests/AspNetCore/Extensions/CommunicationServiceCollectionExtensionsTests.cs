using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class CommunicationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCommunicationAspNetCore_RegistersHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCommunicationAspNetCore();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        
        hostedServices.Should().Contain(x => x.GetType().Name == "CommunicationLoggerConfigurationService");
    }

    [Fact]
    public void AddCommunicationAspNetCore_WithLoggerFactory_ConfiguresLogger()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        services.AddCommunicationAspNetCore(loggerFactory);

        // Assert
        // Verify that CommunicationLogger was configured
        var logger = CommunicationLogger.GetLogger<CommunicationServiceCollectionExtensionsTests>();
        logger.Should().NotBeNull();
    }

    [Fact]
    public void AddCommunicationAspNetCore_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddCommunicationAspNetCore();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCommunicationAspNetCore_WithLoggerFactory_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        var result = services.AddCommunicationAspNetCore(loggerFactory);

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public async Task CommunicationLoggerConfigurationService_StartsAndConfiguresLogger()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommunicationAspNetCore();

        var serviceProvider = services.BuildServiceProvider();
        var hostedService = serviceProvider.GetServices<IHostedService>()
            .First(x => x.GetType().Name == "CommunicationLoggerConfigurationService");

        // Act
        await hostedService.StartAsync(CancellationToken.None);

        // Assert
        var logger = CommunicationLogger.GetLogger<CommunicationServiceCollectionExtensionsTests>();
        logger.Should().NotBeNull();
    }

    [Fact]
    public async Task CommunicationLoggerConfigurationService_StopsWithoutError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommunicationAspNetCore();

        var serviceProvider = services.BuildServiceProvider();
        var hostedService = serviceProvider.GetServices<IHostedService>()
            .First(x => x.GetType().Name == "CommunicationLoggerConfigurationService");

        await hostedService.StartAsync(CancellationToken.None);

        // Act & Assert
        var act = () => hostedService.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CommunicationLoggerConfigurationService_WithCancellation_HandlesCancellation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommunicationAspNetCore();

        var serviceProvider = services.BuildServiceProvider();
        var hostedService = serviceProvider.GetServices<IHostedService>()
            .First(x => x.GetType().Name == "CommunicationLoggerConfigurationService");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Should not throw on cancelled token
        await hostedService.StartAsync(cts.Token);
        await hostedService.StopAsync(cts.Token);
    }
}
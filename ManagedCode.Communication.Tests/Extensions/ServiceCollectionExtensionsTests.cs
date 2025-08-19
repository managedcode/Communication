using System;
using FluentAssertions;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void ConfigureCommunication_WithLoggerFactory_ConfiguresLogger()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        services.ConfigureCommunication(loggerFactory);

        // Assert
        // Verify that CommunicationLogger was configured
        var logger = CommunicationLogger.GetLogger<ServiceCollectionExtensionsTests>();
        logger.Should().NotBeNull();
    }


    [Fact]
    public void ConfigureCommunication_WithLoggerFactory_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        var result = services.ConfigureCommunication(loggerFactory);

        // Assert
        result.Should().BeSameAs(services);
    }

}

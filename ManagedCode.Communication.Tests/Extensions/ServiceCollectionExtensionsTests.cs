using System;
using Shouldly;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void LoggerCenter_SourceGenerators_Work()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ServiceCollectionExtensionsTests>();
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw, LoggerCenter methods should be generated
        LoggerCenter.LogControllerException(logger, exception, "TestController", "TestAction");
        LoggerCenter.LogValidationFailed(logger, "TestAction");
        LoggerCenter.LogCommandCleanupExpired(logger, 5, TimeSpan.FromHours(1));
        
        // This test passes if Source Generators work correctly
        true.ShouldBeTrue();
    }

    [Fact]
    public void CommunicationLogger_Caching_WorksCorrectly()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        CommunicationLogger.Configure(loggerFactory);

        var logger1 = CommunicationLogger.GetLogger();
        var logger2 = CommunicationLogger.GetLogger();

        logger1.ShouldBeSameAs(logger2);
    }

    [Fact]
    public void ConfigureCommunication_WithLoggerFactory_ConfiguresLoggerAndReturns()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Act
        var result = services.ConfigureCommunication(loggerFactory);

        // Assert
        result.ShouldBeSameAs(services);
        var logger = CommunicationLogger.GetLogger();
        logger.ShouldNotBeNull();
    }
}

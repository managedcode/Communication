using System;
using Shouldly;
using ManagedCode.Communication.AspNetCore.Extensions;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class HubOptionsExtensionsTests
{
    [Fact]
    public void AddCommunicationHubFilter_DoesNotThrow()
    {
        // Arrange
        var hubOptions = new HubOptions();

        // Act & Assert - Should complete without throwing
        var act = () => hubOptions.AddCommunicationHubFilter();
        Should.NotThrow(act);
    }

    [Fact]
    public void AddCommunicationHubFilter_WithNullHubOptions_ThrowsArgumentNullException()
    {
        // Arrange
        HubOptions? hubOptions = null;

        // Act & Assert
        var act = () => hubOptions!.AddCommunicationHubFilter();
        var exception = Should.Throw<ArgumentNullException>(act);
        exception.ParamName.ShouldBe("options");
    }

    [Fact]
    public void AddCommunicationHubFilter_CanBeCalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var hubOptions = new HubOptions();

        // Act & Assert - Multiple calls should not throw
        var act = () =>
        {
            hubOptions.AddCommunicationHubFilter();
            hubOptions.AddCommunicationHubFilter();
            hubOptions.AddCommunicationHubFilter();
        };
        
        Should.NotThrow(act);
    }
}

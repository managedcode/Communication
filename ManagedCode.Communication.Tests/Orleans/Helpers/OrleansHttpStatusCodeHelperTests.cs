using System;
using System.Reflection;
using System.Net;
using ManagedCode.Communication.Helpers;
using ManagedCode.Communication.Orleans.Helpers;
using Orleans.Runtime;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Orleans.Helpers;

public class OrleansHttpStatusCodeHelperTests
{
    [Fact]
    public void GetStatusCodeForException_SiloUnavailable_ReturnsServiceUnavailable()
    {
        var statusCode = OrleansHttpStatusCodeHelper.GetStatusCodeForException(new SiloUnavailableException("silo down"));

        statusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public void GetStatusCodeForException_OrleansMessageRejection_ReturnsServiceUnavailable()
    {
        var exception = (OrleansMessageRejectionException)Activator.CreateInstance(
            typeof(OrleansMessageRejectionException),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new object[] { "message rejected" },
            null)!;
        var statusCode = OrleansHttpStatusCodeHelper.GetStatusCodeForException(exception);

        statusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public void GetStatusCodeForException_Timeout_ReturnsGatewayTimeout()
    {
        var statusCode = OrleansHttpStatusCodeHelper.GetStatusCodeForException(new TimeoutException("timeout"));

        statusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
    }

    [Fact]
    public void GetStatusCodeForException_GrainExtensionNotInstalled_ReturnsNotImplemented()
    {
        var statusCode = OrleansHttpStatusCodeHelper.GetStatusCodeForException(
            new GrainExtensionNotInstalledException("missing extension"));

        statusCode.ShouldBe(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public void GetStatusCodeForException_OrleansConfiguration_ReturnsInternalServerError()
    {
        var statusCode = OrleansHttpStatusCodeHelper.GetStatusCodeForException(
            new OrleansConfigurationException("bad config"));

        statusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void GetStatusCodeForException_UnknownExceptionFallsBackToBaseHelper()
    {
        var statusCode = OrleansHttpStatusCodeHelper.GetStatusCodeForException(new InvalidOperationException("bad"));

        statusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}

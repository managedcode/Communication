using ManagedCode.Communication.Extensions.Filters;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Tests.Common.TestApp;

public class TestHubExceptionFilter(ILogger<TestHubExceptionFilter> logger) : HubExceptionFilterBase(logger)
{
    // Uses default implementation from base class
}
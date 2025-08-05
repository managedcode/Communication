using ManagedCode.Communication.Extensions.Filters;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Tests.Common.TestApp;

public class TestModelValidationFilter(ILogger<TestModelValidationFilter> logger) : ModelValidationFilterBase(logger)
{
    // Uses default implementation from base class
}
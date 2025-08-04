using ManagedCode.Communication.Extensions;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Tests.Common.TestApp;

public class TestModelValidationFilter(ILogger<TestModelValidationFilter> logger) : ModelValidationFilterBase(logger); 
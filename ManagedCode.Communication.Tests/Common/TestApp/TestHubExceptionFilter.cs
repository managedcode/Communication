using ManagedCode.Communication.Extensions;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Tests.Common.TestApp;

public class TestHubExceptionFilter(ILogger<TestHubExceptionFilter> logger) : HubExceptionFilterBase(logger); 
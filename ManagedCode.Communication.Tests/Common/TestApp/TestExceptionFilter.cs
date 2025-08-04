using ManagedCode.Communication.Extensions;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Tests.Common.TestApp;

public class TestExceptionFilter(ILogger<TestExceptionFilter> logger) : ExceptionFilterBase(logger); 
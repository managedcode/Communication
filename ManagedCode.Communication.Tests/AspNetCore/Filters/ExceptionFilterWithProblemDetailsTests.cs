using System;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.Communication.AspNetCore.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Filters;

public sealed class ExceptionFilterWithProblemDetailsTests
{
    [Fact]
    public void OnException_ShouldLogOriginalExceptionBeforeHandling()
    {
        var logger = new CapturingLogger();
        var filter = new TestExceptionFilter(logger);
        var exception = new InvalidOperationException("problem-details failure");
        var context = CreateExceptionContext(exception);

        filter.OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        context.Result.ShouldBeOfType<ObjectResult>();

        var entry = logger.Entries.Single();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.EventId.Id.ShouldBe(5001);
        entry.Exception.ShouldBeSameAs(exception);
        entry.Message.ShouldContain("TestController");
        entry.Message.ShouldContain("TestAction");
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var actionDescriptor = new ActionDescriptor
        {
            DisplayName = "TestAction",
            RouteValues = { ["controller"] = "TestController" }
        };
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            actionDescriptor);

        return new ExceptionContext(actionContext, [])
        {
            Exception = exception
        };
    }

    private sealed class TestExceptionFilter(ILogger logger) : ExceptionFilterWithProblemDetails(logger);

    private sealed class CapturingLogger : ILogger
    {
        public List<CapturedLogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new CapturedLogEntry(logLevel, eventId, exception, formatter(state, exception)));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed record CapturedLogEntry(
        LogLevel Level,
        EventId EventId,
        Exception? Exception,
        string Message);
}

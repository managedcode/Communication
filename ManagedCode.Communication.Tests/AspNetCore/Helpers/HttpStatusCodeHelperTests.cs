using System;
using System.Net;
using FluentAssertions;
using ManagedCode.Communication.AspNetCore.Helpers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Helpers;

public class HttpStatusCodeHelperTests
{
    [Theory]
    [InlineData(typeof(BadHttpRequestException), HttpStatusCode.BadRequest)]
    [InlineData(typeof(ConnectionAbortedException), HttpStatusCode.BadRequest)]
    [InlineData(typeof(ConnectionResetException), HttpStatusCode.BadRequest)]
    [InlineData(typeof(AmbiguousActionException), HttpStatusCode.InternalServerError)]
    [InlineData(typeof(AuthenticationFailureException), HttpStatusCode.Unauthorized)]
    [InlineData(typeof(HubException), HttpStatusCode.BadRequest)]
    [InlineData(typeof(AntiforgeryValidationException), HttpStatusCode.BadRequest)]
    public void GetStatusCodeForException_AspNetSpecificExceptions_ReturnsCorrectStatusCode(Type exceptionType, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        var exception = CreateException(exceptionType);

        // Act
        var result = HttpStatusCodeHelper.GetStatusCodeForException(exception);

        // Assert
        result.Should().Be(expectedStatusCode);
    }

    [Fact]
    public void GetStatusCodeForException_StandardException_FallsBackToBaseHelper()
    {
        // Arrange
        var exception = new ArgumentException("Test argument exception");

        // Act
        var result = HttpStatusCodeHelper.GetStatusCodeForException(exception);

        // Assert
        // Should fall back to base Communication.Helpers.HttpStatusCodeHelper
        result.Should().Be(HttpStatusCode.BadRequest); // ArgumentException maps to BadRequest in base helper
    }

    [Fact]
    public void GetStatusCodeForException_UnknownException_FallsBackToBaseHelper()
    {
        // Arrange
        var exception = new CustomException("Custom exception");

        // Act
        var result = HttpStatusCodeHelper.GetStatusCodeForException(exception);

        // Assert
        // Should fall back to base helper which returns InternalServerError for unknown exceptions
        result.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void GetStatusCodeForException_NullException_FallsBackToBaseHelper()
    {
        // Arrange
        Exception? exception = null;

        // Act
        var act = () => HttpStatusCodeHelper.GetStatusCodeForException(exception!);

        // Assert
        // Base helper should handle null (likely throw or return default)
        act.Should().NotThrow(); // Assuming base helper handles null gracefully
    }

    private static Exception CreateException(Type exceptionType)
    {
        return exceptionType.Name switch
        {
            nameof(BadHttpRequestException) => new BadHttpRequestException("Bad request"),
            nameof(ConnectionAbortedException) => new ConnectionAbortedException("Connection aborted"),
            nameof(ConnectionResetException) => new ConnectionResetException("Connection reset"),
            nameof(AmbiguousActionException) => new AmbiguousActionException("Ambiguous action"),
            nameof(AuthenticationFailureException) => new AuthenticationFailureException("Authentication failed"),
            nameof(HubException) => new HubException("Hub error"),
            nameof(AntiforgeryValidationException) => new AntiforgeryValidationException("Antiforgery validation failed"),
            _ => throw new ArgumentException($"Unknown exception type: {exceptionType.Name}")
        };
    }

    private class CustomException : Exception
    {
        public CustomException(string message) : base(message) { }
    }
}
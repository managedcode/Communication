using System;
using System.Net;
using ManagedCode.Communication.AspNetCore.Helpers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Shouldly;

namespace ManagedCode.Communication.Tests.AspNetCore.Helpers;

public class HttpStatusCodeHelperTests
{
    [Test]
    [Arguments(typeof(BadHttpRequestException), HttpStatusCode.BadRequest)]
    [Arguments(typeof(ConnectionAbortedException), HttpStatusCode.BadRequest)]
    [Arguments(typeof(ConnectionResetException), HttpStatusCode.BadRequest)]
    [Arguments(typeof(AmbiguousActionException), HttpStatusCode.InternalServerError)]
    [Arguments(typeof(AuthenticationFailureException), HttpStatusCode.Unauthorized)]
    [Arguments(typeof(HubException), HttpStatusCode.BadRequest)]
    [Arguments(typeof(AntiforgeryValidationException), HttpStatusCode.BadRequest)]
    public void GetStatusCodeForException_AspNetSpecificExceptions_ReturnsCorrectStatusCode(Type exceptionType, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        var exception = CreateException(exceptionType);

        // Act
        var result = HttpStatusCodeHelper.GetStatusCodeForException(exception);

        // Assert
        result.ShouldBe(expectedStatusCode);
    }

    [Test]
    public void GetStatusCodeForException_StandardException_FallsBackToBaseHelper()
    {
        // Arrange
        var exception = new ArgumentException("Test argument exception");

        // Act
        var result = HttpStatusCodeHelper.GetStatusCodeForException(exception);

        // Assert
        // Should fall back to base Communication.Helpers.HttpStatusCodeHelper
        result.ShouldBe(HttpStatusCode.BadRequest); // ArgumentException maps to BadRequest in base helper
    }

    [Test]
    public void GetStatusCodeForException_UnknownException_FallsBackToBaseHelper()
    {
        // Arrange
        var exception = new CustomException("Custom exception");

        // Act
        var result = HttpStatusCodeHelper.GetStatusCodeForException(exception);

        // Assert
        // Should fall back to base helper which returns InternalServerError for unknown exceptions
        result.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Test]
    public void GetStatusCodeForException_NullException_Throws()
    {
        Exception? exception = null;

        Should.Throw<ArgumentNullException>(() => HttpStatusCodeHelper.GetStatusCodeForException(exception!));
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ManagedCode.Communication.Extensions.Helpers;

public static class HttpStatusCodeHelper
{
    public static HttpStatusCode GetStatusCodeForException(Exception exception)
    {
        return exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            NotSupportedException => HttpStatusCode.BadRequest,
            FormatException => HttpStatusCode.BadRequest,
            JsonException => HttpStatusCode.BadRequest,
            XmlException => HttpStatusCode.BadRequest,

            UnauthorizedAccessException => HttpStatusCode.Unauthorized,

            SecurityException => HttpStatusCode.Forbidden,

            FileNotFoundException => HttpStatusCode.NotFound,
            DirectoryNotFoundException => HttpStatusCode.NotFound,
            KeyNotFoundException => HttpStatusCode.NotFound,

            TimeoutException => HttpStatusCode.RequestTimeout,
            TaskCanceledException => HttpStatusCode.RequestTimeout,
            OperationCanceledException => HttpStatusCode.RequestTimeout,

            InvalidDataException => HttpStatusCode.Conflict,

            NotImplementedException => HttpStatusCode.NotImplemented,
            NotFiniteNumberException => HttpStatusCode.InternalServerError,
            OutOfMemoryException => HttpStatusCode.InternalServerError,
            StackOverflowException => HttpStatusCode.InternalServerError,
            ThreadAbortException => HttpStatusCode.InternalServerError,

            _ => HttpStatusCode.InternalServerError
        };
    }
}
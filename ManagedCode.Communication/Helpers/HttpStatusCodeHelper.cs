using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ManagedCode.Communication.Helpers;

/// <summary>
///     Maps an exception to the HTTP status code that best describes it.
/// </summary>
/// <remarks>
///     <para>
///         The guiding rule: <b>4xx means the caller did something wrong; 5xx means the server did.</b> Exceptions
///         that indicate a bug in the server — <see cref="InvalidOperationException" /> ("Sequence contains no
///         elements", an invalid EF Core state), <see cref="NotSupportedException" />,
///         <see cref="InvalidCastException" />, <see cref="NullReferenceException" /> — therefore map to
///         <c>500</c>. Reporting those as <c>400</c> blames the client for a server defect and, worse, hides the
///         defect from every dashboard and alert that watches the 5xx rate.
///     </para>
///     <para>
///         Only exceptions that genuinely describe bad input map to <c>400</c>:
///         <see cref="ArgumentException" /> and friends, <see cref="FormatException" />,
///         <see cref="JsonException" />, <see cref="XmlException" />.
///     </para>
///     <para>
///         The mapping is a heuristic and knows nothing about your domain. Override it with
///         <see cref="ExceptionStatusCodeMap" />; overrides win over everything below.
///     </para>
/// </remarks>
public static class HttpStatusCodeHelper
{
    /// <summary>
    ///     Maps an exception to a status code, honouring any override registered in <c>ExceptionStatusCodeMap</c>.
    /// </summary>
    public static HttpStatusCode GetStatusCodeForException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (ExceptionStatusCodeMap.TryResolve(exception, out var configured))
        {
            return configured;
        }

        return exception switch
        {
            // ---- 400: the request itself is malformed ----
            ArgumentNullException => HttpStatusCode.BadRequest,
            ArgumentOutOfRangeException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            FormatException => HttpStatusCode.BadRequest,
            JsonException => HttpStatusCode.BadRequest,
            XmlException => HttpStatusCode.BadRequest,

            // ---- 401 / 403 ----
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            SecurityException => HttpStatusCode.Forbidden,

            // ---- 404 ----
            FileNotFoundException => HttpStatusCode.NotFound,
            DirectoryNotFoundException => HttpStatusCode.NotFound,
            KeyNotFoundException => HttpStatusCode.NotFound,

            // ---- 408 ----
            TimeoutException => HttpStatusCode.RequestTimeout,
            TaskCanceledException => HttpStatusCode.RequestTimeout,
            OperationCanceledException => HttpStatusCode.RequestTimeout,

            // ---- 409 ----
            InvalidDataException => HttpStatusCode.Conflict,

            // ---- 501 ----
            NotImplementedException => HttpStatusCode.NotImplemented,

            // ---- 500: the server is at fault ----
            // These are the ones people most often expect to be 4xx. They are not: each one means the process
            // reached a state its own code did not allow for.
            InvalidOperationException => HttpStatusCode.InternalServerError,
            NotSupportedException => HttpStatusCode.InternalServerError,
            InvalidCastException => HttpStatusCode.InternalServerError,
            NullReferenceException => HttpStatusCode.InternalServerError,
            IndexOutOfRangeException => HttpStatusCode.InternalServerError,
            OutOfMemoryException => HttpStatusCode.InternalServerError,
            StackOverflowException => HttpStatusCode.InternalServerError,
            ApplicationException => HttpStatusCode.InternalServerError,
            SystemException => HttpStatusCode.InternalServerError,

            _ => HttpStatusCode.InternalServerError
        };
    }
}

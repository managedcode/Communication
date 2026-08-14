using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Xml;
using System.Security;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Communication.Helpers;
using Xunit;
using Shouldly;

namespace ManagedCode.Communication.Tests.Helpers;

public class HttpStatusCodeHelperTests
{
    [Theory]
    [MemberData(nameof(ExceptionCases))]
    public void GetStatusCodeForException_ReturnsExpectedStatusCode(Exception exception, HttpStatusCode expectedStatusCode)
    {
        var statusCode = HttpStatusCodeHelper.GetStatusCodeForException(exception);

        statusCode.ShouldBe(expectedStatusCode);
    }

    public static IEnumerable<object[]> ExceptionCases()
    {
        yield return [new ArgumentNullException("value"), HttpStatusCode.BadRequest];
        yield return [new ArgumentOutOfRangeException("value"), HttpStatusCode.BadRequest];
        yield return [new ArgumentException("value"), HttpStatusCode.BadRequest];
        yield return [new InvalidOperationException("value"), HttpStatusCode.InternalServerError];
        yield return [new NotSupportedException("value"), HttpStatusCode.InternalServerError];
        yield return [new FormatException("value"), HttpStatusCode.BadRequest];
        yield return [new JsonException("value"), HttpStatusCode.BadRequest];
        yield return [new XmlException("value"), HttpStatusCode.BadRequest];
        yield return [new InvalidCastException("value"), HttpStatusCode.InternalServerError];
        yield return [new UnauthorizedAccessException("value"), HttpStatusCode.Unauthorized];
        yield return [new SecurityException("value"), HttpStatusCode.Forbidden];
        yield return [new FileNotFoundException("value"), HttpStatusCode.NotFound];
        yield return [new DirectoryNotFoundException("value"), HttpStatusCode.NotFound];
        yield return [new KeyNotFoundException("value"), HttpStatusCode.NotFound];
        yield return [new TimeoutException("value"), HttpStatusCode.RequestTimeout];
        yield return [new TaskCanceledException(), HttpStatusCode.RequestTimeout];
        yield return [new OperationCanceledException("value"), HttpStatusCode.RequestTimeout];
        yield return [new InvalidDataException("value"), HttpStatusCode.Conflict];
        yield return [new NotImplementedException("value"), HttpStatusCode.NotImplemented];
        yield return [new NotFiniteNumberException("value"), HttpStatusCode.InternalServerError];
        yield return [new OutOfMemoryException("value"), HttpStatusCode.InternalServerError];
        yield return [new StackOverflowException(), HttpStatusCode.InternalServerError];
        yield return [new ApplicationException("value"), HttpStatusCode.InternalServerError];
        yield return [new SystemException("value"), HttpStatusCode.InternalServerError];
        yield return [new Exception("value"), HttpStatusCode.InternalServerError];
    }
}

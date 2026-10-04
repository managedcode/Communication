using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Telemetry;

namespace ManagedCode.Communication.CQRS;

internal static class CqrsFailureBodyReader
{
    private const int DetectionBytes = 1;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static async Task<Problem> ReadAsync(
        HttpResponseMessage response,
        CqrsStreamClientOptions options,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is { } length && length > options.MaximumFailureBodyBytes)
        {
            return Limited(response.StatusCode);
        }

        var buffer = new byte[checked(options.MaximumFailureBodyBytes + DetectionBytes)];
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var count = await ReadBodyAsync(stream, buffer, options.MaximumFailureBodyBytes, cancellationToken)
            .ConfigureAwait(false);

        if (count > options.MaximumFailureBodyBytes)
        {
            return Limited(response.StatusCode);
        }

        try
        {
            _ = StrictUtf8.GetCharCount(buffer.AsSpan(0, count));
        }
        catch (DecoderFallbackException exception)
        {
            CommunicationDiagnostics.ReportFailure(CommunicationLogger.GetLogger(), null, exception);
            return SafeFailure(response.StatusCode);
        }

        return SafeFailure(response.StatusCode, ParseProblem(response, buffer.AsSpan(0, count), options));
    }

    private static async Task<int> ReadBodyAsync(
        Stream stream,
        byte[] buffer,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        var count = 0;
        while (count < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(count), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            count += read;
            if (count > maximumBytes)
            {
                break;
            }
        }

        return count;
    }

    private static Problem? ParseProblem(
        HttpResponseMessage response,
        ReadOnlySpan<byte> body,
        CqrsStreamClientOptions options)
    {
        if (IsWhitespace(body))
        {
            return null;
        }

        Problem? problem;
        try
        {
            problem = JsonSerializer.Deserialize<Problem>(body, options.ResolveJsonOptions());
        }
        catch (JsonException exception)
        {
            CommunicationDiagnostics.ReportFailure(CommunicationLogger.GetLogger(), null, exception);
            return null;
        }
        catch (NotSupportedException exception)
        {
            CommunicationDiagnostics.ReportFailure(CommunicationLogger.GetLogger(), null, exception);
            return null;
        }

        if (problem is null)
        {
            return null;
        }

        if (problem.StatusCode == 0)
        {
            problem.StatusCode = (int)response.StatusCode;
        }

        if (string.IsNullOrWhiteSpace(problem.Title))
        {
            problem.Title = response.ReasonPhrase ?? response.StatusCode.ToString();
        }

        if (string.IsNullOrWhiteSpace(problem.Type) ||
            string.Equals(problem.Type, ProblemConstants.Types.AboutBlank, StringComparison.Ordinal))
        {
            problem.Type = response.StatusCode.ToString();
        }

        return problem;
    }

    private static bool IsWhitespace(ReadOnlySpan<byte> body)
    {
        foreach (var value in body)
        {
            if (value != (byte)' ' && value != (byte)'\t' && value != (byte)'\r' && value != (byte)'\n')
            {
                return false;
            }
        }

        return true;
    }

    private static Problem Limited(HttpStatusCode statusCode)
    {
        return CqrsStreamProblems.FailureBodyLimitExceededFor(statusCode);
    }

    private static Problem SafeFailure(HttpStatusCode statusCode, Problem? parsedProblem = null)
    {
        return parsedProblem ?? CqrsStreamProblems.NonSuccess(statusCode);
    }
}

using System;
using System.Buffers;
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
    private const int InitialBufferSize = 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static async Task<Problem> ReadAsync(
        HttpResponseMessage response,
        CqrsStreamClientOptions options,
        CancellationToken cancellationToken)
    {
        var length = response.Content.Headers.ContentLength;
        var maximumBytes = options.MaximumFailureBodyBytes;
        if (length > maximumBytes)
        {
            return Limited(response.StatusCode);
        }

        var readLimit = maximumBytes + DetectionBytes;
        var initialSize = length is { } declaredLength
            ? (int)declaredLength + DetectionBytes
            : Math.Min(readLimit, InitialBufferSize);
        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(initialSize);
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var count = 0;
            while (count < readLimit)
            {
                if (count == buffer.Length)
                {
                    var expanded = pool.Rent(Math.Min(readLimit, buffer.Length * 2));
                    buffer.AsSpan(0, count).CopyTo(expanded);
                    pool.Return(buffer, clearArray: true);
                    buffer = expanded;
                }

                var read = await stream.ReadAsync(
                        buffer.AsMemory(count, Math.Min(readLimit, buffer.Length) - count), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                count += read;
            }

            if (count > maximumBytes)
            {
                return Limited(response.StatusCode);
            }

            var body = buffer.AsSpan(0, count);
            try
            {
                _ = StrictUtf8.GetCharCount(body);
            }
            catch (DecoderFallbackException exception)
            {
                CommunicationDiagnostics.ReportFailure(CommunicationLogger.GetLogger(), null, exception);
                return SafeFailure(response.StatusCode);
            }

            return SafeFailure(response.StatusCode, ParseProblem(response, body, options));
        }
        finally
        {
            pool.Return(buffer, clearArray: true);
        }
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

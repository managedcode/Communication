using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Telemetry;

namespace ManagedCode.Communication;

/// <summary>
///     Turns an <c>HttpResponseMessage</c> into a <c>Result</c>.
/// </summary>
public static class HttpResponseExtension
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    ///     Reads a <see cref="Result{T}" /> from a JSON response, turning a non-success status or an unreadable
    ///     body into a failed result rather than an exception.
    /// </summary>
    public static async Task<Result<T>> FromJsonToResult<T>(
        this HttpResponseMessage responseMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(responseMessage);

        if (!responseMessage.IsSuccessStatusCode)
        {
            return Result<T>.Fail(await ReadProblemAsync(responseMessage, cancellationToken).ConfigureAwait(false));
        }

        try
        {
            var stream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (IsResultEnvelope(document.RootElement))
            {
                return InvalidResponseBody<T>(responseMessage,
                    new JsonException(
                        "Serialized Result<T> envelopes are not accepted. Successful responses must contain the raw JSON payload."));
            }

            var value = document.RootElement.Deserialize<T>(ResponseJsonOptions);
            return value is null
                ? InvalidResponseBody<T>(responseMessage, new JsonException("The response payload was null."))
                : Result<T>.Succeed(value);
        }
        catch (JsonException exception)
        {
            return InvalidResponseBody<T>(responseMessage, exception);
        }
        catch (NotSupportedException exception)
        {
            return InvalidResponseBody<T>(responseMessage, exception);
        }
    }

    /// <summary>
    ///     Turns a response into a <see cref="Result" /> based on its status code.
    /// </summary>
    public static async Task<Result> FromRequestToResult(
        this HttpResponseMessage responseMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(responseMessage);

        if (responseMessage.IsSuccessStatusCode)
        {
            return Result.Succeed();
        }

        return Result.Fail(await ReadProblemAsync(responseMessage, cancellationToken).ConfigureAwait(false));
    }

    private static Result<T> InvalidResponseBody<T>(HttpResponseMessage responseMessage, Exception exception)
    {
        var problem = Problem.Create(
            "Invalid response body",
            $"The response could not be read as a raw {typeof(T).Name} payload: {exception.Message}",
            (int)responseMessage.StatusCode);

        CommunicationDiagnostics.ReportFailure(CommunicationLogger.GetLogger(), problem, exception);
        return Result<T>.Fail(problem);
    }

    private static bool IsResultEnvelope(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var hasSuccess = false;
        var hasPayloadOrProblem = false;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, CommunicationJsonNames.IsSuccess, StringComparison.OrdinalIgnoreCase))
            {
                hasSuccess = property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False;
            }
            else if (string.Equals(property.Name, CommunicationJsonNames.Value, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(property.Name, CommunicationJsonNames.Problem, StringComparison.OrdinalIgnoreCase))
            {
                hasPayloadOrProblem = true;
            }
        }

        return hasSuccess && hasPayloadOrProblem;
    }

    private static async Task<Problem> ReadProblemAsync(
        HttpResponseMessage responseMessage,
        CancellationToken cancellationToken)
    {
        var content = await ReadBodyAsync(responseMessage, cancellationToken).ConfigureAwait(false);

        if (LooksLikeJsonProblem(responseMessage, content))
        {
            try
            {
                var problem = JsonSerializer.Deserialize<Problem>(content, ResponseJsonOptions);
                if (problem is not null)
                {
                    // The HTTP response is authoritative if a proxy or remote producer supplied a mismatching status member.
                    problem.StatusCode = (int)responseMessage.StatusCode;
                    return problem;
                }
            }
            catch (JsonException exception)
            {
                var parseProblem = Problem.Create(
                    "Invalid problem response",
                    "The non-success response declared a JSON problem body that could not be parsed.",
                    (int)responseMessage.StatusCode);
                CommunicationDiagnostics.ReportFailure(CommunicationLogger.GetLogger(), parseProblem, exception);
            }
        }

        return Problem.Create(content, content, responseMessage.StatusCode);
    }

    private static bool LooksLikeJsonProblem(HttpResponseMessage responseMessage, string content)
    {
        var mediaType = responseMessage.Content.Headers.ContentType?.MediaType;
        return (!string.IsNullOrWhiteSpace(mediaType)
                && (mediaType.Equals("application/problem+json", StringComparison.OrdinalIgnoreCase)
                    || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)))
               || content.AsSpan().TrimStart().StartsWith("{".AsSpan(), StringComparison.Ordinal);
    }

    private static async Task<string> ReadBodyAsync(HttpResponseMessage responseMessage, CancellationToken cancellationToken)
    {
        var content = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        // An error body can be an entire HTML page; keep the Problem readable instead of carrying megabytes
        // of markup as a title.
        return content.Length <= MaxErrorBodyLength
            ? content
            : content[..MaxErrorBodyLength] + "…";
    }

    private const int MaxErrorBodyLength = 2048;
}

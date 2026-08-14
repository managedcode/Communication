using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public static class HttpResponseExtension
{
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
            var content = await ReadBodyAsync(responseMessage, cancellationToken).ConfigureAwait(false);
            return Result<T>.Fail(content, content, responseMessage.StatusCode);
        }

        try
        {
            // DeserializeAsync, not the synchronous overload: the latter blocks a thread-pool thread on
            // network I/O for the whole body.
            var stream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<Result<T>>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            // A success status with a body we cannot read is still a failure for the caller; surfacing it as a
            // Result keeps this method consistent with the rest of the library.
            return Result<T>.Fail(
                Problem.Create(
                    "Invalid response body",
                    $"The response could not be read as a {typeof(Result<T>).Name}: {exception.Message}",
                    (int)responseMessage.StatusCode));
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

        var content = await ReadBodyAsync(responseMessage, cancellationToken).ConfigureAwait(false);
        return Result.Fail(content, content, responseMessage.StatusCode);
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
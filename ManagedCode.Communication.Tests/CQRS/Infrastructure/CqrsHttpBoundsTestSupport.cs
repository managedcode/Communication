using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace ManagedCode.Communication.Tests.CQRS;

internal static class CqrsHttpBoundsTestSupport
{
    internal const string StreamPath = "/bounded";
    internal const string HealthyPath = "/healthy";
    internal const string EventStreamContentType = "text/event-stream";
    internal const string PlainTextContentType = "text/plain";
    private const string DataPrefixText = "data: ";
    private const string InvalidJsonText = "{\"broken\":";
    internal const int CleanupTimeoutSeconds = 10;

    public static byte[] Payload()
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            CqrsTestStreams.Completed(sequence: 1),
            CqrsStreamSerialization.Default);
    }

    public static byte[] NonTerminalPayload()
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            CqrsTestStreams.Started(sequence: 1),
            CqrsStreamSerialization.Default);
    }

    public static byte[] IncompleteDataLine()
    {
        return Encoding.UTF8.GetBytes(DataPrefixText);
    }

    public static byte[] InvalidJson()
    {
        return Encoding.UTF8.GetBytes(InvalidJsonText);
    }

    public static Task<WebApplication> StartAsync(Func<HttpContext, Task> endpoint, bool addHealthyRoute = false)
    {
        return CqrsTestHost.StartMinimalApiAsync(app =>
        {
            app.MapGet(StreamPath, endpoint);
            if (addHealthyRoute)
            {
                app.MapGet(HealthyPath, () => CqrsTestStreams.CompletedAsync()).WithCommunicationCqrsResults();
            }
        });
    }

    public static async Task WriteBytesAsync(HttpContext context, byte[] bytes, bool splitEveryByte = false)
    {
        context.Response.ContentType = EventStreamContentType;
        var token = context.RequestAborted;
        if (!splitEveryByte)
        {
            await context.Response.Body.WriteAsync(bytes, token).ConfigureAwait(false);
            await context.Response.Body.FlushAsync(token).ConfigureAwait(false);
            return;
        }

        for (var index = 0; index < bytes.Length; index++)
        {
            await context.Response.Body.WriteAsync(bytes.AsMemory(index, 1), token).ConfigureAwait(false);
            await context.Response.Body.FlushAsync(token).ConfigureAwait(false);
        }
    }

    public static async Task<List<Chunk>> ReadAsync(
        HttpClient client,
        string path,
        CqrsStreamClientOptions options,
        CancellationToken cancellationToken = default)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var read = ReadCoreAsync(client, path, options, lifetime.Token);
        List<Chunk>? result = null;
        Exception? primaryFailure = null;
        var cleanupFailures = new List<Exception>();
        try
        {
            result = await read.WaitAsync(TimeSpan.FromSeconds(CleanupTimeoutSeconds)).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
        }
        finally
        {
            try
            {
                cleanupFailures.AddRange(await CqrsHttpBoundsReadSettlement.CancelAndJoinReadAsync(
                    lifetime,
                    read,
                    primaryFailure).ConfigureAwait(false));
            }
            catch (Exception exception)
            {
                CqrsHttpBoundsFailure.AddDistinct(cleanupFailures, exception, primaryFailure);
            }
        }

        CqrsHttpBoundsFailure.Throw(primaryFailure, cleanupFailures);
        return result!;
    }

    public static async Task WaitForAbortAsync(HttpContext context, TaskCompletionSource<bool> aborted)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            aborted.TrySetResult(true);
        }
    }

    public static byte[] NewLine(byte value)
    {
        return [value];
    }

    public static byte[] CrLf { get; } = [(byte)'\r', (byte)'\n'];

    private static async Task<List<Chunk>> ReadCoreAsync(
        HttpClient client,
        string path,
        CqrsStreamClientOptions options,
        CancellationToken cancellationToken)
    {
        var chunks = new List<Chunk>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(path, options, cancellationToken)
                           .ConfigureAwait(false))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }
}

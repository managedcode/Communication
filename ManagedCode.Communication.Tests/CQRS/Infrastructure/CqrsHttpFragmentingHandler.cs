using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Tests.CQRS;

internal sealed class CqrsHttpFragmentingHandler(
    HttpMessageHandler innerHandler,
    List<CqrsReadObservation> observations) : DelegatingHandler(innerHandler)
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        try
        {
            var originalContent = response.Content;
            var source = await originalContent.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            response.Content = new CqrsObservedHttpContent(originalContent, source, observations);
            return response;
        }
        catch (Exception primaryFailure)
        {
            try
            {
                response.Dispose();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(primaryFailure, cleanupFailure);
            }

            ExceptionDispatchInfo.Capture(primaryFailure).Throw();
            throw;
        }
    }
}

internal readonly record struct CqrsReadObservation(int Ordinal, byte Value);

internal sealed class CqrsObservedHttpContent : HttpContent
{
    private readonly HttpContent _originalContent;
    private readonly Stream _source;
    private readonly CqrsOneByteReadStream _observedStream;
    private readonly long? _contentLength;

    public CqrsObservedHttpContent(
        HttpContent originalContent,
        Stream source,
        List<CqrsReadObservation> observations)
    {
        _originalContent = originalContent;
        _source = source;
        _observedStream = new CqrsOneByteReadStream(source, observations);
        _contentLength = originalContent.Headers.ContentLength;
        foreach (var header in originalContent.Headers)
        {
            Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return _source.CopyToAsync(stream);
    }

    protected override Task<Stream> CreateContentReadStreamAsync()
    {
        return Task.FromResult<Stream>(_observedStream);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _contentLength.GetValueOrDefault();
        return _contentLength is not null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _observedStream.Dispose();
            }
            finally
            {
                _originalContent.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}

internal sealed class CqrsOneByteReadStream(Stream source, List<CqrsReadObservation> observations) : Stream
{
    private int _ordinal;

    public override bool CanRead => source.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return Read(buffer.AsSpan(offset, count));
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override int Read(Span<byte> buffer)
    {
        var read = buffer.IsEmpty ? 0 : source.Read(buffer[..1]);
        RecordRead(buffer, read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = buffer.IsEmpty ? 0 : await source.ReadAsync(buffer[..1], cancellationToken).ConfigureAwait(false);
        RecordRead(buffer.Span, read);
        return read;
    }

    private void RecordRead(ReadOnlySpan<byte> buffer, int read)
    {
        if (read == 1)
        {
            observations.Add(new CqrsReadObservation(++_ordinal, buffer[0]));
        }
    }

    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

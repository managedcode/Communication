using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Counts physical wire bytes while forwarding them directly to the BCL SSE parser.
/// </summary>
internal sealed class CqrsResponseByteLimitStream : Stream
{
    private const int ReadBufferSize = 8 * 1024;

    private readonly Stream _source;
    private readonly CqrsResponseByteAdmission _admission;
    private bool _disposed;

    public CqrsResponseByteLimitStream(Stream source, CqrsStreamClientOptions options)
    {
        _source = source;
        _admission = new CqrsResponseByteAdmission(options);
    }

    public override bool CanRead => !_disposed;
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

    public override int Read(Span<byte> buffer)
    {
        ThrowIfUnavailable();
        if (buffer.IsEmpty)
        {
            return 0;
        }

        var readLimit = _admission.GetReadLimit(buffer.Length, ReadBufferSize);
        var read = _source.Read(buffer[..readLimit]);
        return _admission.Admit(buffer[..read]);
    }

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return await ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        if (buffer.IsEmpty)
        {
            return 0;
        }

        var readLimit = _admission.GetReadLimit(buffer.Length, ReadBufferSize);
        var read = await _source.ReadAsync(buffer[..readLimit], cancellationToken).ConfigureAwait(false);
        return _admission.Admit(buffer.Span[..read]);
    }

    private void ThrowIfUnavailable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        _disposed = true;
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

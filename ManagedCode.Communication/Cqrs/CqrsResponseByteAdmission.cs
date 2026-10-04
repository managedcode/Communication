using System;
using System.IO;

namespace ManagedCode.Communication.CQRS;

internal enum CqrsResponseLimitKind
{
    Frame,
    Total
}

internal sealed class CqrsResponseLimitExceededException(CqrsResponseLimitKind kind) : IOException
{
    public CqrsResponseLimitKind Kind { get; } = kind;
}

internal sealed class CqrsResponseByteAdmission
{
    private readonly int _maximumFrameBytes;
    private readonly long? _maximumStreamBytes;
    private int _frameBytes;
    private long _streamBytes;
    private bool _lineHasBytes;
    private bool _carriageReturnPending;
    private CqrsResponseLimitKind? _pendingFailure;

    public CqrsResponseByteAdmission(CqrsStreamClientOptions options)
    {
        _maximumFrameBytes = options.MaximumFrameBytes;
        _maximumStreamBytes = options.MaximumStreamBytes;
    }

    public int GetReadLimit(int destinationCapacity, int readBufferSize)
    {
        ThrowPendingFailure();
        var readLimit = Math.Min(Math.Min(destinationCapacity, readBufferSize), _maximumFrameBytes - _frameBytes + 1);
        if (_maximumStreamBytes is { } maximumStreamBytes)
        {
            var remainingStreamBytes = maximumStreamBytes - _streamBytes;
            if (remainingStreamBytes < readLimit)
            {
                readLimit = checked((int)remainingStreamBytes + 1);
            }
        }

        return Math.Max(readLimit, 1);
    }

    public int Admit(ReadOnlySpan<byte> input)
    {
        var accepted = 0;
        foreach (var value in input)
        {
            PrepareForByte(value);
            if (WouldExceed(out var kind))
            {
                _pendingFailure = kind;
                break;
            }

            Count(value);
            accepted++;
        }

        if (accepted > 0)
        {
            return accepted;
        }

        ThrowPendingFailure();
        return 0;
    }

    private bool WouldExceed(out CqrsResponseLimitKind kind)
    {
        if (_frameBytes >= _maximumFrameBytes)
        {
            kind = CqrsResponseLimitKind.Frame;
            return true;
        }

        if (_maximumStreamBytes is { } maximumStreamBytes && _streamBytes >= maximumStreamBytes)
        {
            kind = CqrsResponseLimitKind.Total;
            return true;
        }

        kind = default;
        return false;
    }

    private void Count(byte value)
    {
        _frameBytes++;
        if (_maximumStreamBytes is not null)
        {
            _streamBytes++;
        }

        if (_carriageReturnPending)
        {
            CompleteLine();
            _carriageReturnPending = false;
            return;
        }

        if (value == (byte)'\r')
        {
            _carriageReturnPending = true;
        }
        else if (value == (byte)'\n')
        {
            CompleteLine();
        }
        else
        {
            _lineHasBytes = true;
        }
    }

    private void PrepareForByte(byte value)
    {
        if (_carriageReturnPending && value != (byte)'\n')
        {
            CompleteLine();
            _carriageReturnPending = false;
        }
    }

    private void CompleteLine()
    {
        if (!_lineHasBytes)
        {
            _frameBytes = 0;
        }

        _lineHasBytes = false;
    }

    private void ThrowPendingFailure()
    {
        if (_pendingFailure is { } kind)
        {
            throw new CqrsResponseLimitExceededException(kind);
        }
    }
}

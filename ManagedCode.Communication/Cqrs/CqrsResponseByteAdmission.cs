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
        while (accepted < input.Length)
        {
            var value = input[accepted];
            PrepareForByte(value);
            if (WouldExceed(out var kind))
            {
                _pendingFailure = kind;
                break;
            }

            if (value is (byte)'\r' or (byte)'\n')
            {
                CountDelimiter(value);
                accepted++;
                continue;
            }

            var available = Math.Min(input.Length - accepted, _maximumFrameBytes - _frameBytes);
            if (_maximumStreamBytes is { } maximumStreamBytes)
            {
                available = (int)Math.Min(available, maximumStreamBytes - _streamBytes);
            }

            var delimiter = input.Slice(accepted, available).IndexOfAny((byte)'\r', (byte)'\n');
            var dataBytes = delimiter < 0 ? available : delimiter;
            _frameBytes += dataBytes;
            if (_maximumStreamBytes is not null)
            {
                _streamBytes += dataBytes;
            }

            _lineHasBytes = true;
            accepted += dataBytes;
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

    private void CountDelimiter(byte value)
    {
        _frameBytes++;
        if (_maximumStreamBytes is not null)
        {
            _streamBytes++;
        }

        if (value == (byte)'\r')
        {
            _carriageReturnPending = true;
        }
        else
        {
            CompleteLine();
            _carriageReturnPending = false;
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

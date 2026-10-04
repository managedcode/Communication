using System;
using System.Text;

namespace ManagedCode.Communication.Tests.CQRS;

internal static class CqrsHttpBoundsWireFactory
{
    private const string DataPrefixText = "data: ";
    private const string CommentText = ": heartbeat";
    private static readonly byte[] DataPrefix = Encoding.UTF8.GetBytes(DataPrefixText);
    private static readonly byte[] Comment = Encoding.UTF8.GetBytes(CommentText);

    public static byte[] Frame(
        byte[] payload,
        byte[] newline,
        bool includeBom = false,
        bool includeComment = false,
        bool multiline = false)
    {
        var splitAt = GetSplitAt(payload, multiline);
        var result = new byte[MeasureFrame(payload.Length, newline.Length, includeBom, includeComment, multiline)];
        var offset = WritePrefix(result, newline, includeBom, includeComment);
        offset = WriteDataLines(result, payload, newline, splitAt, multiline, offset);
        newline.CopyTo(result.AsSpan(offset));
        return result;
    }

    private static int GetSplitAt(byte[] payload, bool multiline)
    {
        var splitAt = multiline ? Array.IndexOf(payload, (byte)',') + 1 : payload.Length;
        if (splitAt <= 0)
        {
            throw new ArgumentException(nameof(payload));
        }

        return splitAt;
    }

    private static int MeasureFrame(int payloadLength, int newlineLength, bool includeBom, bool includeComment, bool multiline)
    {
        var lineCount = multiline ? 2 : 1;
        var bomLength = includeBom ? 3 : 0;
        var commentLength = includeComment ? Comment.Length + newlineLength : 0;
        var prefixLength = DataPrefix.Length * lineCount;
        var newlineCount = lineCount + 1;
        return checked(bomLength + commentLength + prefixLength + payloadLength + newlineLength * newlineCount);
    }

    private static int WritePrefix(byte[] result, byte[] newline, bool includeBom, bool includeComment)
    {
        var offset = 0;
        if (includeBom)
        {
            result[offset++] = 0xEF;
            result[offset++] = 0xBB;
            result[offset++] = 0xBF;
        }

        if (includeComment)
        {
            Comment.CopyTo(result.AsSpan(offset));
            offset += Comment.Length;
            newline.CopyTo(result.AsSpan(offset));
            offset += newline.Length;
        }

        return offset;
    }

    private static int WriteDataLines(
        byte[] result,
        byte[] payload,
        byte[] newline,
        int splitAt,
        bool multiline,
        int offset)
    {
        DataPrefix.CopyTo(result.AsSpan(offset));
        offset += DataPrefix.Length;
        payload.AsSpan(0, splitAt).CopyTo(result.AsSpan(offset));
        offset += splitAt;
        newline.CopyTo(result.AsSpan(offset));
        offset += newline.Length;
        if (!multiline)
        {
            return offset;
        }

        DataPrefix.CopyTo(result.AsSpan(offset));
        offset += DataPrefix.Length;
        payload.AsSpan(splitAt).CopyTo(result.AsSpan(offset));
        offset += payload.Length - splitAt;
        newline.CopyTo(result.AsSpan(offset));
        return offset + newline.Length;
    }

    public static byte[] FrameWithoutFinalDelimiter(byte[] payload, byte[] newline)
    {
        var result = new byte[DataPrefix.Length + payload.Length + newline.Length];
        DataPrefix.CopyTo(result);
        payload.CopyTo(result.AsSpan(DataPrefix.Length));
        newline.CopyTo(result.AsSpan(DataPrefix.Length + payload.Length));
        return result;
    }

    public static byte[] Heartbeat(byte[] newline)
    {
        var result = new byte[Comment.Length + newline.Length * 2];
        Comment.CopyTo(result);
        newline.CopyTo(result.AsSpan(Comment.Length));
        newline.CopyTo(result.AsSpan(Comment.Length + newline.Length));
        return result;
    }

    public static byte[] Join(byte[] first, byte[] second)
    {
        var result = new byte[checked(first.Length + second.Length)];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }
}

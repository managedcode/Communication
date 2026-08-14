using System;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Marks an exception that a stream stage raised deliberately and that must reach the caller as-is, instead of
///     being converted into a terminal <see cref="CqrsStreamChunkKind.Failed" /> chunk by
///     <see cref="CqrsStreamNormalizer" />.
/// </summary>
/// <remarks>
///     Without this, an opt-in policy such as <c>CqrsMalformedChunkBehavior.Throw</c> would be silently undone by the
///     very normalization that exists to hide accidental faults.
/// </remarks>
internal sealed class CqrsStreamPassthroughException : Exception
{
    public CqrsStreamPassthroughException(Exception inner)
        : base(inner.Message, inner)
    {
    }

    public Exception Inner => InnerException!;
}

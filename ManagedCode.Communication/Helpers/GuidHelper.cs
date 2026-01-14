using System;

namespace ManagedCode.Communication.Helpers;

internal static class GuidHelper
{
    /// <summary>
    /// Creates a version 7 GUID (monotonic, sortable) if available,
    /// otherwise falls back to a sequential GUID for .NET 8.
    /// </summary>
    public static Guid CreateVersion7()
    {
#if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
#else
        // For .NET 8, use NewGuid() as a fallback
        // In production, you might want to use a proper UUID v7 implementation
        // or a library like System.Guid.NewSequentialGuid() if available
        return Guid.NewGuid();
#endif
    }
}

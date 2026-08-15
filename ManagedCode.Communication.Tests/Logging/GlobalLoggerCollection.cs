using Xunit;

namespace ManagedCode.Communication.Tests.Logging;

/// <summary>
///     Serializes every test that touches the process-wide logger.
/// </summary>
/// <remarks>
///     <see cref="ManagedCode.Communication.Logging.CommunicationLogger" /> is deliberately a static singleton —
///     it is configured once at application startup. That makes any test which configures it, or asserts on what
///     it resolves to, incompatible with a test running concurrently that does the same. Marking one class
///     <c>DisableParallelization</c> is not enough: xUnit only serializes *within* a collection, so the classes
///     have to share this one.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GlobalLoggerCollection
{
    public const string Name = "global-logger";
}

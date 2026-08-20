namespace ManagedCode.Communication.Commands.Execution;

/// <summary>
///     Determines how the delay grows between command retries.
/// </summary>
public enum RetryBackoffType
{
    /// <summary>Uses the configured delay for every retry.</summary>
    Constant,

    /// <summary>Multiplies the configured delay by the retry number.</summary>
    Linear,

    /// <summary>Doubles the configured delay after every retry.</summary>
    Exponential
}

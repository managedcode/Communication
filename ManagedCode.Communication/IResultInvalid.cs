using System;

namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a result that contains invalid data.
/// </summary>
public interface IResultInvalid
{
    /// <summary>
    ///     Gets a value indicating whether the result is invalid.
    /// </summary>
    /// <value>true if the result is invalid; otherwise, false.</value>
    bool IsInvalid { get; }


}

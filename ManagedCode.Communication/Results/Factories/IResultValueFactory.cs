using System;

namespace ManagedCode.Communication.Results;

/// <summary>
///     The factory surface for results that carry a value.
/// </summary>
public partial interface IResultValueFactory<TSelf, TValue>
    where TSelf : struct, IResultValueFactory<TSelf, TValue>
{
    /// <summary>
    ///     Creates a success carrying the value.
    /// </summary>
    static abstract TSelf Succeed(TValue value);

    /// <summary>
    ///     Creates a success from a value produced by the factory.
    /// </summary>
    static virtual TSelf Succeed(Func<TValue> valueFactory)
    {
        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        return TSelf.Succeed(valueFactory());
    }
}

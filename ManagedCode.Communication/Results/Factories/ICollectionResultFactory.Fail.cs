using System.Collections.Generic;
using System.Linq;
using ManagedCode.Communication;

namespace ManagedCode.Communication.Results;

public partial interface ICollectionResultFactory<TSelf, TValue>
    where TSelf : struct, ICollectionResultFactory<TSelf, TValue>
{
    /// <summary>
    ///     Creates a failure that still carries items.
    /// </summary>
    static virtual TSelf Fail(TValue[] items)
    {
        return TSelf.Fail(Problem.GenericError(), items);
    }

    /// <summary>
    ///     Creates a failure that still carries items.
    /// </summary>
    static virtual TSelf Fail(IEnumerable<TValue> items)
    {
        return TSelf.Fail(Problem.GenericError(), items as TValue[] ?? items.ToArray());
    }

    /// <summary>
    ///     Creates a failure carrying both a problem and items.
    /// </summary>
    static virtual TSelf Fail(Problem problem, IEnumerable<TValue> items)
    {
        return TSelf.Fail(problem, items as TValue[] ?? items.ToArray());
    }

    /// <summary>
    ///     Creates a failure carrying both a problem and items.
    /// </summary>
    static abstract TSelf Fail(Problem problem, TValue[] items);
}

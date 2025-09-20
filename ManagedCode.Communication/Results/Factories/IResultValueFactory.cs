using System;

namespace ManagedCode.Communication.Results;

public partial interface IResultValueFactory<TSelf, TValue>
    where TSelf : struct, IResultValueFactory<TSelf, TValue>
{
    static abstract TSelf Succeed(TValue value);

    static virtual TSelf Succeed(Func<TValue> valueFactory)
    {
        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        return TSelf.Succeed(valueFactory());
    }
}

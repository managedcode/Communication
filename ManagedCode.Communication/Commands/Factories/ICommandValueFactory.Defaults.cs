using System;

namespace ManagedCode.Communication.Commands;

public partial interface ICommandValueFactory<TSelf, TValue>
    where TSelf : class, ICommandValueFactory<TSelf, TValue>
{
    static virtual TSelf Create(TValue value)
    {
        var commandType = ResolveCommandType(value);
        return TSelf.Create(Guid.CreateVersion7(), commandType, value);
    }

    static virtual TSelf Create(Guid commandId, TValue value)
    {
        var commandType = ResolveCommandType(value);
        return TSelf.Create(commandId, commandType, value);
    }

    static virtual TSelf Create(Guid commandId, string commandType, Func<TValue> valueFactory)
    {
        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        return TSelf.Create(commandId, commandType, valueFactory());
    }

    static virtual TSelf Create(Func<TValue> valueFactory)
    {
        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        return TSelf.Create(valueFactory());
    }

    static virtual TSelf From(TValue value)
    {
        return TSelf.Create(value);
    }

    static virtual TSelf From(Guid commandId, TValue value)
    {
        return TSelf.Create(commandId, value);
    }

    static virtual TSelf From(Guid commandId, string commandType, TValue value)
    {
        return TSelf.Create(commandId, commandType, value);
    }

    private static string ResolveCommandType(TValue value)
    {
        return value?.GetType().Name ?? typeof(TValue).Name;
    }
}

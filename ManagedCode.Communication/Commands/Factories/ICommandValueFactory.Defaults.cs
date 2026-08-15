using System;

namespace ManagedCode.Communication.Commands;

public partial interface ICommandValueFactory<TSelf, TValue>
    where TSelf : class, ICommandValueFactory<TSelf, TValue>
{
    /// <summary>
    ///     Creates a command carrying <paramref name="value" />, naming it after the payload's runtime type.
    /// </summary>
    /// <inheritdoc cref="Create(string,TValue,Guid?)" />
    static virtual TSelf Create(TValue value, Guid? commandId = null)
    {
        return TSelf.Create(ResolveCommandType(value), value, commandId);
    }

    /// <inheritdoc cref="Create(string,TValue,Guid?)" />
    static virtual TSelf Create(string commandType, Func<TValue> valueFactory, Guid? commandId = null)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        return TSelf.Create(commandType, valueFactory(), commandId);
    }

    /// <inheritdoc cref="Create(TValue,Guid?)" />
    static virtual TSelf Create(Func<TValue> valueFactory, Guid? commandId = null)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        return TSelf.Create(valueFactory(), commandId);
    }

    /// <inheritdoc cref="Create(TValue,Guid?)" />
    static virtual TSelf From(TValue value, Guid? commandId = null)
    {
        return TSelf.Create(value, commandId);
    }

    /// <inheritdoc cref="Create(string,TValue,Guid?)" />
    static virtual TSelf From(string commandType, TValue value, Guid? commandId = null)
    {
        return TSelf.Create(commandType, value, commandId);
    }

    private static string ResolveCommandType(TValue value)
    {
        return value?.GetType().Name ?? typeof(TValue).Name;
    }
}

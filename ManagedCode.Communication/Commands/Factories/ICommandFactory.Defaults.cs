using System;

namespace ManagedCode.Communication.Commands;

public partial interface ICommandFactory<TSelf>
    where TSelf : class, ICommandFactory<TSelf>
{
    /// <inheritdoc cref="Create(string,Guid?)" />
    static virtual TSelf Create<TEnum>(TEnum commandType, Guid? commandId = null)
        where TEnum : Enum
    {
        ArgumentNullException.ThrowIfNull(commandType);

        return TSelf.Create(commandType.ToString(), commandId);
    }

    /// <inheritdoc cref="Create(string,Guid?)" />
    static virtual TSelf From(string commandType, Guid? commandId = null)
    {
        return TSelf.Create(commandType, commandId);
    }

    /// <inheritdoc cref="Create(string,Guid?)" />
    static virtual TSelf From<TEnum>(TEnum commandType, Guid? commandId = null)
        where TEnum : Enum
    {
        return TSelf.Create(commandType, commandId);
    }

    /// <summary>
    ///     The identity a command gets when the caller does not supply one: a time-ordered UUIDv7, so ids sort by
    ///     creation time and index well.
    /// </summary>
    protected static Guid NewCommandId()
    {
        return Guid.CreateVersion7();
    }
}

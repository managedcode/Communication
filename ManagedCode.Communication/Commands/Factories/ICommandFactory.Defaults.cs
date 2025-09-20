using System;

namespace ManagedCode.Communication.Commands;

public partial interface ICommandFactory<TSelf>
    where TSelf : class, ICommandFactory<TSelf>
{
    static virtual TSelf Create(string commandType)
    {
        if (string.IsNullOrWhiteSpace(commandType))
        {
            throw new ArgumentException("Command type must be provided.", nameof(commandType));
        }

        return TSelf.Create(Guid.CreateVersion7(), commandType);
    }

    static virtual TSelf Create<TEnum>(TEnum commandType)
        where TEnum : Enum
    {
        return TSelf.Create(Guid.CreateVersion7(), commandType.ToString());
    }

    static virtual TSelf Create<TEnum>(Guid commandId, TEnum commandType)
        where TEnum : Enum
    {
        return TSelf.Create(commandId, commandType.ToString());
    }

    static virtual TSelf From(string commandType)
    {
        return TSelf.Create(commandType);
    }

    static virtual TSelf From(Guid commandId, string commandType)
    {
        return TSelf.Create(commandId, commandType);
    }

    static virtual TSelf From<TEnum>(TEnum commandType)
        where TEnum : Enum
    {
        return TSelf.Create(commandType);
    }

    static virtual TSelf From<TEnum>(Guid commandId, TEnum commandType)
        where TEnum : Enum
    {
        return TSelf.Create(commandId, commandType);
    }
}

using System;

namespace ManagedCode.Communication.Commands;

public partial class Command<T>
{
    public static Command<T> Create(T value)
    {
        return CommandValueFactoryBridge.Create<Command<T>, T>(value);
    }

    /// <summary>
    /// Creates a command with a specific identifier using the provided value.
    /// </summary>
    public static Command<T> Create(Guid id, T value)
    {
        return CommandValueFactoryBridge.Create<Command<T>, T>(id, value);
    }

    public static Command<T> Create(Guid id, string commandType, T value)
    {
        if (string.IsNullOrWhiteSpace(commandType))
        {
            throw new ArgumentException("Command type must be provided.", nameof(commandType));
        }

        return new Command<T>(id, commandType, value);
    }

    public static Command<T> Create(Guid id, string commandType, Func<T> valueFactory)
    {
        return CommandValueFactoryBridge.Create<Command<T>, T>(id, commandType, valueFactory);
    }

    public static Command<T> Create(Func<T> valueFactory)
    {
        return CommandValueFactoryBridge.Create<Command<T>, T>(valueFactory);
    }

    // Legacy From methods for backward compatibility
    public static Command<T> From(Guid id, T value)
    {
        return CommandValueFactoryBridge.From<Command<T>, T>(id, value);
    }

    public static Command<T> From(T value)
    {
        return CommandValueFactoryBridge.From<Command<T>, T>(value);
    }

    public static Command<T> From(Guid id, string commandType, T value)
    {
        return CommandValueFactoryBridge.From<Command<T>, T>(id, commandType, value);
    }
}

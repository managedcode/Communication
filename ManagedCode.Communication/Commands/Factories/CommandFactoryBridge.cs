using System;

namespace ManagedCode.Communication.Commands;

/// <summary>
/// Lightweight façade that allows concrete command types to reuse factory defaults without repeating boilerplate.
/// </summary>
internal static class CommandFactoryBridge
{
    public static TSelf Create<TSelf>(string commandType)
        where TSelf : class, ICommandFactory<TSelf>
    {
        return TSelf.Create(Guid.CreateVersion7(), commandType);
    }

    public static TSelf Create<TSelf>(Guid commandId, string commandType)
        where TSelf : class, ICommandFactory<TSelf>
    {
        return TSelf.Create(commandId, commandType);
    }

    public static TSelf Create<TSelf, TEnum>(TEnum commandType)
        where TSelf : class, ICommandFactory<TSelf>
        where TEnum : Enum
    {
        return TSelf.Create(Guid.CreateVersion7(), commandType.ToString());
    }

    public static TSelf Create<TSelf, TEnum>(Guid commandId, TEnum commandType)
        where TSelf : class, ICommandFactory<TSelf>
        where TEnum : Enum
    {
        return TSelf.Create(commandId, commandType.ToString());
    }

    public static TSelf From<TSelf>(string commandType)
        where TSelf : class, ICommandFactory<TSelf>
    {
        return Create<TSelf>(commandType);
    }

    public static TSelf From<TSelf>(Guid commandId, string commandType)
        where TSelf : class, ICommandFactory<TSelf>
    {
        return Create<TSelf>(commandId, commandType);
    }

    public static TSelf From<TSelf, TEnum>(TEnum commandType)
        where TSelf : class, ICommandFactory<TSelf>
        where TEnum : Enum
    {
        return Create<TSelf, TEnum>(commandType);
    }

    public static TSelf From<TSelf, TEnum>(Guid commandId, TEnum commandType)
        where TSelf : class, ICommandFactory<TSelf>
        where TEnum : Enum
    {
        return Create<TSelf, TEnum>(commandId, commandType);
    }
}

/// <summary>
/// Helper methods for invoking <see cref="ICommandValueFactory{TSelf, TValue}"/> static interface members from concrete command types.
/// </summary>
internal static class CommandValueFactoryBridge
{
    public static TSelf Create<TSelf, TValue>(TValue value)
        where TSelf : class, ICommandValueFactory<TSelf, TValue>
    {
        return TSelf.Create(Guid.CreateVersion7(), ResolveCommandType(value), value);
    }

    public static TSelf Create<TSelf, TValue>(Guid commandId, TValue value)
        where TSelf : class, ICommandValueFactory<TSelf, TValue>
    {
        return TSelf.Create(commandId, ResolveCommandType(value), value);
    }

    public static TSelf Create<TSelf, TValue>(Guid commandId, string commandType, TValue value)
        where TSelf : class, ICommandValueFactory<TSelf, TValue>
    {
        return TSelf.Create(commandId, commandType, value);
    }

    public static TSelf Create<TSelf, TValue>(Guid commandId, string commandType, Func<TValue> valueFactory)
        where TSelf : class, ICommandValueFactory<TSelf, TValue>
    {
        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        return TSelf.Create(commandId, commandType, valueFactory());
    }

    public static TSelf Create<TSelf, TValue>(Func<TValue> valueFactory)
        where TSelf : class, ICommandValueFactory<TSelf, TValue>
    {
        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        return Create<TSelf, TValue>(valueFactory());
    }

    public static TSelf From<TSelf, TValue>(TValue value)
        where TSelf : class, ICommandValueFactory<TSelf, TValue>
    {
        return Create<TSelf, TValue>(value);
    }

    public static TSelf From<TSelf, TValue>(Guid commandId, TValue value)
        where TSelf : class, ICommandValueFactory<TSelf, TValue>
    {
        return Create<TSelf, TValue>(commandId, value);
    }

    public static TSelf From<TSelf, TValue>(Guid commandId, string commandType, TValue value)
        where TSelf : class, ICommandValueFactory<TSelf, TValue>
    {
        return TSelf.Create(commandId, commandType, value);
    }

    private static string ResolveCommandType<TValue>(TValue value)
    {
        return value?.GetType().Name ?? typeof(TValue).Name;
    }
}

using System;

namespace ManagedCode.Communication.Commands;

public partial class Command
{
    public static Command<T> From<T>(Guid id, T value)
    {
        return Command<T>.From(id, value);
    }

    public static Command<T> From<T>(T value)
    {
        return Command<T>.From(value);
    }

    public static Command<T> From<T>(Guid id, string commandType, T value)
    {
        return Command<T>.From(id, commandType, value);
    }
}

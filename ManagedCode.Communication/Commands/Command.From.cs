using System;

namespace ManagedCode.Communication.Commands;

public partial class Command
{
    public static Command<T> From<T>(Guid id, T value)
    {
        return Command<T>.Create(id, value);
    }

    public static Command<T> From<T>(T value)
    {
        return Command<T>.Create(value);
    }
}
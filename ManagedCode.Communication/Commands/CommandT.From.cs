using System;

namespace ManagedCode.Communication.Commands;

public partial class Command<T>
{
    public static Command<T> From(string id, T value)
    {
        return new Command<T>(id, value);
    }

    public static Command<T> From(T value)
    {
        return new Command<T>(Guid.NewGuid()
            .ToString("N"), value);
    }
}
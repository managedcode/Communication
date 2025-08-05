using System;

namespace ManagedCode.Communication.Commands;

public partial class Command
{
    public static Command<T> From<T>(string id, T value)
    {
        return new Command<T>(id, value);
    }

    public static Command<T> From<T>(T value)
    {
        return new Command<T>(Guid.NewGuid()
            .ToString("N"), value);
    }
}
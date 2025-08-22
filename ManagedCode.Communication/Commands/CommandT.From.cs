using System;

namespace ManagedCode.Communication.Commands;

public partial class Command<T>
{
    public static Command<T> Create(T value)
    {
        return new Command<T>(Guid.NewGuid(), value);
    }
    
    public static Command<T> Create(Guid id, T value)
    {
        return new Command<T>(id, value);
    }
    
    public static Command<T> Create(Guid id, string commandType, T value)
    {
        return new Command<T>(id, commandType, value);
    }
    
    // Legacy From methods for backward compatibility
    public static Command<T> From(Guid id, T value)
    {
        return Create(id, value);
    }

    public static Command<T> From(T value)
    {
        return Create(value);
    }
}
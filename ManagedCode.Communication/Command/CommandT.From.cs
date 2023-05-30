using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

public partial struct Command<T>
{
    public static Command<T> From(string id, T value)
    {
        return new Command<T>(id ,value);
    }
    public static Command<T> From(T value)
    {
        return new Command<T>(default,value);
    }
}
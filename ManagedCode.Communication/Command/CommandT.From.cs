using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

public partial class Command<T>
{
    public static Command<T> From(string id, T value)
    {
        return new Command<T>(id ,value);
    }
    public static Command<T> From(T value)
    {
        return new Command<T>(Guid.NewGuid().ToString("N"),value);
    }
}
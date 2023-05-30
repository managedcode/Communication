using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

public partial struct Command
{
    public static Command<T> From<T>(string id, T value)
    {
        return new Command<T>(id ,value);
    }
    
    public static Command<T> From<T>(T value)
    {
        return new Command<T>(default,value);
    }
}
using System;

namespace ManagedCode.Communication;

public class Error
{
    public Error(string message)
    {
        Message = message;
    }

    public Error(Exception? exception)
    {
        Exception = exception;
        Message = exception.Message;
    }

    public Exception? Exception { get; set; }
    public string Message { get; set; }
}
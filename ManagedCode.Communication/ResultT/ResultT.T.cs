using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace ManagedCode.Communication.ZALIPA.Result;

public partial class Result<T> : Result
{
    public Task<Result<T>> AsTask()
    {
        return Task.FromResult(this);
    }
    
    
#if NET6_0_OR_GREATER
    
    public ValueTask<Result<T>> AsValueTask()
    {
        return ValueTask.FromResult(this);
    }

    
#endif

    public static implicit operator Result<T>(T value)
    {
        return new Result<T>(value);
    }

    public static implicit operator Result<T>(Error error)
    {
        return new Result<T>(error);
    }

    public static implicit operator Result<T>(List<Error> errors)
    {
        return new Result<T>(errors);
    }

    public static implicit operator Result<T>(Exception? exception)
    {
        return new Result<T>(Error.FromException(exception));
    }

    public static Result<T> Succeed(T value)
    {
        return new Result<T>(value);
    }
    
    protected Result(T value) : base(true)
    {
        Value = value;
    }

    protected Result(bool isSuccess) : base(isSuccess)
    {
    }

    protected Result(bool isSuccess, T value) : base(isSuccess)
    {
        Value = value;
    }

    protected Result(Error error) : base(error)
    {
    }

    protected Result(Error error, T value) : base(error)
    {
        Value = value;
    }

    protected Result(List<Error> errors) : base(errors)
    {
    }

    protected Result(List<Error> errors, T value) : base(errors)
    {
        Value = value;
    }

    protected Result(bool isSuccess, List<Error> errors, T value) : base(isSuccess, errors)
    {
        if (isSuccess && value is null)
        {
            throw new InvalidOperationException($"{nameof(Value)} value cannot be null if the result is successful");
        }

        Value = value;
    }

    public T? Value { get; }

    public T? ValueOrDefault(T? defaultValue = default)
    {
        return Value ?? defaultValue;
    }
    
    public static bool operator== (Result<T> obj1, bool obj2)
    {
        return obj1.IsSuccess == obj2;
    }
    
    public static bool operator!= (Result<T> obj1, bool obj2)
    {
        return obj1.IsSuccess != obj2;
    }
}
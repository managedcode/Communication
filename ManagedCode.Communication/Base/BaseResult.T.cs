using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Communication;

public abstract class BaseResult<T, TErrorCode> : BaseResult<TErrorCode> where TErrorCode : Enum
{
    protected BaseResult(T value) : base(true)
    {
        Value = value;
    }

    protected BaseResult(bool isSuccess) : base(isSuccess)
    {
    }

    protected BaseResult(bool isSuccess, T value) : base(isSuccess)
    {
        Value = value;
    }

    protected BaseResult(Error<TErrorCode> error) : base(error)
    {
    }

    protected BaseResult(Error<TErrorCode> error, T value) : base(error)
    {
        Value = value;
    }

    protected BaseResult(List<Error<TErrorCode>> errors) : base(errors)
    {
    }

    protected BaseResult(List<Error<TErrorCode>> errors, T value) : base(errors)
    {
        Value = value;
    }

    protected BaseResult(bool isSuccess, List<Error<TErrorCode>> errors, T value) : base(isSuccess, errors)
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
    
    public static bool operator== (BaseResult<T, TErrorCode> obj1, bool obj2)
    {
        return obj1.IsSuccess == obj2;
    }
    
    public static bool operator!= (BaseResult<T, TErrorCode> obj1, bool obj2)
    {
        return obj1.IsSuccess != obj2;
    }
}
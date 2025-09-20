using System;
using System.Collections.Generic;
using ManagedCode.Communication.CollectionResultT;

namespace ManagedCode.Communication.CollectionResults.Extensions;

public static partial class CollectionResultExecutionExtensions
{
    public static CollectionResult<T> ToCollectionResult<T>(this Func<T[]> func)
    {
        return Execute(func, CollectionResult<T>.Succeed);
    }

    public static CollectionResult<T> ToCollectionResult<T>(this Func<IEnumerable<T>> func)
    {
        return Execute(func, CollectionResult<T>.Succeed);
    }

    public static CollectionResult<T> ToCollectionResult<T>(this Func<CollectionResult<T>> func)
    {
        try
        {
            return func();
        }
        catch (Exception exception)
        {
            return CollectionResult<T>.Fail(exception);
        }
    }

    public static Result ToResult<T>(this CollectionResult<T> result)
    {
        return result.IsSuccess ? Result.Succeed() : Result.Fail(result.Problem ?? Problem.GenericError());
    }

    private static CollectionResult<T> Execute<T, TValue>(Func<TValue> func, Func<TValue, CollectionResult<T>> projector)
    {
        try
        {
            var value = func();
            return projector(value);
        }
        catch (Exception exception)
        {
            return CollectionResult<T>.Fail(exception);
        }
    }
}

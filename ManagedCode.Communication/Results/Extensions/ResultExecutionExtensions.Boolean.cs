using System;

namespace ManagedCode.Communication.Results.Extensions;

public static partial class ResultExecutionExtensions
{
    public static Result ToResult(this bool condition)
    {
        return condition ? Result.Succeed() : Result.Fail();
    }

    public static Result ToResult(this bool condition, Problem problem)
    {
        return condition ? Result.Succeed() : Result.Fail(problem);
    }

    public static Result ToResult(this Func<bool> predicate)
    {
        return predicate() ? Result.Succeed() : Result.Fail();
    }

    public static Result ToResult(this Func<bool> predicate, Problem problem)
    {
        return predicate() ? Result.Succeed() : Result.Fail(problem);
    }
}

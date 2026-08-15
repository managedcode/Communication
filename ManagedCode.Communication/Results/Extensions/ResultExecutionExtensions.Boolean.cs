using System;

namespace ManagedCode.Communication.Results.Extensions;

public static partial class ResultExecutionExtensions
{
    /// <summary>
    ///     Succeeds when the condition holds, otherwise fails with the generic problem.
    /// </summary>
    public static Result ToResult(this bool condition)
    {
        return condition ? Result.Succeed() : Result.Fail();
    }

    /// <summary>
    ///     Succeeds when the condition holds, otherwise fails with the given problem.
    /// </summary>
    public static Result ToResult(this bool condition, Problem problem)
    {
        return condition ? Result.Succeed() : Result.Fail(problem);
    }

    /// <summary>
    ///     Evaluates the predicate; succeeds when it holds, otherwise fails with the generic problem.
    /// </summary>
    public static Result ToResult(this Func<bool> predicate)
    {
        return predicate() ? Result.Succeed() : Result.Fail();
    }

    /// <summary>
    ///     Evaluates the predicate; succeeds when it holds, otherwise fails with the given problem.
    /// </summary>
    public static Result ToResult(this Func<bool> predicate, Problem problem)
    {
        return predicate() ? Result.Succeed() : Result.Fail(problem);
    }
}

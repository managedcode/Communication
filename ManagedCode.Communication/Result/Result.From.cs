using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Runs the delegate and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static Result From(Action action)
    {
        return action.ToResult();
    }

    /// <summary>
    ///     Runs the delegate and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static Result From(Func<Result> func)
    {
        return func.ToResult();
    }

    /// <summary>
    ///     Awaits the operation and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result> From(Task task)
    {
        return await task.ToResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns the result unchanged; present so conversions compose uniformly.
    /// </summary>
    public static Result From(Result result)
    {
        return result;
    }

    /// <summary>
    ///     Discards the value and keeps only success or failure.
    /// </summary>
    public static Result From<T>(Result<T> result)
    {
        return result.ToResult();
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result> From(Func<Task> task, CancellationToken cancellationToken = default)
    {
        return await task.ToResultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the operation and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<Result> From(ValueTask valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result> From(Func<ValueTask> valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Succeeds when the condition holds, otherwise fails with the generic problem.
    /// </summary>
    public static Result From(bool condition)
    {
        return condition.ToResult();
    }

    /// <summary>
    ///     Succeeds when the condition holds, otherwise fails with the given problem.
    /// </summary>
    public static Result From(bool condition, Problem problem)
    {
        return condition.ToResult(problem);
    }

    /// <summary>
    ///     Evaluates the predicate; succeeds when it holds, otherwise fails with the generic problem.
    /// </summary>
    public static Result From(Func<bool> condition)
    {
        return condition.ToResult();
    }

    /// <summary>
    ///     Evaluates the predicate; succeeds when it holds, otherwise fails with the given problem.
    /// </summary>
    public static Result From(Func<bool> condition, Problem problem)
    {
        return condition.ToResult(problem);
}
}

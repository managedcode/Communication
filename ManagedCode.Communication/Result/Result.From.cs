using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result From(Action action)
    {
        return action.ToResult();
    }

    public static Result From(Func<Result> func)
    {
        return func.ToResult();
    }

    public static async Task<Result> From(Task task)
    {
        return await task.ToResultAsync().ConfigureAwait(false);
    }

    public static Result From(Result result)
    {
        return result;
    }

    public static Result From<T>(Result<T> result)
    {
        return result.ToResult();
    }

    public static async Task<Result> From(Func<Task> task, CancellationToken cancellationToken = default)
    {
        return await task.ToResultAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<Result> From(ValueTask valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }

    public static async Task<Result> From(Func<ValueTask> valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }

    public static Result From(bool condition)
    {
        return condition.ToResult();
    }

    public static Result From(bool condition, Problem problem)
    {
        return condition.ToResult(problem);
    }

    public static Result From(Func<bool> condition)
    {
        return condition.ToResult();
    }

    public static Result From(Func<bool> condition, Problem problem)
    {
        return condition.ToResult(problem);
}
}

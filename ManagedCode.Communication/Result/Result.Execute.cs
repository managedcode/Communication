using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial class Result
{
    public static Result Execute(Action action)
    {
        try
        {
            action();
            return Succeed();
        }
        catch (Exception e)
        {
            return new Result(Error.FromException(e));
        }
    }

    public static Result Execute(Func<Result> func)
    {
        try
        {
            return func();
        }
        catch (Exception e)
        {
            return new Result(Error.FromException(e));
        }
    }

    public static async Task<Result> Execute(Task task, CancellationToken cancellationToken = default)
    {
        if (task.IsCompleted)
        {
            return Succeed();
        }

        if (task.IsCanceled || task.IsFaulted)
        {
            return new Result(Error.FromException(task.Exception));
        }

        try
        {
            await task;
            return Succeed();
        }
        catch (Exception e)
        {
            return new Result(Error.FromException(e));
        }
    }

    public static async Task<Result> Execute(Func<Task<Result>> task, CancellationToken cancellationToken = default)
    {
        try
        {
            return await task();
        }
        catch (Exception e)
        {
            return new Result(Error.FromException(e));
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result From(Action action)
    {
        try
        {
            action();
            return Succeed();
        }
        catch (Exception e)
        {
            return new Result(ManagedCode.Communication.Error.FromException(e));
        }
    }
    
    public static Result From(Func<Result> func)
    {
        try
        {
            return func();
        }
        catch (Exception e)
        {
            return new Result(ManagedCode.Communication.Error.FromException(e));
        }
    }
    
    public static async Task<Result> From(Task task)
    {
        try
        {
            if (task.IsCompleted)
            {
                return Succeed();
            }

            if (task.IsCanceled || task.IsFaulted)
            {
                return new Result(ManagedCode.Communication.Error.FromException(task.Exception));
            }
            
            await task;
            return Succeed();
        }
        catch (Exception e)
        {
            return new Result(ManagedCode.Communication.Error.FromException(e));
        }
    }
    
    public static async Task<Result> From(Func<Task> task, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run(task, cancellationToken);
            return Succeed();
        }
        catch (Exception e)
        {
            return new Result(ManagedCode.Communication.Error.FromException(e));
        }
    }
    
#if NET6_0_OR_GREATER

    public static async ValueTask<Result> From(ValueTask valueTask)
    {
        try
        {
            if (valueTask.IsCompleted)
            {
                return Succeed();
            }

            if (valueTask.IsCanceled || valueTask.IsFaulted)
            {
                return Result.Fail();
            }
            
            await valueTask;
            return Succeed();
        }
        catch (Exception e)
        {
            return new Result(ManagedCode.Communication.Error.FromException(e));
        }
    }
    
    public static async Task<Result> From(Func<ValueTask> valueTask)
    {
        try
        {
            await valueTask();
            return Succeed();
        }
        catch (Exception e)
        {
            return new Result(ManagedCode.Communication.Error.FromException(e));
        }
    }

#endif

}
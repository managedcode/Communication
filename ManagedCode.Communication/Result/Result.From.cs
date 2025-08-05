using System;
using System.Net;
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
            return Fail(e);
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
            return Fail(e);
        }
    }

    public static async Task<Result> From(Task task)
    {
        try
        {
            if (task.IsCompleted)
                return Succeed();

            if (task.IsCanceled)
                return Fail(new TaskCanceledException());
            
            if (task.IsFaulted && task.Exception != null)
                return Fail(task.Exception);

            await task;
            return Succeed();
        }
        catch (Exception e)
        {
            return Fail(e);
        }
    }

    public static Result From(Result result)
    {
        return result;
    }

    public static Result From<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Succeed();

        return result.Problem != null ? Fail(result.Problem) : Fail("Operation failed", null, HttpStatusCode.InternalServerError);
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
            return Fail(e);
        }
    }

    public static async ValueTask<Result> From(ValueTask valueTask)
    {
        try
        {
            if (valueTask.IsCompleted)
                return Succeed();

            if (valueTask.IsCanceled || valueTask.IsFaulted)
                return Fail();

            await valueTask;
            return Succeed();
        }
        catch (Exception e)
        {
            return Fail(e);
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
            return Fail(e);
        }
    }
    
    public static Result From(bool condition)
    {
        return condition ? Succeed() : Fail();
    }
    
    public static Result From(bool condition, Problem problem)
    {
        return condition ? Succeed() : Fail(problem);
    }
    
    public static Result From(Func<bool> condition)
    {
        return condition() ? Succeed() : Fail();
    }
    
    public static Result From(Func<bool> condition, Problem problem)
    {
        return condition() ? Succeed() : Fail(problem);
    }
}
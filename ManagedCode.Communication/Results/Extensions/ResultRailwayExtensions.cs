using System;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication.Results.Extensions;

/// <summary>
///     Railway-oriented helpers for <see cref="Result"/> and <see cref="Result{T}"/>.
/// </summary>
public static class ResultRailwayExtensions
{
    private static Result<TOut> PropagateFailure<TOut>(this IResult result)
    {
        return result.TryGetProblem(out var problem)
            ? Result<TOut>.Fail(problem)
            : Result<TOut>.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
    }

    private static Result PropagateFailure(this IResult result)
    {
        return result.TryGetProblem(out var problem)
            ? Result.Fail(problem)
            : Result.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
    }

    public static Result Bind(this Result result, Func<Result> next)
    {
        return result.IsSuccess ? next() : result;
    }

    public static Result<T> Bind<T>(this Result result, Func<Result<T>> next)
    {
        return result.IsSuccess ? next() : result.PropagateFailure<T>();
    }

    public static Result Tap(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    public static Result Finally(this Result result, Action<Result> action)
    {
        action(result);
        return result;
    }

    public static Result Else(this Result result, Func<Result> alternative)
    {
        return result.IsSuccess ? result : alternative();
    }

    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
    {
        return result.IsSuccess
            ? Result<TOut>.Succeed(mapper(result.Value))
            : result.PropagateFailure<TOut>();
    }

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder)
    {
        return result.IsSuccess
            ? binder(result.Value)
            : result.PropagateFailure<TOut>();
    }

    public static Result Bind<T>(this Result<T> result, Func<T, Result> binder)
    {
        return result.IsSuccess
            ? binder(result.Value)
            : result.PropagateFailure();
    }

    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Problem problem)
    {
        if (result.IsSuccess && !predicate(result.Value))
        {
            return Result<T>.Fail(problem);
        }

        return result;
    }

    public static Result<T> Else<T>(this Result<T> result, Func<Result<T>> alternative)
    {
        return result.IsSuccess ? result : alternative();
    }

    public static Result<T> Finally<T>(this Result<T> result, Action<Result<T>> action)
    {
        action(result);
        return result;
    }

    public static async Task<Result> BindAsync(this Task<Result> resultTask, Func<Task<Result>> next)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? await next().ConfigureAwait(false) : result;
    }

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? await binder(result.Value).ConfigureAwait(false)
            : result.PropagateFailure<TOut>();
    }

    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<TOut>> mapper)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? Result<TOut>.Succeed(await mapper(result.Value).ConfigureAwait(false))
            : result.PropagateFailure<TOut>();
    }

    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action(result.Value).ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> binder)
    {
        return result.IsSuccess
            ? await binder(result.Value).ConfigureAwait(false)
            : result.PropagateFailure<TOut>();
    }

    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<TOut>> mapper)
    {
        return result.IsSuccess
            ? Result<TOut>.Succeed(await mapper(result.Value).ConfigureAwait(false))
            : result.PropagateFailure<TOut>();
    }

    public static async Task<Result> BindAsync(this Result result, Func<Task<Result>> next)
    {
        return result.IsSuccess ? await next().ConfigureAwait(false) : result;
    }

    #region Pattern Matching Helpers

    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<Problem, TOut> onFailure)
    {
        if (result.IsSuccess)
        {
            return onSuccess();
        }

        var problem = result.TryGetProblem(out var extracted) ? extracted : Problem.GenericError();
        return onFailure(problem);
    }

    public static TOut Match<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<Problem, TOut> onFailure)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        var problem = result.TryGetProblem(out var extracted) ? extracted : Problem.GenericError();
        return onFailure(problem);
    }

    public static void Match(this Result result, Action onSuccess, Action<Problem> onFailure)
    {
        if (result.IsSuccess)
        {
            onSuccess();
        }
        else
        {
            var problem = result.TryGetProblem(out var extracted) ? extracted : Problem.GenericError();
            onFailure(problem);
        }
    }

    public static void Match<T>(this Result<T> result, Action<T> onSuccess, Action<Problem> onFailure)
    {
        if (result.IsSuccess)
        {
            onSuccess(result.Value);
        }
        else
        {
            var problem = result.TryGetProblem(out var extracted) ? extracted : Problem.GenericError();
            onFailure(problem);
        }
    }

    #endregion
}

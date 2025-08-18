using System;
using System.Threading.Tasks;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.Extensions;

/// <summary>
///     Extension methods for railway-oriented programming with Result types.
/// </summary>
public static class ResultRailwayExtensions
{
    #region Result Extensions

    /// <summary>
    ///     Executes the next function if the result is successful (railway-oriented programming).
    /// </summary>
    public static Result Bind(this Result result, Func<Result> next)
    {
        return result.IsSuccess ? next() : result;
    }

    /// <summary>
    ///     Executes the next function if the result is successful, transforming to Result<T>.
    /// </summary>
    public static Result<T> Bind<T>(this Result result, Func<Result<T>> next)
    {
        if (result.IsSuccess)
            return next();
            
        return result.TryGetProblem(out var problem)
            ? Result<T>.Fail(problem)
            : Result<T>.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
    }

    /// <summary>
    ///     Executes a side effect if the result is successful, returning the original result.
    /// </summary>
    public static Result Tap(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    /// <summary>
    ///     Executes a function regardless of success/failure, useful for cleanup.
    /// </summary>
    public static Result Finally(this Result result, Action<Result> action)
    {
        action(result);
        return result;
    }

    /// <summary>
    ///     Provides an alternative result if the current one is failed.
    /// </summary>
    public static Result Else(this Result result, Func<Result> alternative)
    {
        return result.IsSuccess ? result : alternative();
    }

    #endregion

    #region Result<T> Extensions

    /// <summary>
    ///     Transforms the value if successful (functor map).
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
    {
        if (result.IsSuccess)
            return Result<TOut>.Succeed(mapper(result.Value));
            
        return result.TryGetProblem(out var problem)
            ? Result<TOut>.Fail(problem)
            : Result<TOut>.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
    }

    /// <summary>
    ///     Chains Result-returning operations (monadic bind).
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder)
    {
        if (result.IsSuccess)
            return binder(result.Value);
            
        return result.TryGetProblem(out var problem)
            ? Result<TOut>.Fail(problem)
            : Result<TOut>.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
    }

    /// <summary>
    ///     Chains Result-returning operations without value transformation.
    /// </summary>
    public static Result Bind<T>(this Result<T> result, Func<T, Result> binder)
    {
        if (result.IsSuccess)
            return binder(result.Value);
            
        return result.TryGetProblem(out var problem)
            ? Result.Fail(problem)
            : Result.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
    }

    /// <summary>
    ///     Executes a side effect if successful, returning the original result.
    /// </summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    ///     Validates the value and potentially fails the result.
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Problem problem)
    {
        if (result.IsSuccess && !predicate(result.Value))
        {
            return Result<T>.Fail(problem);
        }

        return result;
    }

    /// <summary>
    ///     Provides an alternative value if the result is failed.
    /// </summary>
    public static Result<T> Else<T>(this Result<T> result, Func<Result<T>> alternative)
    {
        return result.IsSuccess ? result : alternative();
    }

    /// <summary>
    ///     Executes a function regardless of success/failure.
    /// </summary>
    public static Result<T> Finally<T>(this Result<T> result, Action<Result<T>> action)
    {
        action(result);
        return result;
    }

    #endregion

    #region Async Extensions

    /// <summary>
    ///     Async version of Bind for Result.
    /// </summary>
    public static async Task<Result> BindAsync(this Task<Result> resultTask, Func<Task<Result>> next)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? await next()
                .ConfigureAwait(false)
            : result;
    }

    /// <summary>
    ///     Async version of Bind for Result<T>.
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return await binder(result.Value).ConfigureAwait(false);
            
        return result.TryGetProblem(out var problem)
            ? Result<TOut>.Fail(problem)
            : Result<TOut>.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
    }

    /// <summary>
    ///     Async version of Map for Result<T>.
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<TOut>> mapper)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            return Result<TOut>.Succeed(await mapper(result.Value).ConfigureAwait(false));
            
        return result.TryGetProblem(out var problem)
            ? Result<TOut>.Fail(problem)
            : Result<TOut>.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
    }

    /// <summary>
    ///     Async version of Tap for Result<T>.
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action(result.Value)
                .ConfigureAwait(false);
        }

        return result;
    }

    #endregion

    #region Pattern Matching Helpers

    /// <summary>
    ///     Pattern matching helper for Result.
    /// </summary>
    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<Problem, TOut> onFailure)
    {
        if (result.IsSuccess)
            return onSuccess();
        
        var problem = result.TryGetProblem(out var p) ? p : Problem.GenericError();
        return onFailure(problem);
    }

    /// <summary>
    ///     Pattern matching helper for Result<T>.
    /// </summary>
    public static TOut Match<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<Problem, TOut> onFailure)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value);
        
        var problem = result.TryGetProblem(out var p) ? p : Problem.GenericError();
        return onFailure(problem);
    }

    /// <summary>
    ///     Pattern matching helper with side effects.
    /// </summary>
    public static void Match(this Result result, Action onSuccess, Action<Problem> onFailure)
    {
        if (result.IsSuccess)
        {
            onSuccess();
        }
        else
        {
            var problem = result.TryGetProblem(out var p) ? p : Problem.GenericError();
            onFailure(problem);
        }
    }

    /// <summary>
    ///     Pattern matching helper with side effects for Result<T>.
    /// </summary>
    public static void Match<T>(this Result<T> result, Action<T> onSuccess, Action<Problem> onFailure)
    {
        if (result.IsSuccess)
        {
            onSuccess(result.Value);
        }
        else
        {
            var problem = result.TryGetProblem(out var p) ? p : Problem.GenericError();
            onFailure(problem);
        }
    }

    #endregion
}
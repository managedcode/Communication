using System;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

/// <summary>
/// Extension methods for railway-oriented programming with Result types.
/// </summary>
public static class ResultExtensions
{
    #region Result Extensions
    
    /// <summary>
    /// Executes the next function if the result is successful (railway-oriented programming).
    /// </summary>
    public static Result Bind(this Result result, Func<Result> next)
    {
        return result.IsSuccess ? next() : result;
    }
    
    /// <summary>
    /// Executes the next function if the result is successful, transforming to Result<T>.
    /// </summary>
    public static Result<T> Bind<T>(this Result result, Func<Result<T>> next)
    {
        return result.IsSuccess ? next() : Result<T>.Fail(result.Problem!);
    }
    
    /// <summary>
    /// Executes a side effect if the result is successful, returning the original result.
    /// </summary>
    public static Result Tap(this Result result, Action action)
    {
        if (result.IsSuccess)
            action();
        return result;
    }
    
    /// <summary>
    /// Executes a function regardless of success/failure, useful for cleanup.
    /// </summary>
    public static Result Finally(this Result result, Action<Result> action)
    {
        action(result);
        return result;
    }
    
    /// <summary>
    /// Provides an alternative result if the current one is failed.
    /// </summary>
    public static Result Else(this Result result, Func<Result> alternative)
    {
        return result.IsSuccess ? result : alternative();
    }
    
    #endregion
    
    #region Result<T> Extensions
    
    /// <summary>
    /// Transforms the value if successful (functor map).
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
    {
        return result.IsSuccess 
            ? Result<TOut>.Succeed(mapper(result.Value)) 
            : Result<TOut>.Fail(result.Problem!);
    }
    
    /// <summary>
    /// Chains Result-returning operations (monadic bind).
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder)
    {
        return result.IsSuccess ? binder(result.Value) : Result<TOut>.Fail(result.Problem!);
    }
    
    /// <summary>
    /// Chains Result-returning operations without value transformation.
    /// </summary>
    public static Result Bind<T>(this Result<T> result, Func<T, Result> binder)
    {
        return result.IsSuccess ? binder(result.Value) : Result.Fail(result.Problem!);
    }
    
    /// <summary>
    /// Executes a side effect if successful, returning the original result.
    /// </summary>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
        return result;
    }
    
    /// <summary>
    /// Validates the value and potentially fails the result.
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Problem problem)
    {
        if (result.IsSuccess && !predicate(result.Value))
            return Result<T>.Fail(problem);
        return result;
    }
    
    /// <summary>
    /// Provides an alternative value if the result is failed.
    /// </summary>
    public static Result<T> Else<T>(this Result<T> result, Func<Result<T>> alternative)
    {
        return result.IsSuccess ? result : alternative();
    }
    
    /// <summary>
    /// Executes a function regardless of success/failure.
    /// </summary>
    public static Result<T> Finally<T>(this Result<T> result, Action<Result<T>> action)
    {
        action(result);
        return result;
    }
    
    #endregion
    
    #region Async Extensions
    
    /// <summary>
    /// Async version of Bind for Result.
    /// </summary>
    public static async Task<Result> BindAsync(this Task<Result> resultTask, Func<Task<Result>> next)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? await next().ConfigureAwait(false) : result;
    }
    
    /// <summary>
    /// Async version of Bind for Result<T>.
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<Result<TOut>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess 
            ? await binder(result.Value).ConfigureAwait(false)
            : Result<TOut>.Fail(result.Problem!);
    }
    
    /// <summary>
    /// Async version of Map for Result<T>.
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<TOut>> mapper)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess 
            ? Result<TOut>.Succeed(await mapper(result.Value).ConfigureAwait(false))
            : Result<TOut>.Fail(result.Problem!);
    }
    
    /// <summary>
    /// Async version of Tap for Result<T>.
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
            await action(result.Value).ConfigureAwait(false);
        return result;
    }
    
    #endregion
    
    #region Pattern Matching Helpers
    
    /// <summary>
    /// Pattern matching helper for Result.
    /// </summary>
    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<Problem, TOut> onFailure)
    {
        return result.IsSuccess ? onSuccess() : onFailure(result.Problem!);
    }
    
    /// <summary>
    /// Pattern matching helper for Result<T>.
    /// </summary>
    public static TOut Match<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<Problem, TOut> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Problem!);
    }
    
    /// <summary>
    /// Pattern matching helper with side effects.
    /// </summary>
    public static void Match(this Result result, Action onSuccess, Action<Problem> onFailure)
    {
        if (result.IsSuccess)
            onSuccess();
        else
            onFailure(result.Problem!);
    }
    
    /// <summary>
    /// Pattern matching helper with side effects for Result<T>.
    /// </summary>
    public static void Match<T>(this Result<T> result, Action<T> onSuccess, Action<Problem> onFailure)
    {
        if (result.IsSuccess)
            onSuccess(result.Value);
        else
            onFailure(result.Problem!);
    }
    
    #endregion
}
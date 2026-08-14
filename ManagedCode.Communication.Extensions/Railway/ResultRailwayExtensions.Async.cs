using System;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Extensions;

/// <summary>
///     Completes the railway matrix for asynchronous chains.
/// </summary>
/// <remarks>
///     Every operator needs a <see cref="Task{TResult}" /> receiver, otherwise a chain has to be broken with an
///     <c>await</c> and a temporary variable the moment it reaches an operator that lacks one. The overloads here
///     fill in the combinations the synchronous file does not cover, so an async pipeline reads the same as a
///     synchronous one from the first step to the last.
/// </remarks>
public static partial class ResultRailwayExtensions
{
    // ---------------------------------------------------------------------------------------------------
    // Task<Result> receivers
    // ---------------------------------------------------------------------------------------------------

    /// <inheritdoc cref="Bind(Result,Func{Result})" />
    public static async Task<Result> ThenAsync(this Task<Result> resultTask, Func<Task<Result>> next)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? await next().ConfigureAwait(false) : result;
    }

    /// <inheritdoc cref="Bind{T}(Result,Func{Result{T}})" />
    public static async Task<Result<T>> BindAsync<T>(this Task<Result> resultTask, Func<Task<Result<T>>> next)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? await next().ConfigureAwait(false) : Result<T>.Fail(result.Problem!);
    }

    /// <inheritdoc cref="Tap(Result,Action)" />
    public static async Task<Result> TapAsync(this Task<Result> resultTask, Func<Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action().ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc cref="Else(Result,Func{Result})" />
    public static async Task<Result> ElseAsync(this Task<Result> resultTask, Func<Task<Result>> alternative)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : await alternative().ConfigureAwait(false);
    }

    /// <inheritdoc cref="Finally(Result,Action{Result})" />
    public static async Task<Result> FinallyAsync(this Task<Result> resultTask, Func<Result, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        await action(result).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc cref="Match{TOut}(Result,Func{TOut},Func{Problem,TOut})" />
    public static async Task<TOut> MatchAsync<TOut>(
        this Task<Result> resultTask,
        Func<TOut> onSuccess,
        Func<Problem, TOut> onFailure)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? onSuccess() : onFailure(result.Problem!);
    }

    // ---------------------------------------------------------------------------------------------------
    // Task<Result<T>> receivers
    // ---------------------------------------------------------------------------------------------------

    /// <inheritdoc cref="Bind{T}(Result{T},Func{T,Result})" />
    public static async Task<Result> BindAsync<T>(this Task<Result<T>> resultTask, Func<T, Task<Result>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? await binder(result.Value!).ConfigureAwait(false) : Result.Fail(result.Problem!);
    }

    /// <inheritdoc cref="Ensure{T}(Result{T},Func{T,bool},Problem)" />
    public static async Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, bool> predicate,
        Problem problem)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Ensure(predicate, problem);
    }

    /// <summary>
    ///     Fails the chain when an asynchronous predicate does not hold.
    /// </summary>
    public static async Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<bool>> predicate,
        Problem problem)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailed)
        {
            return result;
        }

        return await predicate(result.Value!).ConfigureAwait(false) ? result : Result<T>.Fail(problem);
    }

    /// <inheritdoc cref="Do{T}(Result{T},Action{T})" />
    public static async Task<Result<T>> DoAsync<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action(result.Value!).ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc cref="Else{T}(Result{T},Func{Result{T}})" />
    public static async Task<Result<T>> ElseAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Task<Result<T>>> alternative)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : await alternative().ConfigureAwait(false);
    }

    /// <inheritdoc cref="Compensate{T}(Result{T},Func{Problem,Result{T}})" />
    public static async Task<Result<T>> CompensateAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Problem, Task<Result<T>>> recovery)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : await recovery(result.Problem!).ConfigureAwait(false);
    }

    /// <inheritdoc cref="CompensateWith{T}(Result{T},T)" />
    public static async Task<Result<T>> CompensateWithAsync<T>(this Task<Result<T>> resultTask, T defaultValue)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : Result<T>.Succeed(defaultValue);
    }

    /// <inheritdoc cref="Finally{T}(Result{T},Action{Result{T}})" />
    public static async Task<Result<T>> FinallyAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Result<T>, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        await action(result).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc cref="Match{TIn,TOut}(Result{TIn},Func{TIn,TOut},Func{Problem,TOut})" />
    public static async Task<TOut> MatchAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, TOut> onSuccess,
        Func<Problem, TOut> onFailure)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? onSuccess(result.Value!) : onFailure(result.Problem!);
    }

    /// <summary>
    ///     Leaves the railway by handling both branches asynchronously.
    /// </summary>
    public static async Task<TOut> MatchAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<TOut>> onSuccess,
        Func<Problem, Task<TOut>> onFailure)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? await onSuccess(result.Value!).ConfigureAwait(false)
            : await onFailure(result.Problem!).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------------------------------------------
    // Synchronous receivers with an asynchronous continuation
    // ---------------------------------------------------------------------------------------------------

    /// <inheritdoc cref="Tap{T}(Result{T},Action{T})" />
    public static async Task<Result<T>> TapAsync<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess)
        {
            await action(result.Value!).ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc cref="Tap(Result,Action)" />
    public static async Task<Result> TapAsync(this Result result, Func<Task> action)
    {
        if (result.IsSuccess)
        {
            await action().ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc cref="Match{TIn,TOut}(Result{TIn},Func{TIn,TOut},Func{Problem,TOut})" />
    public static async Task<TOut> MatchAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> onSuccess,
        Func<Problem, Task<TOut>> onFailure)
    {
        return result.IsSuccess
            ? await onSuccess(result.Value!).ConfigureAwait(false)
            : await onFailure(result.Problem!).ConfigureAwait(false);
    }

    /// <inheritdoc cref="Else{T}(Result{T},Func{Result{T}})" />
    public static async Task<Result<T>> ElseAsync<T>(this Result<T> result, Func<Task<Result<T>>> alternative)
    {
        return result.IsSuccess ? result : await alternative().ConfigureAwait(false);
    }
}

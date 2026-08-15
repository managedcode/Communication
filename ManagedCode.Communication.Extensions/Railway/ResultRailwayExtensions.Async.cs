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

    /// <summary>
    ///     Awaits the result, then transforms its value with a synchronous mapper.
    /// </summary>
    /// <typeparam name="TIn">The incoming value type.</typeparam>
    /// <typeparam name="TOut">The transformed value type.</typeparam>
    /// <param name="resultTask">The result being awaited.</param>
    /// <param name="mapper">Runs only on success.</param>
    /// <returns>The mapped value, or the incoming failure unchanged.</returns>
    /// <remarks>
    ///     Named <c>Map</c> rather than <c>MapAsync</c> because the suffix here describes the mapper, not the
    ///     receiver: <see cref="MapAsync{TIn,TOut}(Task{Result{TIn}},Func{TIn,Task{TOut}})" /> takes an
    ///     asynchronous one. Giving both the same name would make every call with an async mapper ambiguous,
    ///     since the two would infer to the identical delegate type.
    /// </remarks>
    public static async Task<Result<TOut>> Map<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? Result<TOut>.Succeed(mapper(result.Value))
            : result.PropagateFailure<TOut>();
    }

    /// <summary>
    ///     Awaits the result, then runs a synchronous next step on its value.
    /// </summary>
    /// <typeparam name="TIn">The incoming value type.</typeparam>
    /// <typeparam name="TOut">The value type the step returns.</typeparam>
    /// <param name="resultTask">The result being awaited.</param>
    /// <param name="binder">Runs only on success.</param>
    /// <returns>The step's result, or the incoming failure unchanged.</returns>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? binder(result.Value) : result.PropagateFailure<TOut>();
    }

    /// <summary>
    ///     Awaits the result, then runs a synchronous side effect on success.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The result being awaited.</param>
    /// <param name="action">Runs only on success. Its outcome does not affect the result.</param>
    /// <returns>The result, unchanged.</returns>
    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> resultTask, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <inheritdoc cref="TapAsync{T}(Task{Result{T}},Action{T})" />
    public static Task<Result<T>> DoAsync<T>(this Task<Result<T>> resultTask, Action<T> action)
    {
        return resultTask.TapAsync(action);
    }

    /// <summary>
    ///     Awaits the result, then recovers from a failure synchronously.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The result being awaited.</param>
    /// <param name="recovery">Runs only on failure, and may itself return a failure.</param>
    /// <returns>The original success, or whatever the recovery produced.</returns>
    public static async Task<Result<T>> CompensateAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Problem, Result<T>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : recovery(result.Problem!);
    }

    /// <summary>
    ///     Awaits the result, then substitutes a synchronously produced alternative for a failure.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The result being awaited.</param>
    /// <param name="alternative">Runs only on failure. Unlike a compensation, it is not given the problem.</param>
    /// <returns>The original success, or the alternative.</returns>
    public static async Task<Result<T>> ElseAsync<T>(this Task<Result<T>> resultTask, Func<Result<T>> alternative)
    {
        ArgumentNullException.ThrowIfNull(alternative);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : alternative();
    }

    /// <summary>
    ///     Awaits the result, then runs a synchronous action whatever the outcome.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The result being awaited.</param>
    /// <param name="action">Runs on success and on failure alike.</param>
    /// <returns>The result, unchanged.</returns>
    public static async Task<Result<T>> FinallyAsync<T>(this Task<Result<T>> resultTask, Action<Result<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var result = await resultTask.ConfigureAwait(false);
        action(result);
        return result;
    }

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

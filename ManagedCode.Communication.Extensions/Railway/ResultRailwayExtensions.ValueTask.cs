using System;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Extensions;

/// <summary>
///     Allocation-conscious railway overloads for <see cref="ValueTask{TResult}" /> result receivers.
/// </summary>
public static partial class ResultRailwayExtensions
{
    /// <summary>Awaits a value-task result, then transforms its value synchronously.</summary>
    public static async ValueTask<Result<TOut>> Map<TIn, TOut>(
        this ValueTask<Result<TIn>> resultTask,
        Func<TIn, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? Result<TOut>.Succeed(mapper(result.Value))
            : result.PropagateFailure<TOut>();
    }

    /// <summary>Awaits a value-task result, then runs a synchronous result-producing step.</summary>
    public static async ValueTask<Result<TOut>> BindAsync<TIn, TOut>(
        this ValueTask<Result<TIn>> resultTask,
        Func<TIn, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? binder(result.Value) : result.PropagateFailure<TOut>();
    }

    /// <summary>Alias for the synchronous-step value-task <c>BindAsync</c> overload.</summary>
    public static ValueTask<Result<TOut>> ThenAsync<TIn, TOut>(
        this ValueTask<Result<TIn>> resultTask,
        Func<TIn, Result<TOut>> next)
    {
        return resultTask.BindAsync(next);
    }

    /// <summary>Awaits a value-task result, then runs a synchronous side effect on success.</summary>
    public static async ValueTask<Result<T>> TapAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>Alias for the synchronous-action value-task <c>TapAsync</c> overload.</summary>
    public static ValueTask<Result<T>> DoAsync<T>(this ValueTask<Result<T>> resultTask, Action<T> action)
    {
        return resultTask.TapAsync(action);
    }

    /// <summary>Awaits a value-task result, then recovers from failure synchronously.</summary>
    public static async ValueTask<Result<T>> CompensateAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<Problem, Result<T>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : recovery(result.Problem!);
    }

    /// <summary>Awaits a value-task result, then substitutes a synchronous alternative on failure.</summary>
    public static async ValueTask<Result<T>> ElseAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<Result<T>> alternative)
    {
        ArgumentNullException.ThrowIfNull(alternative);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : alternative();
    }

    /// <summary>Awaits a value-task result, then runs a synchronous action on either outcome.</summary>
    public static async ValueTask<Result<T>> FinallyAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Action<Result<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var result = await resultTask.ConfigureAwait(false);
        action(result);
        return result;
    }

    /// <summary>Awaits a value-task result, then runs an asynchronous next step.</summary>
    public static async ValueTask<Result> ThenAsync(
        this ValueTask<Result> resultTask,
        Func<ValueTask<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? await next().ConfigureAwait(false) : result;
    }

    /// <summary>Awaits a value-task result, then introduces a value asynchronously.</summary>
    public static async ValueTask<Result<T>> BindAsync<T>(
        this ValueTask<Result> resultTask,
        Func<ValueTask<Result<T>>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? await next().ConfigureAwait(false)
            : Result<T>.Fail(result.Problem!);
    }

    /// <summary>Awaits a value-task result, then runs an asynchronous side effect on success.</summary>
    public static async ValueTask<Result> TapAsync(
        this ValueTask<Result> resultTask,
        Func<ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action().ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>Awaits a value-task result, then substitutes an asynchronous alternative on failure.</summary>
    public static async ValueTask<Result> ElseAsync(
        this ValueTask<Result> resultTask,
        Func<ValueTask<Result>> alternative)
    {
        ArgumentNullException.ThrowIfNull(alternative);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : await alternative().ConfigureAwait(false);
    }

    /// <summary>Awaits a value-task result, then runs an asynchronous action on either outcome.</summary>
    public static async ValueTask<Result> FinallyAsync(
        this ValueTask<Result> resultTask,
        Func<Result, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var result = await resultTask.ConfigureAwait(false);
        await action(result).ConfigureAwait(false);
        return result;
    }

    /// <summary>Awaits a value-task result and handles both branches synchronously.</summary>
    public static async ValueTask<TOut> MatchAsync<TOut>(
        this ValueTask<Result> resultTask,
        Func<TOut> onSuccess,
        Func<Problem, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? onSuccess() : onFailure(result.Problem!);
    }

    /// <summary>Awaits a value-task result, then runs an asynchronous result-producing step on its value.</summary>
    public static async ValueTask<Result<TOut>> BindAsync<TIn, TOut>(
        this ValueTask<Result<TIn>> resultTask,
        Func<TIn, ValueTask<Result<TOut>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? await binder(result.Value).ConfigureAwait(false)
            : result.PropagateFailure<TOut>();
    }

    /// <summary>Alias for the asynchronous value-task <c>BindAsync</c> overload.</summary>
    public static ValueTask<Result<TOut>> ThenAsync<TIn, TOut>(
        this ValueTask<Result<TIn>> resultTask,
        Func<TIn, ValueTask<Result<TOut>>> next)
    {
        return resultTask.BindAsync(next);
    }

    /// <summary>Awaits a value-task result, then transforms its value asynchronously.</summary>
    public static async ValueTask<Result<TOut>> MapAsync<TIn, TOut>(
        this ValueTask<Result<TIn>> resultTask,
        Func<TIn, ValueTask<TOut>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? Result<TOut>.Succeed(await mapper(result.Value).ConfigureAwait(false))
            : result.PropagateFailure<TOut>();
    }

    /// <summary>Awaits a value-task result, then runs an asynchronous step that discards its value.</summary>
    public static async ValueTask<Result> BindAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<T, ValueTask<Result>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? await binder(result.Value).ConfigureAwait(false)
            : Result.Fail(result.Problem!);
    }

    /// <summary>Awaits a value-task result and applies a synchronous success predicate.</summary>
    public static async ValueTask<Result<T>> EnsureAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<T, bool> predicate,
        Problem problem)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(problem);

        var result = await resultTask.ConfigureAwait(false);
        return result.Ensure(predicate, problem);
    }

    /// <summary>Awaits a value-task result and applies an asynchronous success predicate.</summary>
    public static async ValueTask<Result<T>> EnsureAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<T, ValueTask<bool>> predicate,
        Problem problem)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(problem);

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailed)
        {
            return result;
        }

        return await predicate(result.Value).ConfigureAwait(false) ? result : Result<T>.Fail(problem);
    }

    /// <summary>Awaits a value-task result, then runs an asynchronous side effect on success.</summary>
    public static async ValueTask<Result<T>> DoAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<T, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action(result.Value).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>Alias for the asynchronous-action value-task <c>DoAsync</c> overload.</summary>
    public static ValueTask<Result<T>> TapAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<T, ValueTask> action)
    {
        return resultTask.DoAsync(action);
    }

    /// <summary>Awaits a value-task result, then substitutes an asynchronous alternative on failure.</summary>
    public static async ValueTask<Result<T>> ElseAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<ValueTask<Result<T>>> alternative)
    {
        ArgumentNullException.ThrowIfNull(alternative);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : await alternative().ConfigureAwait(false);
    }

    /// <summary>Awaits a value-task result, then recovers from failure asynchronously.</summary>
    public static async ValueTask<Result<T>> CompensateAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<Problem, ValueTask<Result<T>>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : await recovery(result.Problem!).ConfigureAwait(false);
    }

    /// <summary>Awaits a value-task result, then replaces a failure with a successful default value.</summary>
    public static async ValueTask<Result<T>> CompensateWithAsync<T>(
        this ValueTask<Result<T>> resultTask,
        T defaultValue)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? result : Result<T>.Succeed(defaultValue);
    }

    /// <summary>Awaits a value-task result, then runs an asynchronous action on either outcome.</summary>
    public static async ValueTask<Result<T>> FinallyAsync<T>(
        this ValueTask<Result<T>> resultTask,
        Func<Result<T>, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var result = await resultTask.ConfigureAwait(false);
        await action(result).ConfigureAwait(false);
        return result;
    }

    /// <summary>Awaits a value-task result and handles both branches synchronously.</summary>
    public static async ValueTask<TOut> MatchAsync<TIn, TOut>(
        this ValueTask<Result<TIn>> resultTask,
        Func<TIn, TOut> onSuccess,
        Func<Problem, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Problem!);
    }

    /// <summary>Awaits a value-task result and handles both branches asynchronously.</summary>
    public static async ValueTask<TOut> MatchAsync<TIn, TOut>(
        this ValueTask<Result<TIn>> resultTask,
        Func<TIn, ValueTask<TOut>> onSuccess,
        Func<Problem, ValueTask<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess
            ? await onSuccess(result.Value).ConfigureAwait(false)
            : await onFailure(result.Problem!).ConfigureAwait(false);
    }
}

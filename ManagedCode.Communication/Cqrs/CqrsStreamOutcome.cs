using System.Collections.Generic;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Everything a finished CQRS stream produced: its terminal result, the progress it reported on the way, and
///     the chunks themselves.
/// </summary>
/// <typeparam name="TProgress">Progress payload type.</typeparam>
/// <typeparam name="TResult">Terminal payload type.</typeparam>
/// <remarks>
///     Produced by <see cref="CqrsStreamExtensions.ToOutcomeAsync{TProgress,TResult}" />, for when the answer
///     alone is not enough — a test asserting on the shape of the stream, an audit log, a UI that replays what
///     happened. When only the answer matters, <c>ToResultAsync</c> is cheaper: it keeps nothing.
/// </remarks>
public sealed record CqrsStreamOutcome<TProgress, TResult>
{
    /// <summary>
    ///     The terminal result. A stream that ended without a terminal chunk yields a failure carrying
    ///     <see cref="CqrsStreamProblems.IncompleteStream" />, so this is never absent.
    /// </summary>
    public required Result<TResult> Result { get; init; }

    /// <summary>
    ///     Every progress payload reported, in order. Empty when the command reported none.
    /// </summary>
    public required IReadOnlyList<TProgress> Progress { get; init; }

    /// <summary>
    ///     Every chunk received, in order, including the terminal one.
    /// </summary>
    public required IReadOnlyList<CqrsStreamChunk<TProgress, TResult>> Chunks { get; init; }

    /// <summary>
    ///     Whether the command succeeded.
    /// </summary>
    public bool IsSuccess => Result.IsSuccess;

    /// <summary>
    ///     Whether the command failed, the stream broke, or it ended without saying how it went.
    /// </summary>
    public bool IsFailed => Result.IsFailed;

    /// <summary>
    ///     The terminal payload on success; <c>default</c> otherwise.
    /// </summary>
    public TResult? Value => Result.Value;

    /// <summary>
    ///     What went wrong, or <c>null</c> on success.
    /// </summary>
    public Problem? Problem => Result.Problem;

    /// <summary>
    ///     Unwraps the outcome to its <see cref="Result" />, so an outcome can be returned wherever a result is
    ///     expected and fed straight into the railway operators.
    /// </summary>
    /// <param name="outcome">The outcome to unwrap.</param>
    public static implicit operator Result<TResult>(CqrsStreamOutcome<TProgress, TResult> outcome)
    {
        return outcome is null ? Result<TResult>.Fail(CqrsStreamProblems.Incomplete()) : outcome.Result;
    }

    /// <summary>
    ///     Named alternative to the implicit conversion to <see cref="Result{TResult}" />.
    /// </summary>
    /// <returns>The terminal result.</returns>
    public Result<TResult> ToResult()
    {
        return Result;
    }
}

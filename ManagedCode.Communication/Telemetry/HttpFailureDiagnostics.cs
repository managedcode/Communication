using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Telemetry;

// HTTP diagnostics must remain private even when tracing is disabled or unsampled.
internal sealed class HttpFailureDiagnostics : IDisposable
{
    private const string SafeTitle = "HTTP dependency failure";
    private const string SafeDetail = "The HTTP dependency operation failed.";
    private const string CauseSeparator = " -> ";
    private const string ExceptionEvent = "exception";
    private const string ExceptionTypeTag = "exception.type";
    private const string ExceptionStackTraceTag = "exception.stacktrace";
    private const string ExceptionCauseTypesTag = "exception.cause_types";
    private static readonly AsyncLocal<bool> Redacted = new();

    internal static bool IsActive => Redacted.Value;

    internal static IDisposable? Begin(bool redact)
    {
        if (!redact || IsActive)
        {
            return null;
        }
        Redacted.Value = true;
        return new HttpFailureDiagnostics();
    }

    public void Dispose() => Redacted.Value = false;

    [return: NotNullIfNotNull(nameof(problem))]
    internal static Problem? Project(Problem? problem) => IsActive && problem is not null
        ? Problem.Create(SafeTitle, SafeDetail, problem.StatusCode)
        : problem;

    internal static (string? ExceptionType, string? CauseTypes, string? StackTrace) Describe(Exception? exception)
    {
        if (exception is null)
        {
            return default;
        }
        var causes = new List<Exception>();
        var pending = new Stack<Exception>();
        var visited = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
        pending.Push(exception);
        while (pending.TryPop(out var cause))
        {
            if (!visited.Add(cause))
            {
                continue;
            }
            causes.Add(cause);
            if (cause is AggregateException aggregate)
            {
                for (var index = aggregate.InnerExceptions.Count - 1; index >= 0; index--)
                {
                    pending.Push(aggregate.InnerExceptions[index]);
                }
            }
            else if (cause.InnerException is { } inner)
            {
                pending.Push(inner);
            }
        }
        return (TypeName(exception), string.Join(CauseSeparator, causes.Select(TypeName)),
            string.Join(Environment.NewLine, causes.Select(static cause =>
                string.Concat(TypeName(cause), Environment.NewLine, new StackTrace(cause, false).ToString()))));
    }

    internal static void RecordException(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }
        if (!IsActive)
        {
            activity.AddException(exception);
            return;
        }
        var diagnostic = Describe(exception);
        activity.AddEvent(new ActivityEvent(ExceptionEvent, tags: new ActivityTagsCollection
        {
            { ExceptionTypeTag, diagnostic.ExceptionType },
            { ExceptionStackTraceTag, diagnostic.StackTrace },
            { ExceptionCauseTypesTag, diagnostic.CauseTypes }
        }));
    }

    internal static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        using var diagnostics = Begin(true);
        return await operation().ConfigureAwait(false);
    }

    // Keep the policy inside each MoveNext/Dispose operation, never around the consumer's yield body.
    internal static async IAsyncEnumerable<T> ReadStreamAsync<T>(IAsyncEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerator<T> enumerator;
        using (Begin(true))
        {
            enumerator = source.GetAsyncEnumerator(cancellationToken);
        }
        try
        {
            while (true)
            {
                T current;
                using (Begin(true))
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield break;
                    }
                    current = enumerator.Current;
                }
                yield return current;
            }
        }
        finally
        {
            using var diagnostics = Begin(true);
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string TypeName(Exception exception) => exception.GetType().FullName ?? exception.GetType().Name;
}

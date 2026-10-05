using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Tests.CQRS;

internal static class CqrsStreamCancellationJoinCleanup
{
    private const string AggregateFailureMessage = "The CQRS stream operation and cleanup both failed.";

    internal static List<Exception> Leaves(Exception failure)
    {
        var leaves = new List<Exception>();
        AddLeaves(failure, leaves);
        return leaves;
    }

    internal static async Task ObserveTaskAsync(Task task, List<Exception> failures)
    {
        await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (task.Exception is { } aggregate)
        {
            failures.AddRange(aggregate.InnerExceptions);
        }
        else if (task.IsCanceled)
        {
            failures.Add(new TaskCanceledException(task));
        }
    }

    internal static void ThrowFailures(Exception? primary, IReadOnlyList<Exception> cleanup)
    {
        if (primary is null && cleanup.Count == 0)
        {
            return;
        }
        if (primary is not null && cleanup.Count == 0)
        {
            ExceptionDispatchInfo.Capture(primary).Throw();
        }
        if (primary is null && cleanup.Count == 1)
        {
            ExceptionDispatchInfo.Capture(cleanup[0]).Throw();
        }

        var failures = new List<Exception>(cleanup.Count + (primary is null ? 0 : 1));
        if (primary is not null)
        {
            failures.Add(primary);
        }
        failures.AddRange(cleanup);
        throw new AggregateException(AggregateFailureMessage, failures);
    }

    private static void AddLeaves(Exception exception, List<Exception> leaves)
    {
        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                AddLeaves(inner, leaves);
            }
        }
        else
        {
            leaves.Add(exception);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace ManagedCode.Communication.Tests.CQRS;

internal static class CqrsHttpBoundsFailure
{
    private const string AggregateFailureMessage = "The response read or its settlement failed.";

    public static void AddDistinct(List<Exception> failures, Exception exception, Exception? primaryFailure)
    {
        if (!ReferenceEquals(exception, primaryFailure))
        {
            failures.Add(exception);
        }
    }

    public static void Throw(Exception? primaryFailure, IReadOnlyCollection<Exception> cleanupFailures)
    {
        if (primaryFailure is null && cleanupFailures.Count == 0)
        {
            return;
        }

        if (primaryFailure is not null && cleanupFailures.Count == 0)
        {
            ExceptionDispatchInfo.Capture(primaryFailure).Throw();
        }

        var failures = new List<Exception>(cleanupFailures.Count + (primaryFailure is null ? 0 : 1));
        if (primaryFailure is not null)
        {
            failures.Add(primaryFailure);
        }

        failures.AddRange(cleanupFailures);
        throw new AggregateException(AggregateFailureMessage, failures);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication;

/// <summary>
///     Aggregation of several results into one.
/// </summary>
/// <remarks>
///     These live on <see cref="Result" /> itself rather than in the railway extensions package, so combining
///     results never needs an extra reference or a <c>using</c>.
/// </remarks>
public partial struct Result
{
    private const string MultipleErrorsTitle = "Multiple errors occurred";
    private const string MultipleErrorsDetail = "The operation failed with multiple errors.";

    /// <summary>
    ///     Merges multiple results and returns the first failure, or success when all results succeed.
    /// </summary>
    public static Result Merge(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailed)
            {
                return result;
            }
        }

        return Succeed();
    }

    /// <summary>
    ///     Merges multiple results and aggregates all failures.
    /// </summary>
    public static Result MergeAll(params Result[] results)
    {
        var problems = CollectMultipleProblems(results, out var firstFailure);
        if (problems is null)
        {
            return firstFailure ?? Succeed();
        }

        return AllAreValidationFailures(problems)
            ? Fail(CreateValidationProblem(problems))
            : Fail(CreateAggregateProblem(problems));
    }

    /// <summary>
    ///     Combines successful result values into a collection result, failing on the first failure.
    /// </summary>
    public static CollectionResult<T> Combine<T>(params Result<T>[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailed)
            {
                return result.TryGetProblem(out var problem)
                    ? CollectionResult<T>.Fail(problem)
                    : CollectionResult<T>.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
            }
        }

        return CollectionResult<T>.Succeed(Array.ConvertAll(results, static result => result.Value!));
    }

    /// <summary>
    ///     Combines successful values or aggregates every failure into a collection result.
    /// </summary>
    public static CollectionResult<T> CombineAll<T>(params Result<T>[] results)
    {
        var problems = CollectMultipleProblems(results, out var firstFailure);
        if (firstFailure is not { } failure)
        {
            return CollectionResult<T>.Succeed(Array.ConvertAll(results, static result => result.Value!));
        }

        problems ??= new List<Problem>(1) { failure.Problem! };
        return AllAreValidationFailures(problems)
            ? CollectionResult<T>.Fail(CreateValidationProblem(problems))
            : CollectionResult<T>.Fail(CreateAggregateProblem(problems));
    }

    private static List<Problem>? CollectMultipleProblems<TResult>(TResult[] results, out TResult? firstFailure)
        where TResult : struct, IResult
    {
        ArgumentNullException.ThrowIfNull(results);
        firstFailure = null;
        List<Problem>? problems = null;
        foreach (var result in results)
        {
            if (!result.IsFailed)
            {
                continue;
            }

            if (problems is not null)
            {
                problems.Add(result.Problem);
            }
            else if (firstFailure is { } first)
            {
                problems = new List<Problem> { first.Problem!, result.Problem };
            }
            else
            {
                firstFailure = result;
            }
        }

        return problems;
    }

    private static bool AllAreValidationFailures(List<Problem> problems)
    {
        return problems.All(static problem => problem.GetValidationErrors() is not null);
    }

    private static Problem CreateValidationProblem(List<Problem> problems)
    {
        var aggregate = Problem.Validation();
        var validationErrors = aggregate.GetValidationErrors()!;

        foreach (var problem in problems)
        {
            var errors = problem.GetValidationErrors();
            if (errors is null || errors.Count == 0)
            {
                aggregate.AddValidationError(
                    ProblemConstants.ValidationFields.General,
                    problem.Detail ?? problem.Title ?? ProblemConstants.Messages.GenericError);
                continue;
            }

            foreach (var (field, messages) in errors)
            {
                if (messages.Count == 0)
                {
                    continue;
                }

                if (!validationErrors.TryGetValue(field, out var existing))
                {
                    validationErrors.Add(field, new List<string>(messages));
                }
                else if (messages.Count == 1)
                {
                    existing.Add(messages[0]);
                }
                else
                {
                    existing.AddRange(messages);
                }
            }
        }

        return aggregate;
    }

    private static Problem CreateAggregateProblem(IReadOnlyCollection<Problem> problems)
    {
        var aggregateProblem = Problem.Create(MultipleErrorsTitle, MultipleErrorsDetail, 500);
        aggregateProblem.Extensions[ProblemConstants.ExtensionKeys.Errors] = problems.ToArray();
        return aggregateProblem;
    }
}

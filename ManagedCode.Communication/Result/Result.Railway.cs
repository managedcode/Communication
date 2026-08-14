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
        var problems = FailedProblems(results);

        if (problems.Length == 0)
        {
            return Succeed();
        }

        if (problems.Length == 1)
        {
            return Fail(problems[0]);
        }

        return AllAreValidationFailures(problems)
            ? FailValidation(CollectValidationErrors(problems).ToArray())
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

        return CollectionResult<T>.Succeed(results.Select(static result => result.Value!).ToList());
    }

    /// <summary>
    ///     Combines successful values or aggregates every failure into a collection result.
    /// </summary>
    public static CollectionResult<T> CombineAll<T>(params Result<T>[] results)
    {
        var problems = FailedProblems(results);

        if (problems.Length == 0)
        {
            return CollectionResult<T>.Succeed(results.Select(static result => result.Value!).ToList());
        }

        return AllAreValidationFailures(problems)
            ? CollectionResult<T>.FailValidation(CollectValidationErrors(problems).ToArray())
            : CollectionResult<T>.Fail(CreateAggregateProblem(problems));
    }

    private static Problem[] FailedProblems(Result[] results)
    {
        return results
            .Where(static result => result.IsFailed)
            .Select(static failure => failure.TryGetProblem(out var problem) ? problem : Problem.GenericError())
            .ToArray();
    }

    private static Problem[] FailedProblems<T>(Result<T>[] results)
    {
        return results
            .Where(static result => result.IsFailed)
            .Select(static failure => failure.TryGetProblem(out var problem) ? problem : Problem.GenericError())
            .ToArray();
    }

    private static bool AllAreValidationFailures(Problem[] problems)
    {
        return problems.All(static problem => problem.GetValidationErrors() is not null);
    }

    private static List<(string field, string message)> CollectValidationErrors(IEnumerable<Problem> problems)
    {
        var validationErrors = new List<(string field, string message)>();

        foreach (var problem in problems)
        {
            var errors = problem.GetValidationErrors();
            if (errors is null || errors.Count == 0)
            {
                validationErrors.Add((
                    ProblemConstants.ValidationFields.General,
                    problem.Detail ?? problem.Title ?? ProblemConstants.Messages.GenericError));
                continue;
            }

            foreach (var kvp in errors)
            {
                foreach (var error in kvp.Value)
                {
                    validationErrors.Add((kvp.Key, error));
                }
            }
        }

        return validationErrors;
    }

    private static Problem CreateAggregateProblem(IReadOnlyCollection<Problem> problems)
    {
        var aggregateProblem = Problem.Create(MultipleErrorsTitle, MultipleErrorsDetail, 500);
        aggregateProblem.Extensions[ProblemConstants.ExtensionKeys.Errors] = problems.ToArray();
        return aggregateProblem;
    }
}

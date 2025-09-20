using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication.Extensions;

/// <summary>
///     Advanced railway-oriented programming extensions for Result types.
/// </summary>
public static class AdvancedRailwayExtensions
{
    #region Then/ThenAsync (Alias for Bind)

    /// <summary>
    ///     Alias for Bind - executes the next function if successful.
    /// </summary>
    public static Result<TOut> Then<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> next)
    {
        return result.Bind(next);
    }

    /// <summary>
    ///     Alias for Bind - executes the next function if successful.
    /// </summary>
    public static Result Then<T>(this Result<T> result, Func<T, Result> next)
    {
        return result.Bind(next);
    }

    /// <summary>
    ///     Async version of Then.
    /// </summary>
    public static Task<Result<TOut>> ThenAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> next)
    {
        if (result.IsSuccess)
            return next(result.Value);
        
        return result.TryGetProblem(out var problem) 
            ? Task.FromResult(Result<TOut>.Fail(problem))
            : Task.FromResult(Result<TOut>.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError));
    }

    /// <summary>
    ///     Async version of Then for Task<Result<T>>.
    /// </summary>
    public static Task<Result<TOut>> ThenAsync<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> next)
    {
        return resultTask.BindAsync(next);
    }

    #endregion

    #region FailIf/OkIf Conditional Methods

    /// <summary>
    ///     Fails the result if the predicate is true.
    /// </summary>
    public static Result<T> FailIf<T>(this Result<T> result, Func<T, bool> predicate, Problem problem)
    {
        if (result.IsSuccess && predicate(result.Value))
        {
            return Result<T>.Fail(problem);
        }
        return result;
    }

    /// <summary>
    ///     Fails the result if the predicate is true with a custom error enum.
    /// </summary>
    public static Result<T> FailIf<T, TEnum>(this Result<T> result, Func<T, bool> predicate, TEnum errorCode) 
        where TEnum : Enum
    {
        if (result.IsSuccess && predicate(result.Value))
        {
            return Result<T>.Fail(errorCode);
        }
        return result;
    }

    /// <summary>
    ///     Fails the result if the predicate is true with validation errors.
    /// </summary>
    public static Result<T> FailIf<T>(this Result<T> result, Func<T, bool> predicate, params (string field, string message)[] errors)
    {
        if (result.IsSuccess && predicate(result.Value))
        {
            return Result<T>.FailValidation(errors);
        }
        return result;
    }

    /// <summary>
    ///     Succeeds only if the predicate is true, otherwise fails.
    /// </summary>
    public static Result<T> OkIf<T>(this Result<T> result, Func<T, bool> predicate, Problem problem)
    {
        if (result.IsSuccess && !predicate(result.Value))
        {
            return Result<T>.Fail(problem);
        }
        return result;
    }

    #endregion

    #region Merge/Combine Multiple Results

    /// <summary>
    ///     Merges multiple results, failing if any failed.
    /// </summary>
    public static Result Merge(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailed)
                return result;
        }
        return Result.Succeed();
    }

    /// <summary>
    ///     Merges multiple results, collecting all failures.
    /// </summary>
    public static Result MergeAll(params Result[] results)
    {
        var failures = results.Where(r => r.IsFailed).ToArray();
        
        if (failures.Length == 0)
            return Result.Succeed();
        
        if (failures.Length == 1)
            return failures[0];
        
        // Combine multiple failures into one with all error details
        var validationErrors = new List<(string field, string message)>();
        foreach (var failure in failures)
        {
            if (failure.Problem != null)
            {
                var errors = failure.Problem.GetValidationErrors();
                if (errors != null)
                {
                    foreach (var kvp in errors)
                    {
                        foreach (var error in kvp.Value)
                        {
                            validationErrors.Add((kvp.Key, error));
                        }
                    }
                }
                else
                {
                    // Non-validation error
                    validationErrors.Add(("error", failure.Problem.Detail ?? failure.Problem.Title ?? ProblemConstants.Messages.GenericError));
                }
            }
        }
        
        return Result.FailValidation(validationErrors.ToArray());
    }

    /// <summary>
    ///     Combines multiple results into a collection result.
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

        var values = results.Select(r => r.Value!).ToList();
        return CollectionResult<T>.Succeed(values);
    }

    /// <summary>
    ///     Combines multiple results, collecting all values or failing with all errors.
    /// </summary>
    public static CollectionResult<T> CombineAll<T>(params Result<T>[] results)
    {
        var failures = results.Where(r => r.IsFailed).ToArray();
        
        if (failures.Length == 0)
        {
            var values = results.Select(r => r.Value!).ToList();
            return CollectionResult<T>.Succeed(values);
        }
        
        // Collect all validation errors
        var validationErrors = new List<(string field, string message)>();
        foreach (var failure in failures)
        {
            if (failure.Problem != null)
            {
                var errors = failure.Problem.GetValidationErrors();
                if (errors != null)
                {
                    foreach (var kvp in errors)
                    {
                        foreach (var error in kvp.Value)
                        {
                            validationErrors.Add((kvp.Key, error));
                        }
                    }
                }
                else
                {
                    validationErrors.Add(("error", failure.Problem.Detail ?? failure.Problem.Title ?? ProblemConstants.Messages.GenericError));
                }
            }
        }
        
        return CollectionResult<T>.FailValidation(validationErrors.ToArray());
    }

    #endregion

    #region Switch/Case Pattern

    /// <summary>
    ///     Switches execution based on the result state.
    /// </summary>
    public static Result<T> Switch<T>(this Result<T> result, Action<T> onSuccess, Action<Problem> onFailure)
    {
        if (result.IsSuccess)
        {
            onSuccess(result.Value);
        }
        else if (result.TryGetProblem(out var problem))
        {
            onFailure(problem);
        }
        else
        {
            onFailure(Problem.GenericError());
        }
        return result;
    }

    /// <summary>
    ///     Switches to different results based on the current state.
    /// </summary>
    public static Result<TOut> SwitchFirst<T, TOut>(this Result<T> result, 
        params (Func<T, bool> condition, Func<T, Result<TOut>> action)[] cases)
    {
        if (result.IsFailed)
        {
            return result.TryGetProblem(out var problem)
                ? Result<TOut>.Fail(problem)
                : Result<TOut>.Fail(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
        }

        foreach (var (condition, action) in cases)
        {
            if (condition(result.Value))
            {
                return action(result.Value);
            }
        }

        return Result<TOut>.Fail(ProblemConstants.Titles.BadRequest, "None of the switch conditions were met");
    }

    #endregion

    #region Compensate/Recover

    /// <summary>
    ///     Attempts to recover from a failure.
    /// </summary>
    public static Result<T> Compensate<T>(this Result<T> result, Func<Problem, Result<T>> recovery)
    {
        if (result.IsSuccess)
            return result;
            
        return result.TryGetProblem(out var problem)
            ? recovery(problem)
            : recovery(Problem.GenericError());
    }

    /// <summary>
    ///     Attempts to recover with a default value.
    /// </summary>
    public static Result<T> CompensateWith<T>(this Result<T> result, T defaultValue)
    {
        return result.IsFailed ? Result<T>.Succeed(defaultValue) : result;
    }

    /// <summary>
    ///     Attempts async recovery from a failure.
    /// </summary>
    public static async Task<Result<T>> CompensateAsync<T>(this Result<T> result, Func<Problem, Task<Result<T>>> recovery)
    {
        if (result.IsSuccess)
            return result;
            
        var problem = result.TryGetProblem(out var p)
            ? p
            : Problem.GenericError();
            
        return await recovery(problem);
    }

    #endregion

    #region Check/Verify

    /// <summary>
    ///     Performs a check without transforming the value.
    /// </summary>
    public static Result<T> Check<T>(this Result<T> result, Action<T> verification)
    {
        if (result.IsSuccess)
        {
            try
            {
                verification(result.Value);
            }
            catch (Exception ex)
            {
                return Result<T>.Fail(ex);
            }
        }
        return result;
    }

    /// <summary>
    ///     Verifies a condition and adds context to any failure.
    /// </summary>
    public static Result<T> Verify<T>(this Result<T> result, Func<T, bool> condition, string context)
    {
        if (result.IsSuccess && !condition(result.Value))
        {
            return Result<T>.Fail($"Verification failed: {context}", $"The condition '{context}' was not met");
        }
        return result;
    }

    #endregion

    #region ToResult Conversions

    /// <summary>
    ///     Converts a nullable value to a Result.
    /// </summary>
    public static Result<T> ToResult<T>(this T? value, Problem problemIfNull) where T : class
    {
        return value != null ? Result<T>.Succeed(value) : Result<T>.Fail(problemIfNull);
    }

    /// <summary>
    ///     Converts a nullable value to a Result with a default error.
    /// </summary>
    public static Result<T> ToResult<T>(this T? value) where T : class
    {
        return value != null 
            ? Result<T>.Succeed(value) 
            : Result<T>.FailNotFound($"{typeof(T).Name} not found");
    }

    /// <summary>
    ///     Converts a nullable struct to a Result.
    /// </summary>
    public static Result<T> ToResult<T>(this T? value, Problem problemIfNull) where T : struct
    {
        return value.HasValue ? Result<T>.Succeed(value.Value) : Result<T>.Fail(problemIfNull);
    }

    #endregion

    #region Do/Execute Side Effects

    /// <summary>
    ///     Executes an action for its side effects if successful.
    /// </summary>
    public static Result<T> Do<T>(this Result<T> result, Action<T> action)
    {
        return result.Tap(action);
    }

    /// <summary>
    ///     Executes an async action for its side effects if successful.
    /// </summary>
    public static async Task<Result<T>> DoAsync<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess)
        {
            await action(result.Value);
        }
        return result;
    }

    #endregion

    #region Filter

    /// <summary>
    ///     Filters the result based on a predicate.
    /// </summary>
    public static Result<T> Where<T>(this Result<T> result, Func<T, bool> predicate, Problem problemIfFalse)
    {
        if (result.IsSuccess && !predicate(result.Value))
        {
            return Result<T>.Fail(problemIfFalse);
        }
        return result;
    }

    /// <summary>
    ///     Filters the result based on a predicate with a custom error message.
    /// </summary>
    public static Result<T> Where<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
        if (result.IsSuccess && !predicate(result.Value))
        {
            return Result<T>.Fail("Filter failed", errorMessage);
        }
        return result;
    }

    #endregion
}

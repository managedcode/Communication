using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for standardized factory methods to create Result instances.
/// </summary>
public interface IResultFactory
{
    #region Basic Success Methods
    
    /// <summary>
    ///     Creates a successful result without a value.
    /// </summary>
    /// <returns>A successful result.</returns>
    Result Succeed();

    /// <summary>
    ///     Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to include in the result.</param>
    /// <returns>A successful result containing the specified value.</returns>
    Result<T> Succeed<T>(T value);

    /// <summary>
    ///     Creates a successful result by executing an action on a new instance.
    /// </summary>
    /// <typeparam name="T">The type to create and configure.</typeparam>
    /// <param name="action">The action to execute on the new instance.</param>
    /// <returns>A successful result containing the configured instance.</returns>
    Result<T> Succeed<T>(Action<T> action) where T : new();

    #endregion

    #region Basic Failure Methods

    /// <summary>
    ///     Creates a failed result without additional details.
    /// </summary>
    /// <returns>A failed result.</returns>
    Result Fail();

    /// <summary>
    ///     Creates a failed result with a problem.
    /// </summary>
    /// <param name="problem">The problem that caused the failure.</param>
    /// <returns>A failed result with the specified problem.</returns>
    Result Fail(Problem problem);

    /// <summary>
    ///     Creates a failed result with a title.
    /// </summary>
    /// <param name="title">The title describing the failure.</param>
    /// <returns>A failed result with the specified title.</returns>
    Result Fail(string title);

    /// <summary>
    ///     Creates a failed result with a title and detail.
    /// </summary>
    /// <param name="title">The title describing the failure.</param>
    /// <param name="detail">Additional details about the failure.</param>
    /// <returns>A failed result with the specified title and detail.</returns>
    Result Fail(string title, string detail);

    /// <summary>
    ///     Creates a failed result with a title, detail, and HTTP status code.
    /// </summary>
    /// <param name="title">The title describing the failure.</param>
    /// <param name="detail">Additional details about the failure.</param>
    /// <param name="status">The HTTP status code.</param>
    /// <returns>A failed result with the specified parameters.</returns>
    Result Fail(string title, string detail, HttpStatusCode status);

    /// <summary>
    ///     Creates a failed result from an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result based on the exception.</returns>
    Result Fail(Exception exception);

    /// <summary>
    ///     Creates a failed result from an exception with a specific HTTP status code.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="status">The HTTP status code.</param>
    /// <returns>A failed result based on the exception and status code.</returns>
    Result Fail(Exception exception, HttpStatusCode status);

    #endregion

    #region Generic Failure Methods

    /// <summary>
    ///     Creates a failed result with a value type and problem.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="problem">The problem that caused the failure.</param>
    /// <returns>A failed result with the specified problem.</returns>
    Result<T> Fail<T>(Problem problem);

    /// <summary>
    ///     Creates a failed result with a value type and title.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="title">The title describing the failure.</param>
    /// <returns>A failed result with the specified title.</returns>
    Result<T> Fail<T>(string title);

    /// <summary>
    ///     Creates a failed result with a value type, title, and detail.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="title">The title describing the failure.</param>
    /// <param name="detail">Additional details about the failure.</param>
    /// <returns>A failed result with the specified title and detail.</returns>
    Result<T> Fail<T>(string title, string detail);

    /// <summary>
    ///     Creates a failed result with a value type from an exception.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed result based on the exception.</returns>
    Result<T> Fail<T>(Exception exception);

    #endregion

    #region Validation Failure Methods

    /// <summary>
    ///     Creates a failed result with validation errors.
    /// </summary>
    /// <param name="errors">The validation errors as field-message pairs.</param>
    /// <returns>A failed result with validation errors.</returns>
    Result FailValidation(params (string field, string message)[] errors);

    /// <summary>
    ///     Creates a failed result with validation errors for a specific value type.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="errors">The validation errors as field-message pairs.</param>
    /// <returns>A failed result with validation errors.</returns>
    Result<T> FailValidation<T>(params (string field, string message)[] errors);

    #endregion

    #region HTTP Status Specific Methods

    /// <summary>
    ///     Creates a failed result for bad request (400).
    /// </summary>
    /// <returns>A failed result with bad request status.</returns>
    Result FailBadRequest();

    /// <summary>
    ///     Creates a failed result for bad request (400) with custom detail.
    /// </summary>
    /// <param name="detail">Additional details about the bad request.</param>
    /// <returns>A failed result with bad request status and custom detail.</returns>
    Result FailBadRequest(string detail);

    /// <summary>
    ///     Creates a failed result for unauthorized access (401).
    /// </summary>
    /// <returns>A failed result with unauthorized status.</returns>
    Result FailUnauthorized();

    /// <summary>
    ///     Creates a failed result for unauthorized access (401) with custom detail.
    /// </summary>
    /// <param name="detail">Additional details about the unauthorized access.</param>
    /// <returns>A failed result with unauthorized status and custom detail.</returns>
    Result FailUnauthorized(string detail);

    /// <summary>
    ///     Creates a failed result for forbidden access (403).
    /// </summary>
    /// <returns>A failed result with forbidden status.</returns>
    Result FailForbidden();

    /// <summary>
    ///     Creates a failed result for forbidden access (403) with custom detail.
    /// </summary>
    /// <param name="detail">Additional details about the forbidden access.</param>
    /// <returns>A failed result with forbidden status and custom detail.</returns>
    Result FailForbidden(string detail);

    /// <summary>
    ///     Creates a failed result for not found (404).
    /// </summary>
    /// <returns>A failed result with not found status.</returns>
    Result FailNotFound();

    /// <summary>
    ///     Creates a failed result for not found (404) with custom detail.
    /// </summary>
    /// <param name="detail">Additional details about what was not found.</param>
    /// <returns>A failed result with not found status and custom detail.</returns>
    Result FailNotFound(string detail);

    #endregion

    #region Enum-based Failure Methods

    /// <summary>
    ///     Creates a failed result from a custom error enum.
    /// </summary>
    /// <typeparam name="TEnum">The enum type representing error codes.</typeparam>
    /// <param name="errorCode">The error code from the enum.</param>
    /// <returns>A failed result based on the error code.</returns>
    Result Fail<TEnum>(TEnum errorCode) where TEnum : Enum;

    /// <summary>
    ///     Creates a failed result from a custom error enum with additional detail.
    /// </summary>
    /// <typeparam name="TEnum">The enum type representing error codes.</typeparam>
    /// <param name="errorCode">The error code from the enum.</param>
    /// <param name="detail">Additional details about the error.</param>
    /// <returns>A failed result based on the error code and detail.</returns>
    Result Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum;

    /// <summary>
    ///     Creates a failed result from a custom error enum with specific HTTP status.
    /// </summary>
    /// <typeparam name="TEnum">The enum type representing error codes.</typeparam>
    /// <param name="errorCode">The error code from the enum.</param>
    /// <param name="status">The HTTP status code.</param>
    /// <returns>A failed result based on the error code and status.</returns>
    Result Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum;

    /// <summary>
    ///     Creates a failed result from a custom error enum with detail and specific HTTP status.
    /// </summary>
    /// <typeparam name="TEnum">The enum type representing error codes.</typeparam>
    /// <param name="errorCode">The error code from the enum.</param>
    /// <param name="detail">Additional details about the error.</param>
    /// <param name="status">The HTTP status code.</param>
    /// <returns>A failed result based on the error code, detail, and status.</returns>
    Result Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum;

    #endregion

    #region From Methods

    /// <summary>
    ///     Creates a result from a boolean value.
    /// </summary>
    /// <param name="success">Whether the operation was successful.</param>
    /// <returns>A result based on the success value.</returns>
    Result From(bool success);

    /// <summary>
    ///     Creates a result from a boolean and problem.
    /// </summary>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="problem">The problem to include if not successful.</param>
    /// <returns>A result based on the success value and problem.</returns>
    Result From(bool success, Problem? problem);

    /// <summary>
    ///     Creates a result with value from a boolean and value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="value">The value to include in the result.</param>
    /// <returns>A result based on the success value and containing the value.</returns>
    Result<T> From<T>(bool success, T? value);

    /// <summary>
    ///     Creates a result with value from a boolean, value, and problem.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="value">The value to include in the result.</param>
    /// <param name="problem">The problem to include if not successful.</param>
    /// <returns>A result based on the success value, containing the value and problem.</returns>
    Result<T> From<T>(bool success, T? value, Problem? problem);

    /// <summary>
    ///     Creates a result from another result.
    /// </summary>
    /// <param name="result">The source result to copy from.</param>
    /// <returns>A new result based on the source result.</returns>
    Result From(IResult result);

    /// <summary>
    ///     Creates a result from a task that returns a result.
    /// </summary>
    /// <param name="resultTask">The task that returns a result.</param>
    /// <returns>A task that returns a result.</returns>
    Task<Result> From(Task<Result> resultTask);

    /// <summary>
    ///     Creates a result with value from a task that returns a result with value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="resultTask">The task that returns a result with value.</param>
    /// <returns>A task that returns a result with value.</returns>
    Task<Result<T>> From<T>(Task<Result<T>> resultTask);

    #endregion
}
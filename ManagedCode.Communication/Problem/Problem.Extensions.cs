using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication;

public partial class Problem
{
    /// <summary>
    ///     Custom error code stored in Extensions.
    /// </summary>
    [JsonIgnore]
    public string? ErrorCode
    {
        get => Extensions.TryGetValue(ProblemExtensionKeys.ErrorCode, out var code) ? code?.ToString() : null;
        set
        {
            if (value != null)
            {
                Extensions[ProblemExtensionKeys.ErrorCode] = value;
            }
            else
            {
                Extensions.Remove(ProblemExtensionKeys.ErrorCode);
            }
        }
    }

    /// <summary>
    ///     Creates a Problem with specified values.
    /// </summary>
    public static Problem Create(string type, string title, int statusCode, string detail, string? instance = null)
    {
        return new Problem
        {
            Type = type,
            Title = title,
            StatusCode = statusCode,
            Detail = detail,
            Instance = instance
        };
    }

    /// <summary>
    ///     Creates a Problem from an HTTP status code.
    /// </summary>
    public static Problem FromStatusCode(HttpStatusCode statusCode, string? detail = null)
    {
        return new Problem
        {
            Type = $"https://httpstatuses.io/{(int)statusCode}",
            Title = statusCode.ToString(),
            StatusCode = (int)statusCode,
            Detail = detail
        };
    }

    /// <summary>
    ///     Creates a Problem from a custom error enum.
    /// </summary>
    public static Problem FromEnum<TEnum>(TEnum errorCode, string? detail = null, int status = 400) where TEnum : Enum
    {
        var problem = Create($"https://httpstatuses.io/{status}", errorCode.ToString(), status, detail ?? $"An error occurred: {errorCode}");

        problem.ErrorCode = errorCode.ToString();
        problem.Extensions[ProblemExtensionKeys.ErrorType] = typeof(TEnum).Name;

        return problem;
    }

    /// <summary>
    ///     Creates a Problem from an exception.
    /// </summary>
    public static Problem FromException(Exception exception, int status = 500)
    {
        var problem = new Problem
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = exception.GetType()
                .Name,
            Detail = exception.Message,
            StatusCode = status
        };

        problem.ErrorCode = exception.GetType()
            .FullName;
        
        // Store the original exception type for potential reconstruction
        problem.Extensions[ProblemExtensionKeys.OriginalExceptionType] = exception.GetType().FullName;

        if (exception.Data.Count > 0)
        {
            foreach (var key in exception.Data.Keys)
            {
                if (key != null)
                {
                    problem.Extensions[$"{ProblemExtensionKeys.ExceptionDataPrefix}{key}"] = exception.Data[key];
                }
            }
        }

        return problem;
    }

    /// <summary>
    ///     Creates a validation Problem with errors.
    /// </summary>
    public static Problem Validation(params (string field, string message)[] errors)
    {
        var problem = new Problem
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Failed",
            StatusCode = 400,
            Detail = "One or more validation errors occurred."
        };

        var errorDict = new Dictionary<string, List<string>>();
        foreach (var (field, message) in errors)
        {
            if (!errorDict.ContainsKey(field))
            {
                errorDict[field] = new List<string>();
            }

            errorDict[field]
                .Add(message);
        }

        problem.Extensions[ProblemExtensionKeys.Errors] = errorDict;
        return problem;
    }

    /// <summary>
    ///     Checks if this problem has a specific error code.
    /// </summary>
    public bool HasErrorCode<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return ErrorCode == errorCode.ToString();
    }

    /// <summary>
    ///     Gets error code as enum if possible.
    /// </summary>
    public TEnum? GetErrorCodeAs<TEnum>() where TEnum : struct, Enum
    {
        if (ErrorCode != null && Enum.TryParse<TEnum>(ErrorCode, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    ///     Gets validation errors if any.
    /// </summary>
    public Dictionary<string, List<string>>? GetValidationErrors()
    {
        if (Extensions.TryGetValue(ProblemExtensionKeys.Errors, out var errors))
        {
            return errors as Dictionary<string, List<string>>;
        }

        return null;
    }

    /// <summary>
    ///     Creates a copy of this Problem with the specified extensions added.
    /// </summary>
    public Problem WithExtensions(IDictionary<string, object?> additionalExtensions)
    {
        var problem = Create(Type ?? "about:blank", Title ?? "Error", StatusCode, Detail ?? "An error occurred", Instance);

        // Copy existing extensions
        foreach (var extension in Extensions)
        {
            problem.Extensions[extension.Key] = extension.Value;
        }

        // Add new extensions
        foreach (var extension in additionalExtensions)
        {
            problem.Extensions[extension.Key] = extension.Value;
        }

        return problem;
    }

    /// <summary>
    ///     Converts the Problem to an exception, attempting to reconstruct the original exception type if possible.
    /// </summary>
    public Exception ToException()
    {
        // Check if we have the original exception type stored
        if (Extensions.TryGetValue(ProblemExtensionKeys.OriginalExceptionType, out var originalTypeObj) && 
            originalTypeObj is string originalTypeName)
        {
            try
            {
                // Try to get the type from the current app domain
                var originalType = System.Type.GetType(originalTypeName);
                
                // If not found, search in all loaded assemblies
                if (originalType == null)
                {
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        originalType = assembly.GetType(originalTypeName.Split(',')[0]);
                        if (originalType != null)
                            break;
                    }
                }
                
                if (originalType != null && typeof(Exception).IsAssignableFrom(originalType))
                {
                    // Try to create instance of the original exception type
                    if (Activator.CreateInstance(originalType, Detail ?? Title ?? "An error occurred") is Exception reconstructedException)
                    {
                        // Restore any exception data
                        foreach (var kvp in Extensions)
                        {
                            if (kvp.Key.StartsWith(ProblemExtensionKeys.ExceptionDataPrefix))
                            {
                                var dataKey = kvp.Key.Substring(ProblemExtensionKeys.ExceptionDataPrefix.Length);
                                reconstructedException.Data[dataKey] = kvp.Value;
                            }
                        }
                        return reconstructedException;
                    }
                }
            }
            catch
            {
                // If reconstruction fails, fall back to ProblemException
            }
        }

        // Default to ProblemException
        return new ProblemException(this);
    }
}
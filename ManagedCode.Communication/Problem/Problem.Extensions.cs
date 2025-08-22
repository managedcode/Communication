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
    public string ErrorCode
    {
        get
        {
            if (Extensions.TryGetValue(ProblemConstants.ExtensionKeys.ErrorCode, out var code))
            {
                // Handle JsonElement from deserialization
                if (code is System.Text.Json.JsonElement jsonElement)
                {
                    return jsonElement.GetString() ?? string.Empty;
                }
                return code?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Extensions[ProblemConstants.ExtensionKeys.ErrorCode] = value;
            }
            else
            {
                Extensions.Remove(ProblemConstants.ExtensionKeys.ErrorCode);
            }
        }
    }

    /// <summary>
    ///     Creates a Problem with title and detail.
    /// </summary>
    public static Problem Create(string title, string detail)
    {
        return new Problem
        {
            Type = ProblemConstants.Types.HttpStatus(500),
            Title = title,
            StatusCode = 500,
            Detail = detail
        };
    }
    
    /// <summary>
    ///     Creates a Problem with title, detail and status code.
    /// </summary>
    public static Problem Create(string title, string detail, int statusCode)
    {
        return new Problem
        {
            Type = ProblemConstants.Types.HttpStatus(statusCode),
            Title = title,
            StatusCode = statusCode,
            Detail = detail
        };
    }
    
    /// <summary>
    ///     Creates a Problem with title, detail and HttpStatusCode.
    /// </summary>
    public static Problem Create(string title, string detail, HttpStatusCode statusCode)
    {
        return new Problem
        {
            Type = ProblemConstants.Types.HttpStatus((int)statusCode),
            Title = title,
            StatusCode = (int)statusCode,
            Detail = detail
        };
    }
    
    /// <summary>
    ///     Creates a Problem with title, detail, status code and type.
    /// </summary>
    public static Problem Create(string title, string detail, int statusCode, string type)
    {
        return new Problem
        {
            Type = type,
            Title = title,
            StatusCode = statusCode,
            Detail = detail
        };
    }
    
    /// <summary>
    ///     Creates a Problem with all fields.
    /// </summary>
    public static Problem Create(string title, string detail, int statusCode, string type, string instance)
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
    public static Problem Create(HttpStatusCode statusCode)
    {
        return new Problem
        {
            Type = ProblemConstants.Types.HttpStatus((int)statusCode),
            Title = statusCode.ToString(),
            StatusCode = (int)statusCode,
            Detail = statusCode.ToString()
        };
    }
    
    /// <summary>
    ///     Creates a Problem from an HTTP status code with detail.
    /// </summary>
    public static Problem Create(HttpStatusCode statusCode, string detail)
    {
        return new Problem
        {
            Type = ProblemConstants.Types.HttpStatus((int)statusCode),
            Title = statusCode.ToString(),
            StatusCode = (int)statusCode,
            Detail = detail
        };
    }

    /// <summary>
    ///     Creates a Problem from a custom error enum.
    /// </summary>
    public static Problem Create<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        // Try to get the enum's numeric value as status code
        var enumValue = Convert.ToInt32(errorCode);
        
        // If the enum value looks like an HTTP status code (100-599), use it
        // Otherwise default to 400 Bad Request for domain errors
        var statusCode = (enumValue >= 100 && enumValue <= 599) ? enumValue : 400;
        
        var problem = new Problem
        {
            Type = ProblemConstants.Types.HttpStatus(statusCode),
            Title = errorCode.ToString(),
            StatusCode = statusCode,
            Detail = $"{ProblemConstants.Messages.GenericError}: {errorCode}"
        };

        problem.ErrorCode = errorCode.ToString();
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType] = typeof(TEnum).Name;

        return problem;
    }
    
    /// <summary>
    ///     Creates a Problem from a custom error enum with detail.
    /// </summary>
    public static Problem Create<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        // Try to get the enum's numeric value as status code
        var enumValue = Convert.ToInt32(errorCode);
        
        // If the enum value looks like an HTTP status code (100-599), use it
        // Otherwise default to 400 Bad Request for domain errors
        var statusCode = (enumValue >= 100 && enumValue <= 599) ? enumValue : 400;
        
        var problem = new Problem
        {
            Type = ProblemConstants.Types.HttpStatus(statusCode),
            Title = errorCode.ToString(),
            StatusCode = statusCode,
            Detail = detail
        };

        problem.ErrorCode = errorCode.ToString();
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType] = typeof(TEnum).Name;

        return problem;
    }
    
    /// <summary>
    ///     Creates a Problem from a custom error enum with explicit status code.
    /// </summary>
    public static Problem Create<TEnum>(TEnum errorCode, int statusCode) where TEnum : Enum
    {
        var problem = new Problem
        {
            Type = ProblemConstants.Types.HttpStatus(statusCode),
            Title = errorCode.ToString(),
            StatusCode = statusCode,
            Detail = $"{ProblemConstants.Messages.GenericError}: {errorCode}"
        };

        problem.ErrorCode = errorCode.ToString();
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType] = typeof(TEnum).Name;

        return problem;
    }
    
    /// <summary>
    ///     Creates a Problem from a custom error enum with detail and status code.
    /// </summary>
    public static Problem Create<TEnum>(TEnum errorCode, string detail, int statusCode) where TEnum : Enum
    {
        var problem = new Problem
        {
            Type = ProblemConstants.Types.HttpStatus(statusCode),
            Title = errorCode.ToString(),
            StatusCode = statusCode,
            Detail = detail
        };

        problem.ErrorCode = errorCode.ToString();
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType] = typeof(TEnum).Name;

        return problem;
    }

    /// <summary>
    ///     Creates a Problem from an exception.
    /// </summary>
    public static Problem Create(Exception exception)
    {
        var problem = new Problem
        {
            Type = ProblemConstants.Types.HttpStatus(500),
            Title = exception.GetType().Name,
            Detail = exception.Message,
            StatusCode = 500
        };

        problem.ErrorCode = exception.GetType().FullName ?? exception.GetType().Name;
        
        // Store the original exception type for potential reconstruction
        problem.Extensions[ProblemConstants.ExtensionKeys.OriginalExceptionType] = exception.GetType().FullName;

        if (exception.Data.Count > 0)
        {
            foreach (var key in exception.Data.Keys)
            {
                if (key != null)
                {
                    problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}{key}"] = exception.Data[key];
                }
            }
        }

        return problem;
    }
    
    /// <summary>
    ///     Creates a Problem from an exception with HttpStatusCode.
    /// </summary>
    public static Problem Create(Exception exception, HttpStatusCode statusCode)
    {
        return Create(exception, (int)statusCode);
    }
    
    /// <summary>
    ///     Creates a Problem from an exception with status code.
    /// </summary>
    public static Problem Create(Exception exception, int statusCode)
    {
        var problem = new Problem
        {
            Type = ProblemConstants.Types.HttpStatus(statusCode),
            Title = exception.GetType().Name,
            Detail = exception.Message,
            StatusCode = statusCode
        };

        problem.ErrorCode = exception.GetType().FullName ?? exception.GetType().Name;
        
        // Store the original exception type for potential reconstruction
        problem.Extensions[ProblemConstants.ExtensionKeys.OriginalExceptionType] = exception.GetType().FullName;

        if (exception.Data.Count > 0)
        {
            foreach (var key in exception.Data.Keys)
            {
                if (key != null)
                {
                    problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}{key}"] = exception.Data[key];
                }
            }
        }

        return problem;
    }
    
    /// <summary>
    ///     Creates a Problem from an exception.
    /// </summary>
    public static Problem FromException(Exception exception)
    {
        return Create(exception);
    }
    
    /// <summary>
    ///     Creates a Problem from an exception with the status code.
    /// </summary>
    public static Problem FromException(Exception exception, int statusCode)
    {
        return Create(exception, statusCode);
    }
    
    /// <summary>
    ///     Creates a Problem from an HTTP status code.
    /// </summary>
    public static Problem FromStatusCode(HttpStatusCode statusCode)
    {
        return Create(statusCode);
    }
    
    /// <summary>
    ///     Creates a Problem from an HTTP status code with detail.
    /// </summary>
    public static Problem FromStatusCode(HttpStatusCode statusCode, string detail)
    {
        return Create(statusCode, detail);
    }
    
    /// <summary>
    ///     Creates a Problem from a custom error enum.
    /// </summary>
    public static Problem FromEnum<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return Create(errorCode);
    }
    
    /// <summary>
    ///     Creates a Problem from a custom error enum with detail.
    /// </summary>
    public static Problem FromEnum<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return Create(errorCode, detail);
    }
    
    /// <summary>
    ///     Creates a Problem from a custom error enum with detail and explicit status code.
    /// </summary>
    public static Problem FromEnum<TEnum>(TEnum errorCode, string detail, int statusCode) where TEnum : Enum
    {
        return Create(errorCode, detail, statusCode);
    }

    /// <summary>
    ///     Creates a validation Problem with errors.
    /// </summary>
    public static Problem Validation(params (string field, string message)[] errors)
    {
        var problem = new Problem
        {
            Type = ProblemConstants.Types.ValidationFailed,
            Title = ProblemConstants.Titles.ValidationFailed,
            StatusCode = (int)HttpStatusCode.BadRequest,
            Detail = ProblemConstants.Messages.ValidationErrors
        };

        var errorDict = new Dictionary<string, List<string>>();
        foreach (var (field, message) in errors)
        {
            if (!errorDict.ContainsKey(field))
            {
                errorDict[field] = new List<string>();
            }

            errorDict[field].Add(message);
        }

        problem.Extensions[ProblemConstants.ExtensionKeys.Errors] = errorDict;
        return problem;
    }

    /// <summary>
    ///     Creates a Problem for a generic error.
    /// </summary>
    public static Problem GenericError()
    {
        return Create(ProblemConstants.Titles.Error, ProblemConstants.Messages.GenericError);
    }

    /// <summary>
    ///     Checks if this problem has a specific error code.
    /// </summary>
    public bool HasErrorCode<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return !string.IsNullOrEmpty(ErrorCode) && ErrorCode == errorCode.ToString();
    }

    /// <summary>
    ///     Gets error code as enum if possible.
    /// </summary>
    public TEnum? GetErrorCodeAs<TEnum>() where TEnum : struct, Enum
    {
        if (!string.IsNullOrEmpty(ErrorCode) && Enum.TryParse<TEnum>(ErrorCode, out var result))
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
        if (Extensions.TryGetValue(ProblemConstants.ExtensionKeys.Errors, out var errors))
        {
            // Handle direct dictionary
            if (errors is Dictionary<string, List<string>> dict)
            {
                return dict;
            }
            
            // Handle JsonElement from deserialization
            if (errors is System.Text.Json.JsonElement jsonElement)
            {
                var result = new Dictionary<string, List<string>>();
                foreach (var property in jsonElement.EnumerateObject())
                {
                    var list = new List<string>();
                    if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            list.Add(item.GetString() ?? string.Empty);
                        }
                    }
                    result[property.Name] = list;
                }
                return result;
            }
        }

        return null;
    }
    
    /// <summary>
    ///     Adds a validation error for a specific field.
    /// </summary>
    public void AddValidationError(string field, string message)
    {
        if (!Extensions.TryGetValue(ProblemConstants.ExtensionKeys.Errors, out var errorsObj) || 
            errorsObj is not Dictionary<string, List<string>> errors)
        {
            errors = new Dictionary<string, List<string>>();
            Extensions[ProblemConstants.ExtensionKeys.Errors] = errors;
        }

        if (!errors.TryGetValue(field, out var fieldErrors))
        {
            fieldErrors = new List<string>();
            errors[field] = fieldErrors;
        }

        fieldErrors.Add(message);
    }

    public void AddValidationError(string message)
    {
        const string field = ProblemConstants.ValidationFields.General;
        AddValidationError(field, message);
    }

    /// <summary>
    ///     Gets or creates validation errors dictionary.
    /// </summary>
    private Dictionary<string, List<string>> GetOrCreateValidationErrors()
    {
        if (!Extensions.TryGetValue(ProblemConstants.ExtensionKeys.Errors, out var errorsObj) || 
            errorsObj is not Dictionary<string, List<string>> errors)
        {
            errors = new Dictionary<string, List<string>>();
            Extensions[ProblemConstants.ExtensionKeys.Errors] = errors;
        }
        
        return errors;
    }

    public bool InvalidField(string fieldName)
    {
        var errors = GetValidationErrors();
        return errors?.ContainsKey(fieldName) ?? false;
    }

    public string InvalidFieldError(string fieldName)
    {
        var errors = GetValidationErrors();
        return errors?.TryGetValue(fieldName, out var fieldErrors) == true ? string.Join(", ", fieldErrors) : string.Empty;
    }

    /// <summary>
    ///     Creates a copy of this Problem with the specified extensions added.
    /// </summary>
    public Problem WithExtensions(IDictionary<string, object?> additionalExtensions)
    {
        var problem = new Problem
        {
            Type = Type,
            Title = Title,
            StatusCode = StatusCode,
            Detail = Detail,
            Instance = Instance
        };

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
        if (Extensions.TryGetValue(ProblemConstants.ExtensionKeys.OriginalExceptionType, out var originalTypeObj) && 
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
                    var message = !string.IsNullOrEmpty(Detail) ? Detail : 
                                  !string.IsNullOrEmpty(Title) ? Title : 
                                  "An error occurred";
                    if (Activator.CreateInstance(originalType, message) is Exception reconstructedException)
                    {
                        // Restore any exception data
                        foreach (var kvp in Extensions)
                        {
                            if (kvp.Key.StartsWith(ProblemConstants.ExtensionKeys.ExceptionDataPrefix))
                            {
                                var dataKey = kvp.Key.Substring(ProblemConstants.ExtensionKeys.ExceptionDataPrefix.Length);
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
namespace ManagedCode.Communication.Constants;

/// <summary>
/// Constants for Problem extension keys to avoid string literals.
/// </summary>
public static class ProblemExtensionKeys
{
    /// <summary>
    /// Key for validation errors in problem extensions
    /// </summary>
    public const string Errors = "errors";
    
    /// <summary>
    /// Key for error type (enum type name) in problem extensions
    /// </summary>
    public const string ErrorType = "errorType";
    
    /// <summary>
    /// Key for trace ID in problem extensions
    /// </summary>
    public const string TraceId = "traceId";
    
    /// <summary>
    /// Key for exception data prefix
    /// </summary>
    public const string ExceptionDataPrefix = "exception.";
}
namespace ManagedCode.Communication.Extensions.Constants;

/// <summary>
/// Constants for Problem details to avoid string literals throughout the codebase.
/// </summary>
public static class ProblemConstants
{
    /// <summary>
    /// Problem titles
    /// </summary>
    public static class Titles
    {
        /// <summary>
        /// Title for validation failure problems
        /// </summary>
        public const string ValidationFailed = "Validation failed";
        
        /// <summary>
        /// Title for unexpected error problems
        /// </summary>
        public const string UnexpectedError = "An unexpected error occurred";
    }
    
    /// <summary>
    /// Problem extension keys
    /// </summary>
    public static class ExtensionKeys
    {
        /// <summary>
        /// Key for validation errors in problem extensions
        /// </summary>
        public const string ValidationErrors = "validationErrors";
        
        /// <summary>
        /// Key for trace ID in problem extensions
        /// </summary>
        public const string TraceId = "traceId";
        
        /// <summary>
        /// Key for hub method name in problem extensions
        /// </summary>
        public const string HubMethod = "hubMethod";
        
        /// <summary>
        /// Key for hub type name in problem extensions
        /// </summary>
        public const string HubType = "hubType";
        
        /// <summary>
        /// Key for inner exception in problem extensions
        /// </summary>
        public const string InnerException = "innerException";
        
        /// <summary>
        /// Key for stack trace in problem extensions
        /// </summary>
        public const string StackTrace = "stackTrace";
    }
} 
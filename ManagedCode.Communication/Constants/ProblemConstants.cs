namespace ManagedCode.Communication.Constants;

/// <summary>
/// All constants related to Problem types.
/// </summary>
public static class ProblemConstants
{
    /// <summary>
    /// Standard problem titles for common HTTP status codes.
    /// </summary>
    public static class Titles
    {
        public const string Unauthorized = "Unauthorized";
        public const string Forbidden = "Forbidden";
        public const string NotFound = "Not Found";
        public const string ValidationFailed = "Validation Failed";
        public const string InternalServerError = "Internal Server Error";
        public const string BadRequest = "Bad Request";
        public const string Conflict = "Conflict";
        public const string UnprocessableEntity = "Unprocessable Entity";
        public const string TooManyRequests = "Too Many Requests";
        public const string ServiceUnavailable = "Service Unavailable";
        public const string GatewayTimeout = "Gateway Timeout";
        public const string Error = "Error";
    }

    /// <summary>
    /// Standard problem detail messages.
    /// </summary>
    public static class Messages
    {
        public const string BadRequest = "The request could not be understood by the server due to malformed syntax.";
        public const string UnauthorizedAccess = "Authentication is required to access this resource.";
        public const string ForbiddenAccess = "You do not have permission to access this resource.";
        public const string ResourceNotFound = "The requested resource was not found.";
        public const string ValidationErrors = "One or more validation errors occurred.";
        public const string GenericError = "An error occurred";
        public const string InvalidMessage = "Invalid";
    }

    /// <summary>
    /// Standard problem type URIs according to RFC 7807.
    /// </summary>
    public static class Types
    {
        public const string AboutBlank = "about:blank";
        public const string ValidationFailed = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

        public static string HttpStatus(int statusCode) => $"https://httpstatuses.io/{statusCode}";
    }

    /// <summary>
    /// Keys for Problem extensions dictionary to avoid string literals.
    /// </summary>
    public static class ExtensionKeys
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

        /// <summary>
        /// Key for error code in problem extensions
        /// </summary>
        public const string ErrorCode = "errorCode";

        /// <summary>
        /// Key for original exception type in problem extensions
        /// </summary>
        public const string OriginalExceptionType = "originalExceptionType";
    }

    /// <summary>
    /// Field names for validation errors.
    /// </summary>
    public static class ValidationFields
    {
        /// <summary>
        /// General field name for validation errors that don't belong to a specific field.
        /// </summary>
        public const string General = "_general";
    }
}

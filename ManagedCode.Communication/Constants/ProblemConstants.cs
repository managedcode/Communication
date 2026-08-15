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
        /// <summary>
        ///     Title for a request that carries no valid credentials (401).
        /// </summary>
        public const string Unauthorized = "Unauthorized";
        /// <summary>
        ///     Title for a request whose credentials are valid but insufficient (403).
        /// </summary>
        public const string Forbidden = "Forbidden";
        /// <summary>
        ///     Title for a resource that does not exist (404).
        /// </summary>
        public const string NotFound = "Not Found";
        /// <summary>
        ///     Title for a request whose contents failed validation (400/422).
        /// </summary>
        public const string ValidationFailed = "Validation Failed";
        /// <summary>
        ///     Title for a fault on the server side (500).
        /// </summary>
        public const string InternalServerError = "Internal Server Error";
        /// <summary>
        ///     Title for a malformed request (400).
        /// </summary>
        public const string BadRequest = "Bad Request";
        /// <summary>
        ///     Title for a request that conflicts with the current state (409).
        /// </summary>
        public const string Conflict = "Conflict";
        /// <summary>
        ///     Title for a well-formed request the server cannot act on (422).
        /// </summary>
        public const string UnprocessableEntity = "Unprocessable Entity";
        /// <summary>
        ///     Title for a rate-limited request (429).
        /// </summary>
        public const string TooManyRequests = "Too Many Requests";
        /// <summary>
        ///     Title for a temporarily unavailable service (503).
        /// </summary>
        public const string ServiceUnavailable = "Service Unavailable";
        /// <summary>
        ///     Title for an upstream dependency that did not answer in time (504).
        /// </summary>
        public const string GatewayTimeout = "Gateway Timeout";
        /// <summary>
        ///     Title used by the generic fallback problem, when nothing more specific is known.
        /// </summary>
        public const string Error = "Error";
    }

    /// <summary>
    /// Standard problem detail messages.
    /// </summary>
    public static class Messages
    {
        /// <summary>
        ///     Detail for a malformed request.
        /// </summary>
        public const string BadRequest = "The request could not be understood by the server due to malformed syntax.";
        /// <summary>
        ///     Detail for a request missing authentication.
        /// </summary>
        public const string UnauthorizedAccess = "Authentication is required to access this resource.";
        /// <summary>
        ///     Detail for a request that is authenticated but not permitted.
        /// </summary>
        public const string ForbiddenAccess = "You do not have permission to access this resource.";
        /// <summary>
        ///     Detail for a missing resource.
        /// </summary>
        public const string ResourceNotFound = "The requested resource was not found.";
        /// <summary>
        ///     Detail for a validation failure; the individual errors live in the <c>errors</c> extension.
        /// </summary>
        public const string ValidationErrors = "One or more validation errors occurred.";
        /// <summary>
        ///     Detail used by the generic fallback problem.
        /// </summary>
        public const string GenericError = "An error occurred";
        /// <summary>
        ///     Detail used when a value is rejected without a more specific reason.
        /// </summary>
        public const string InvalidMessage = "Invalid";
    }

    /// <summary>
    /// Standard problem type URIs according to RFC 7807.
    /// </summary>
    public static class Types
    {
        /// <summary>
        ///     The RFC 7807 default type, meaning "no more specific type is defined".
        /// </summary>
        public const string AboutBlank = "about:blank";
        /// <summary>
        ///     Type URI marking a validation failure. <c>Result.IsInvalid</c> tests for exactly this value.
        /// </summary>
        public const string ValidationFailed = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

        /// <summary>
        ///     Builds the type URI for a status code, e.g. <c>https://httpstatuses.io/404</c>.
        /// </summary>
        /// <param name="statusCode">HTTP status code.</param>
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

        /// <summary>
        /// Key for the enum member name behind an error code.
        /// </summary>
        public const string ErrorCodeName = "errorCodeName";

        /// <summary>
        /// Key for the numeric enum value behind an error code.
        /// </summary>
        public const string ErrorCodeValue = "errorCodeValue";

        /// <summary>
        /// Key for an inner exception description.
        /// </summary>
        public const string InnerException = "innerException";

        /// <summary>
        /// Key for a stack trace. Only attach this outside production.
        /// </summary>
        public const string StackTrace = "stackTrace";

        /// <summary>
        /// Key for the SignalR hub method name.
        /// </summary>
        public const string HubMethod = "hubMethod";

        /// <summary>
        /// Key for the SignalR hub type name.
        /// </summary>
        public const string HubType = "hubType";
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

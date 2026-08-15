namespace ManagedCode.Communication;

/// <summary>
///     Every member name this library puts on the wire, in one place.
/// </summary>
/// <remarks>
///     These are the JSON contract. They are applied through <c>[JsonPropertyName]</c> on every serialized member,
///     which outranks <c>JsonSerializerOptions.PropertyNamingPolicy</c> — so a document written by one service is
///     readable by another regardless of how either is configured. A member left without one silently becomes
///     policy-dependent, and two services that disagree then exchange documents that parse but lose data.
///     <para>
///         The hand-written converters read these same constants, so a converter and the attribute it stands in
///         for cannot drift apart.
///     </para>
///     <para>
///         Changing any value here is a breaking wire change.
///     </para>
/// </remarks>
public static class CommunicationJsonNames
{
    /// <summary>Whether the operation succeeded. On <c>Result</c>, <c>Result&lt;T&gt;</c>, <c>CollectionResult&lt;T&gt;</c>.</summary>
    public const string IsSuccess = "isSuccess";

    /// <summary>The payload of a successful <c>Result&lt;T&gt;</c>, or a command's argument.</summary>
    public const string Value = "value";

    /// <summary>The RFC 7807 failure carried by a failed result.</summary>
    public const string Problem = "problem";

    /// <summary>The items of a <c>CollectionResult&lt;T&gt;</c>.</summary>
    public const string Collection = "collection";

    /// <summary>One-based page number.</summary>
    public const string PageNumber = "pageNumber";

    /// <summary>Items per page.</summary>
    public const string PageSize = "pageSize";

    /// <summary>Total items across all pages.</summary>
    public const string TotalItems = "totalItems";

    /// <summary>Total number of pages.</summary>
    public const string TotalPages = "totalPages";

    /// <summary>Items to skip, on a pagination request.</summary>
    public const string Skip = "skip";

    /// <summary>Items to take, on a pagination request.</summary>
    public const string Take = "take";

    /// <summary>Lifecycle kind of a CQRS stream chunk.</summary>
    public const string Kind = "kind";

    /// <summary>In-flight payload of a CQRS stream chunk.</summary>
    public const string ProgressResult = "progressResult";

    /// <summary>Terminal payload of a CQRS stream chunk.</summary>
    public const string Final = "final";

    /// <summary>Human-readable message on a CQRS stream chunk.</summary>
    public const string Message = "message";

    /// <summary>SSE <c>event:</c> name, omitted when it is the default for the chunk's kind.</summary>
    public const string EventType = "eventType";

    /// <summary>SSE <c>id:</c> value.</summary>
    public const string EventId = "eventId";

    /// <summary>Monotonic position of a chunk within its stream.</summary>
    public const string Sequence = "sequence";

    /// <summary>UTC timestamp of a chunk.</summary>
    public const string TimestampUtc = "timestampUtc";

    // ------------------------------------------------------------------------------------------------
    // Problem (RFC 7807). These five names are fixed by the specification, not by this library.
    // ------------------------------------------------------------------------------------------------

    /// <summary>RFC 7807 <c>type</c> URI.</summary>
    public const string ProblemType = "type";

    /// <summary>RFC 7807 <c>title</c>.</summary>
    public const string ProblemTitle = "title";

    /// <summary>RFC 7807 <c>status</c> code.</summary>
    public const string ProblemStatus = "status";

    /// <summary>RFC 7807 <c>detail</c>.</summary>
    public const string ProblemDetail = "detail";

    /// <summary>RFC 7807 <c>instance</c>.</summary>
    public const string ProblemInstance = "instance";

    // ------------------------------------------------------------------------------------------------
    // Commands
    // ------------------------------------------------------------------------------------------------

    /// <summary>Idempotency key of a command.</summary>
    public const string CommandId = "commandId";

    /// <summary>Command type discriminator.</summary>
    public const string CommandType = "commandType";

    /// <summary>When the command was created.</summary>
    public const string Timestamp = "timestamp";

    /// <summary>Identifier shared by everything belonging to one business operation.</summary>
    public const string CorrelationId = "correlationId";

    /// <summary>Identifier of the message that caused this one.</summary>
    public const string CausationId = "causationId";

    /// <summary>W3C trace identifier.</summary>
    public const string TraceId = "traceId";

    /// <summary>W3C span identifier.</summary>
    public const string SpanId = "spanId";

    /// <summary>Who issued the command.</summary>
    public const string UserId = "userId";

    /// <summary>Session the command belongs to.</summary>
    public const string SessionId = "sessionId";

    /// <summary>Command metadata envelope.</summary>
    public const string Metadata = "metadata";

    // ------------------------------------------------------------------------------------------------
    // Command metadata
    // ------------------------------------------------------------------------------------------------

    /// <summary>Metadata schema version.</summary>
    public const string Version = "version";

    /// <summary>Free-form metadata properties.</summary>
    public const string Properties = "properties";

    /// <summary>Who or what initiated the command.</summary>
    public const string InitiatedBy = "initiatedBy";

    /// <summary>Originating system.</summary>
    public const string Source = "source";

    /// <summary>Destination system.</summary>
    public const string Target = "target";

    /// <summary>Caller IP address.</summary>
    public const string IpAddress = "ipAddress";

    /// <summary>Caller user agent.</summary>
    public const string UserAgent = "userAgent";

    /// <summary>Dispatch priority.</summary>
    public const string Priority = "priority";

    /// <summary>Attempts made so far.</summary>
    public const string RetryCount = "retryCount";

    /// <summary>Attempt ceiling.</summary>
    public const string MaxRetries = "maxRetries";

    /// <summary>Timeout in seconds.</summary>
    public const string TimeoutSeconds = "timeoutSeconds";

    /// <summary>How long execution took.</summary>
    public const string ExecutionTime = "executionTime";

    /// <summary>Time to live in seconds.</summary>
    public const string TimeToLiveSeconds = "timeToLiveSeconds";

    /// <summary>Free-form tags.</summary>
    public const string Tags = "tags";

    /// <summary>Free-form extensions.</summary>
    public const string Extensions = "extensions";
}

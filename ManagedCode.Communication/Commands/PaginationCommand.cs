using System;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

/// <summary>
/// Represents a command that carries pagination instructions.
/// </summary>
public sealed class PaginationCommand : Command<PaginationRequest>, ICommandValueFactory<PaginationCommand, PaginationRequest>
{
    private const string DefaultCommandType = "Pagination";

    [JsonConstructor]
    private PaginationCommand()
    {
        CommandType = DefaultCommandType;
    }

    private PaginationCommand(Guid commandId, string commandType, PaginationRequest payload)
        : base(commandId, commandType, payload)
    {
    }

    /// <summary>
    ///     Number of items to skip.
    /// </summary>
    public int Skip => Value?.Skip ?? 0;

    /// <summary>
    ///     Number of items to take.
    /// </summary>
    public int Take => Value?.Take ?? 0;

    /// <summary>
    ///     1-based page index derived from skip/take.
    /// </summary>
    public int PageNumber => Value?.PageNumber ?? 1;

    /// <summary>
    ///     Page size, equal to <c>Take</c>.
    /// </summary>
    public int PageSize => Value?.PageSize ?? 0;

    /// <summary>
    ///     Creates a command of the given type from a pagination payload.
    /// </summary>
    /// <param name="commandType">Logical command name.</param>
    /// <param name="value">Pagination payload.</param>
    /// <param name="commandId">
    ///     Identity of the command. Leave it unset — a time-ordered UUIDv7 is generated. Supply one only when the
    ///     identity comes from outside: an idempotency key sent by the caller, or a replayed message.
    /// </param>
    public static new PaginationCommand Create(string commandType, PaginationRequest value, Guid? commandId = null)
    {
        return Create(value, options: null, commandId);
    }

    /// <summary>
    ///     Creates a command from a pagination payload, normalizing it first.
    /// </summary>
    /// <param name="request">Pagination payload.</param>
    /// <param name="options">Optional normalization options.</param>
    /// <param name="commandId">
    ///     Identity of the command. Leave it unset — a time-ordered UUIDv7 is generated. Supply one only when the
    ///     identity comes from outside: an idempotency key sent by the caller, or a replayed message.
    /// </param>
    public static PaginationCommand Create(
        PaginationRequest request,
        PaginationOptions? options = null,
        Guid? commandId = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PaginationCommand(
            commandId ?? Guid.CreateVersion7(),
            DefaultCommandType,
            request.Normalize(options));
    }

    /// <summary>
    ///     Creates a command from skip/take parameters.
    /// </summary>
    /// <param name="skip">Items to skip.</param>
    /// <param name="take">Items to take.</param>
    /// <param name="options">Optional normalization options.</param>
    /// <param name="commandId">
    ///     Identity of the command. Leave it unset — a time-ordered UUIDv7 is generated. Supply one only when the
    ///     identity comes from outside: an idempotency key sent by the caller, or a replayed message.
    /// </param>
    public static PaginationCommand Create(
        int skip,
        int take,
        PaginationOptions? options = null,
        Guid? commandId = null)
    {
        return Create(new PaginationRequest(skip, take), options, commandId);
    }

    /// <inheritdoc cref="Create(PaginationRequest,PaginationOptions,Guid?)" />
    public static PaginationCommand From(
        PaginationRequest request,
        PaginationOptions? options = null,
        Guid? commandId = null)
    {
        return Create(request, options, commandId);
    }

    /// <inheritdoc cref="Create(int,int,PaginationOptions,Guid?)" />
    public static PaginationCommand From(
        int skip,
        int take,
        PaginationOptions? options = null,
        Guid? commandId = null)
    {
        return Create(skip, take, options, commandId);
    }

    /// <summary>
    /// Creates a command from 1-based page values.
    /// </summary>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Requested page size.</param>
    /// <param name="options">Optional normalization options.</param>
    public static PaginationCommand FromPage(int pageNumber, int pageSize, PaginationOptions? options = null)
    {
        return Create(PaginationRequest.FromPage(pageNumber, pageSize, options), options);
    }
}

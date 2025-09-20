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

    public int Skip => Value?.Skip ?? 0;

    public int Take => Value?.Take ?? 0;

    public int PageNumber => Value?.PageNumber ?? 1;

    public int PageSize => Value?.PageSize ?? 0;

    /// <summary>
    /// Creates a command with an explicit identifier, command type, and normalized pagination payload.
    /// </summary>
    /// <param name="commandId">Unique command identifier.</param>
    /// <param name="commandType">Logical command name.</param>
    /// <param name="value">Pagination payload.</param>
    /// <param name="options">Optional normalization options.</param>
    public static new PaginationCommand Create(Guid commandId, string commandType, PaginationRequest value)
    {
        return Create(commandId, commandType, value, options: null);
    }

    /// <summary>
    /// Creates a command with an explicit identifier, command type, and normalized pagination payload.
    /// </summary>
    /// <param name="commandId">Unique command identifier.</param>
    /// <param name="commandType">Logical command name.</param>
    /// <param name="value">Pagination payload.</param>
    /// <param name="options">Optional normalization options.</param>
    public static PaginationCommand Create(Guid commandId, string commandType, PaginationRequest value, PaginationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalizedCommandType = string.IsNullOrWhiteSpace(commandType) ? DefaultCommandType : commandType;

        if (!string.Equals(normalizedCommandType, DefaultCommandType, StringComparison.Ordinal))
        {
            normalizedCommandType = DefaultCommandType;
        }

        var normalizedPayload = value.Normalize(options);

        return new PaginationCommand(commandId, normalizedCommandType, normalizedPayload);
    }

    /// <summary>
    /// Creates a command with a generated identifier from the supplied pagination request.
    /// </summary>
    /// <param name="request">Pagination payload.</param>
    /// <param name="options">Optional normalization options.</param>
    public static PaginationCommand Create(PaginationRequest request, PaginationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = request.Normalize(options);
        return Create(Guid.CreateVersion7(), DefaultCommandType, normalized, options);
    }

    /// <summary>
    /// Creates a command using the provided identifier and pagination request.
    /// </summary>
    /// <param name="commandId">Unique command identifier.</param>
    /// <param name="request">Pagination payload.</param>
    /// <param name="options">Optional normalization options.</param>
    public static PaginationCommand Create(Guid commandId, PaginationRequest request, PaginationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = request.Normalize(options);
        return Create(commandId, DefaultCommandType, normalized, options);
    }

    /// <summary>
    /// Creates a command from skip/take parameters.
    /// </summary>
    /// <param name="skip">Items to skip.</param>
    /// <param name="take">Items to take.</param>
    /// <param name="options">Optional normalization options.</param>
    public static PaginationCommand Create(int skip, int take, PaginationOptions? options = null)
    {
        return Create(new PaginationRequest(skip, take), options);
    }

    /// <summary>
    /// Creates a command with an explicit identifier from skip/take parameters.
    /// </summary>
    /// <param name="commandId">Unique command identifier.</param>
    /// <param name="skip">Items to skip.</param>
    /// <param name="take">Items to take.</param>
    /// <param name="options">Optional normalization options.</param>
    public static PaginationCommand Create(Guid commandId, int skip, int take, PaginationOptions? options = null)
    {
        return Create(commandId, new PaginationRequest(skip, take), options);
    }

    /// <summary>
    /// Creates a command from the provided pagination payload, preserving compatibility with legacy factory syntax.
    /// </summary>
    /// <param name="request">Pagination payload.</param>
    /// <param name="options">Optional normalization options.</param>
    public static PaginationCommand From(PaginationRequest request, PaginationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = request.Normalize(options);
        return Create(normalized, options);
    }

    /// <summary>
    /// Creates a command from skip/take parameters using legacy naming.
    /// </summary>
    /// <param name="skip">Items to skip.</param>
    /// <param name="take">Items to take.</param>
    /// <param name="options">Optional normalization options.</param>
    public static PaginationCommand From(int skip, int take, PaginationOptions? options = null)
    {
        return Create(skip, take, options);
    }

    /// <summary>
    /// Creates a command from skip/take parameters with an explicit identifier using legacy naming.
    /// </summary>
    /// <param name="commandId">Unique command identifier.</param>
    /// <param name="skip">Items to skip.</param>
    /// <param name="take">Items to take.</param>
    /// <param name="options">Optional normalization options.</param>
    public static PaginationCommand From(Guid commandId, int skip, int take, PaginationOptions? options = null)
    {
        return Create(commandId, new PaginationRequest(skip, take), options);
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

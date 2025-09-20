using System;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

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

    public static new PaginationCommand Create(Guid commandId, string commandType, PaginationRequest value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalizedCommandType = string.IsNullOrWhiteSpace(commandType) ? DefaultCommandType : commandType;

        if (!string.Equals(normalizedCommandType, DefaultCommandType, StringComparison.Ordinal))
        {
            normalizedCommandType = DefaultCommandType;
        }

        return new PaginationCommand(commandId, normalizedCommandType, value);
    }

    public static new PaginationCommand Create(PaginationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CommandValueFactoryBridge.Create<PaginationCommand, PaginationRequest>(request);
    }

    public static new PaginationCommand Create(Guid commandId, PaginationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CommandValueFactoryBridge.Create<PaginationCommand, PaginationRequest>(commandId, request);
    }

    public static PaginationCommand Create(int skip, int take)
    {
        return Create(new PaginationRequest(skip, take));
    }

    public static PaginationCommand Create(Guid commandId, int skip, int take)
    {
        return Create(commandId, new PaginationRequest(skip, take));
    }

    public static new PaginationCommand From(PaginationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CommandValueFactoryBridge.From<PaginationCommand, PaginationRequest>(request);
    }

    public static PaginationCommand From(int skip, int take)
    {
        return From(new PaginationRequest(skip, take));
    }

    public static PaginationCommand From(Guid commandId, int skip, int take)
    {
        return CommandValueFactoryBridge.From<PaginationCommand, PaginationRequest>(commandId, new PaginationRequest(skip, take));
    }

    public static PaginationCommand FromPage(int pageNumber, int pageSize)
    {
        return From(PaginationRequest.FromPage(pageNumber, pageSize));
    }
}

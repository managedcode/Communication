using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;

namespace ManagedCode.Communication.AspNetCore;

public interface ICommandIdempotencyStore
{
    Task<CommandExecutionStatus> GetCommandStatusAsync(string commandId, CancellationToken cancellationToken = default);
    Task SetCommandStatusAsync(string commandId, CommandExecutionStatus status, CancellationToken cancellationToken = default);
    Task<T?> GetCommandResultAsync<T>(string commandId, CancellationToken cancellationToken = default);
    Task SetCommandResultAsync<T>(string commandId, T result, CancellationToken cancellationToken = default);
    Task RemoveCommandAsync(string commandId, CancellationToken cancellationToken = default);
}
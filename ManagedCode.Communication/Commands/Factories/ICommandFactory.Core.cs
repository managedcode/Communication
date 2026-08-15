using System;

namespace ManagedCode.Communication.Commands;

/// <summary>
///     The factory surface every command type implements.
/// </summary>
public partial interface ICommandFactory<TSelf>
    where TSelf : class, ICommandFactory<TSelf>
{
    /// <summary>
    ///     Creates a command of the given type.
    /// </summary>
    /// <param name="commandType">What the command is.</param>
    /// <param name="commandId">
    ///     Identity of the command. Leave it unset and a time-ordered UUIDv7 is generated. Supply one only when
    ///     the identity comes from somewhere else — an idempotency key sent by the caller, or a replayed message.
    /// </param>
    static abstract TSelf Create(string commandType, Guid? commandId = null);
}

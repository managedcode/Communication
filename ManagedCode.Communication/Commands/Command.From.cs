using System;

namespace ManagedCode.Communication.Commands;

public partial class Command
{
    /// <summary>
    ///     Creates a typed command carrying <paramref name="value" />, naming it after the payload's runtime type.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="value">The payload.</param>
    /// <param name="commandId">
    ///     Identity of the command. Leave it unset — a time-ordered UUIDv7 is generated. Supply one only when the
    ///     identity comes from outside: an idempotency key sent by the caller, or a replayed message.
    /// </param>
    public static Command<T> From<T>(T value, Guid? commandId = null)
    {
        return Command<T>.From(value, commandId);
    }

    /// <summary>
    ///     Creates a typed command of the given type carrying <paramref name="value" />.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="commandType">Logical command type.</param>
    /// <param name="value">The payload.</param>
    /// <param name="commandId">
    ///     Identity of the command. Leave it unset — a time-ordered UUIDv7 is generated. Supply one only when the
    ///     identity comes from outside: an idempotency key sent by the caller, or a replayed message.
    /// </param>
    public static Command<T> From<T>(string commandType, T value, Guid? commandId = null)
    {
        return Command<T>.From(commandType, value, commandId);
    }
}

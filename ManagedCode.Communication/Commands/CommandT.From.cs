using System;

namespace ManagedCode.Communication.Commands;

public partial class Command<T>
{
    /// <summary>
    ///     Creates a command carrying <paramref name="value" />, naming it after the payload's runtime type.
    /// </summary>
    /// <param name="value">The payload.</param>
    /// <param name="commandId">
    ///     Identity of the command. Leave it unset — a time-ordered UUIDv7 is generated. Supply one only when the
    ///     identity comes from outside: an idempotency key sent by the caller, or a replayed message.
    /// </param>
    public static Command<T> Create(T value, Guid? commandId = null)
    {
        return Create(ResolveCommandType(value), value, commandId);
    }

    /// <summary>
    ///     Creates a command of the given type carrying <paramref name="value" />.
    /// </summary>
    /// <param name="commandType">Logical command type.</param>
    /// <param name="value">The payload.</param>
    /// <param name="commandId">
    ///     Identity of the command. Leave it unset — a time-ordered UUIDv7 is generated. Supply one only when the
    ///     identity comes from outside: an idempotency key sent by the caller, or a replayed message.
    /// </param>
    public static Command<T> Create(string commandType, T value, Guid? commandId = null)
    {
        if (string.IsNullOrWhiteSpace(commandType))
        {
            throw new ArgumentException("Command type must be provided.", nameof(commandType));
        }

        return new Command<T>(commandId ?? Guid.CreateVersion7(), commandType, value);
    }

    /// <summary>
    ///     Creates a command of the given type, obtaining the payload from <paramref name="valueFactory" />.
    /// </summary>
    /// <param name="commandType">Logical command type.</param>
    /// <param name="valueFactory">Produces the payload.</param>
    /// <param name="commandId">
    ///     Identity of the command. Leave it unset — a time-ordered UUIDv7 is generated. Supply one only when the
    ///     identity comes from outside: an idempotency key sent by the caller, or a replayed message.
    /// </param>
    public static Command<T> Create(string commandType, Func<T> valueFactory, Guid? commandId = null)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        return Create(commandType, valueFactory(), commandId);
    }

    /// <summary>
    ///     Creates a command from <paramref name="valueFactory" />, naming it after the payload's runtime type.
    /// </summary>
    /// <param name="valueFactory">Produces the payload.</param>
    /// <param name="commandId">
    ///     Identity of the command. Leave it unset — a time-ordered UUIDv7 is generated. Supply one only when the
    ///     identity comes from outside: an idempotency key sent by the caller, or a replayed message.
    /// </param>
    public static Command<T> Create(Func<T> valueFactory, Guid? commandId = null)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        return Create(valueFactory(), commandId);
    }

    // From(...) mirrors the naming used by Result.From and Command.From.

    /// <inheritdoc cref="Create(T,Guid?)" />
    public static Command<T> From(T value, Guid? commandId = null)
    {
        return Create(value, commandId);
    }

    /// <inheritdoc cref="Create(string,T,Guid?)" />
    public static Command<T> From(string commandType, T value, Guid? commandId = null)
    {
        return Create(commandType, value, commandId);
    }

    private static string ResolveCommandType(T value)
    {
        return value?.GetType().Name ?? typeof(T).Name;
    }
}

using System;

namespace ManagedCode.Communication.Orleans.RateLimiting;

/// <summary>
///     Maps Communication command metadata into Orleans.RateLimiting orchestration context.
/// </summary>
public sealed class OrleansCommandRateLimiterOptions
{
    /// <summary>Resolves the Orleans rate-limit policy name.</summary>
    public Func<ICommand, string?> PolicyName { get; set; } = static _ => null;

    /// <summary>Resolves a tenant partition.</summary>
    public Func<ICommand, string?> TenantId { get; set; } = static command =>
        ReadMetadata(command, "tenantId");

    /// <summary>Resolves a group partition.</summary>
    public Func<ICommand, string?> GroupId { get; set; } = static command => command.SessionId;

    /// <summary>Resolves a role partition.</summary>
    public Func<ICommand, string?> Role { get; set; } = static command =>
        ReadMetadata(command, "role");

    /// <summary>Resolves a resource partition.</summary>
    public Func<ICommand, string?> Resource { get; set; } = static command => command.Metadata?.Target;

    private static string? ReadMetadata(ICommand command, string key)
    {
        if (command.Metadata?.Properties.TryGetValue(key, out var property) == true)
        {
            return property?.ToString();
        }

        return command.Metadata?.Extensions.TryGetValue(key, out var extension) == true
            ? extension?.ToString()
            : null;
    }
}

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace ManagedCode.Communication.Orleans.RateLimiting;

/// <summary>
///     Maps Communication command metadata into Orleans.RateLimiting orchestration context.
/// </summary>
public sealed class OrleansCommandRateLimiterOptions
{
    /// <summary>Maximum time spent tracking a cancelled backend acquisition before disposing its holder.</summary>
    public TimeSpan CancellationCleanupTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Resolves the Orleans rate-limit policy name.</summary>
    public Func<ICommand, string?> PolicyName { get; set; } = static _ => null;

    /// <summary>Resolves a trusted authenticated actor identifier.</summary>
    public Func<ICommand, string?> UserId { get; set; } = static _ => null;

    /// <summary>Resolves a tenant partition.</summary>
    public Func<ICommand, string?> TenantId { get; set; } = static _ => null;

    /// <summary>Resolves a group partition.</summary>
    public Func<ICommand, string?> GroupId { get; set; } = static _ => null;

    /// <summary>Resolves a role partition.</summary>
    public Func<ICommand, string?> Role { get; set; } = static _ => null;

    /// <summary>Resolves a resource partition.</summary>
    public Func<ICommand, string?> Resource { get; set; } = static _ => null;

    /// <summary>Resolves a trusted client network address.</summary>
    public Func<ICommand, string?> IpAddress { get; set; } = static _ => null;

    /// <summary>Resolves bounded trusted metadata forwarded to the distributed limiter.</summary>
    public Func<ICommand, IReadOnlyDictionary<string, string>> Metadata { get; set; } =
        static _ => EmptyMetadata;

    private static IReadOnlyDictionary<string, string> EmptyMetadata { get; } =
        FrozenDictionary<string, string>.Empty;

    internal static OrleansCommandRateLimiterOptions CreateSnapshot(OrleansCommandRateLimiterOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new OrleansCommandRateLimiterOptions
        {
            CancellationCleanupTimeout = source.CancellationCleanupTimeout,
            PolicyName = source.PolicyName,
            UserId = source.UserId,
            TenantId = source.TenantId,
            GroupId = source.GroupId,
            Role = source.Role,
            Resource = source.Resource,
            IpAddress = source.IpAddress,
            Metadata = source.Metadata
        };
    }
}

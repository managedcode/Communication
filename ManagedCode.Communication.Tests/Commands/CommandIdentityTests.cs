using System;
using System.Linq;
using ManagedCode.Communication.Commands;
using Shouldly;

namespace ManagedCode.Communication.Tests.Commands;

/// <summary>
///     How a command gets its identity.
/// </summary>
/// <remarks>
///     Every factory generates the id itself and exposes <c>commandId</c> as an optional trailing parameter.
///     It used to be the <em>first</em> parameter of a parallel set of overloads, which read as required and
///     invited callers — and code generators — to pass a fresh <c>Guid.NewGuid()</c> at every call site. That is
///     noise at best; on a retry path it silently defeats idempotency, because the "same" command arrives with a
///     new identity each time.
/// </remarks>
public class CommandIdentityTests
{
    [Test]
    public void ACommandGeneratesItsOwnIdentity()
    {
        var command = Command.Create("PlaceOrder");

        command.CommandId.ShouldNotBe(Guid.Empty);
        command.CommandType.ShouldBe("PlaceOrder");
    }

    [Test]
    public void ATypedCommandGeneratesItsOwnIdentity()
    {
        var command = Command<string>.From("payload");

        command.CommandId.ShouldNotBe(Guid.Empty);
        command.Value.ShouldBe("payload");
    }

    [Test]
    public void GeneratedIdentitiesAreUniqueAndVersion7()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => Command.Create("X").CommandId).ToArray();

        ids.Distinct().Count().ShouldBe(ids.Length);

        // UUIDv7 embeds a millisecond timestamp in its leading bits, which is what makes these pleasant as
        // database keys. Deliberately not asserting that the sequence sorts: within a single millisecond the
        // remaining bits are random, so ordering is only guaranteed across milliseconds — and Guid.CompareTo
        // does not compare in byte order anyway.
        foreach (var id in ids)
        {
            id.Version.ShouldBe(7);
        }
    }

    [Test]
    public void GeneratedIdentitiesCarryTheCurrentTimestamp()
    {
        var before = DateTime.UtcNow.AddSeconds(-5);

        var bytes = Command.Create("X").CommandId.ToByteArray(bigEndian: true);
        var milliseconds = ((long)bytes[0] << 40) | ((long)bytes[1] << 32) | ((long)bytes[2] << 24) |
                           ((long)bytes[3] << 16) | ((long)bytes[4] << 8) | bytes[5];
        var timestamp = DateTime.UnixEpoch.AddMilliseconds(milliseconds);

        timestamp.ShouldBeGreaterThan(before);
        timestamp.ShouldBeLessThan(DateTime.UtcNow.AddSeconds(5));
    }

    [Test]
    public void AnExplicitIdentityIsHonouredWhenItComesFromOutside()
    {
        // The case the trailing parameter exists for: an idempotency key supplied by the caller, or a replayed
        // message whose identity must be preserved.
        var idempotencyKey = Guid.CreateVersion7();

        Command.Create("PlaceOrder", idempotencyKey).CommandId.ShouldBe(idempotencyKey);
        Command<string>.From("payload", idempotencyKey).CommandId.ShouldBe(idempotencyKey);
        Command<string>.Create("Custom", "payload", idempotencyKey).CommandId.ShouldBe(idempotencyKey);
        Command.From("Custom", "payload", idempotencyKey).CommandId.ShouldBe(idempotencyKey);
    }

    [Test]
    public void PaginationCommandsBehaveTheSameWay()
    {
        var generated = PaginationCommand.Create(skip: 10, take: 5);
        generated.CommandId.ShouldNotBe(Guid.Empty);

        var supplied = Guid.CreateVersion7();
        PaginationCommand.Create(skip: 10, take: 5, options: null, commandId: supplied)
            .CommandId.ShouldBe(supplied);
        PaginationCommand.From(new PaginationRequest(0, 10), options: null, commandId: supplied)
            .CommandId.ShouldBe(supplied);
    }

    [Test]
    public void EnumCommandTypesGenerateAnIdentityToo()
    {
        var command = Command.Create(SampleCommandType.Ship);

        command.CommandId.ShouldNotBe(Guid.Empty);
        command.CommandType.ShouldBe(nameof(SampleCommandType.Ship));
    }

    [Test]
    public void CorrelationAndCausationAreNotGenerated()
    {
        // Only the command id is generated. Correlation and causation describe how this command relates to
        // others, which the library cannot infer — they stay null until the caller sets them.
        var command = Command.Create("PlaceOrder");

        command.CorrelationId.ShouldBeNull();
        command.CausationId.ShouldBeNull();
        command.TraceId.ShouldBeNull();
        command.SpanId.ShouldBeNull();
        command.UserId.ShouldBeNull();
        command.SessionId.ShouldBeNull();
    }

    [Test]
    public void CorrelationAndCausationAreSetThroughTheFluentBuilders()
    {
        var command = Command.Create("PlaceOrder")
            .WithCorrelationId("correlation-1")
            .WithCausationId("parent-1");

        command.CorrelationId.ShouldBe("correlation-1");
        command.CausationId.ShouldBe("parent-1");
    }

    [Test]
    public void TheCommandTypeIsStillRequired()
    {
        Should.Throw<ArgumentException>(() => Command.Create(string.Empty));
        Should.Throw<ArgumentException>(() => Command<string>.Create(string.Empty, "value"));
    }

    private enum SampleCommandType
    {
        Ship
    }
}

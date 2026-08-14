namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>Progress payload shared by every CQRS test.</summary>
public sealed record ProgressUpdate(string State);

/// <summary>Terminal payload shared by every CQRS test.</summary>
public sealed record FinalResult(string Status);

/// <summary>Request body used by POST-with-body tests.</summary>
public sealed record SubmitCommand(string Payload);

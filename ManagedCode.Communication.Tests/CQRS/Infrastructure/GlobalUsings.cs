// A closed alias for the chunk type used across the CQRS tests. Without it every signature in the suite
// repeats CqrsStreamChunk<ProgressUpdate, FinalResult>, which drowns out what each test is actually asserting.
global using Chunk = ManagedCode.Communication.CQRS.CqrsStreamChunk<
    ManagedCode.Communication.Tests.CQRS.ProgressUpdate,
    ManagedCode.Communication.Tests.CQRS.FinalResult>;

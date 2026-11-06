using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Analyzers;
using Microsoft.CodeAnalysis;
using Shouldly;

namespace ManagedCode.Communication.Tests.Analyzers;

public sealed partial class CommandResultSeparationAnalyzerTests
{
    private const int CyclicGraphDepth = 28;
    private const int CyclicGraphTimeoutMilliseconds = 60_000;
    private const string CyclicGraphLeaf = "public sealed class SharedPayload{0} {{ public int Amount {{ get; init; }} public SharedPayload0? Back {{ get; init; }} }}";
    private const string ClosedGenericMembers = "public sealed class Owner<T> { public sealed class Payload { public T? Value { get; set; } } } public sealed record Output(Owner<string>.Payload Safe, Owner<ICommand>.Payload Work); public static Result<Output> Execute() => Result<Output>.Fail();";
    private const string ExpandingGenericRevisit = "public sealed class Owner<T> { public sealed class Payload { public T? Value { get; set; } public Link? Next { get; set; } } } public sealed class Link { public Owner<ICommand>.Payload? Work { get; set; } } public sealed record Output(Owner<string>.Payload First, Link Direct); public static Result<Output> Execute() => Result<Output>.Fail();";
    private const string ReverseExpandingGenericRevisit = "public sealed class Owner<T> { public sealed class Payload { public T? Value { get; set; } public Link? Next { get; set; } } } public sealed class Link { public Owner<IResult<int>>.Payload? Previous { get; set; } } public sealed record Request(Owner<string>.Payload First, Link Direct); public static Command<Request> Execute() => new();";

    [Test]
    [Timeout(CyclicGraphTimeoutMilliseconds)]
    public async Task SharedCyclicPayloadDiamond_CompilerAccepts_ExecutesTerminalResult(CancellationToken cancellationToken)
    {
        var nodes = string.Concat(Enumerable.Range(0, CyclicGraphDepth).Select(index =>
            string.Format(CultureInfo.InvariantCulture, SharedGraphNode, index, index + 1)));
        var leaf = string.Format(CultureInfo.InvariantCulture, CyclicGraphLeaf, CyclicGraphDepth);
        await ExecuteValidFlow(nodes + leaf + SharedGraphExecute, cancellationToken);
    }

    [Test]
    [Arguments(ClosedGenericMembers)]
    [Arguments(ExpandingGenericRevisit)]
    [Arguments(ReverseExpandingGenericRevisit)]
    public async Task GenericPayloadPaths_CompilerRejectsHiddenEnvelopes_SeparateFlowExecutes(string source)
    {
        var compilation = CreateCompilation(source);
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostics = await Analyze(compilation);
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CommandResultSeparationAnalyzer.DiagnosticId &&
            diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Location.IsInSource);
        await ExecuteValidFlow();
    }
}

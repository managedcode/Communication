using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using ManagedCode.Communication.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shouldly;

namespace ManagedCode.Communication.Tests.Analyzers;

public sealed class CommandResultSeparationAnalyzerTests
{
    private const string AssemblyName = "CommunicationEnvelopeSeparationRegression";
    private const string MetadataAssemblyName = "CommunicationEnvelopePayloadMetadata";
    private const string TrustedAssembliesKey = "TRUSTED_PLATFORM_ASSEMBLIES";
    private const string EntryType = "Scenario";
    private const string EntryMethod = "Execute";
    private const string Prefix = """
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using ManagedCode.Communication;
        using ManagedCode.Communication.Commands;
        using ManagedCode.Communication.CollectionResultT;
        public static class Scenario
        {
            public sealed record Input(int Amount);
        """;
    private const string Suffix = "}";
    private const string ExplicitResult = "public static Result<Command<Input>> Execute() => Result<Command<Input>>.Fail();";
    private const string CommandInterface = "public static IResult<ICommand> Execute() => Result<ICommand>.Fail();";
    private const string TypedCommandInterface = "public static IResult<ICommand<Input>> Execute() => Result<ICommand<Input>>.Fail();";
    private const string CollectionResult = "public static CollectionResult<Command<Input>> Execute() => CollectionResult<Command<Input>>.Fail();";
    private const string CollectionInterface = "public static IResultCollection<ICommand> Execute() => CollectionResult<ICommand>.Fail();";
    private const string ArrayPayload = "public static Result<Command<Input>[]> Execute() => Result<Command<Input>[]>.Fail();";
    private const string NestedList = "public static Result<Dictionary<int, List<Command<Input>>>> Execute() => Result<Dictionary<int, List<Command<Input>>>>.Fail();";
    private const string EnumerablePayload = "public static Result<IEnumerable<ICommand>> Execute() => Result<IEnumerable<ICommand>>.Fail();";
    private const string TuplePayload = "public static Result<(int Number, Command<Input> Work)> Execute() => Result<(int, Command<Input>)>.Fail();";
    private const string NestedProperty = "public sealed record Output(List<Command<Input>> Work); public static Result<Output> Execute() => Result<Output>.Fail();";
    private const string InheritedField = "public class Parent { public ICommand Work = new Command(); } public sealed class Output : Parent { } public static Result<Output> Execute() => Result<Output>.Fail();";
    private const string RecursiveProperty = "public sealed class Output { public Output? Next { get; set; } public ICommand? Work { get; set; } } public static Result<Output> Execute() => Result<Output>.Fail();";
    private const string InferredFactory = "public static object Execute() { var work = Command.From(new Input(1)); var result = Result.Succeed(work); return result; }";
    private const string ErasedFactory = "public static object Execute() => Result<object>.Succeed(Command.From(new Input(1)));";
    private const string ErasedArray = "public static object Execute() => Result<object>.Succeed(new[] { Command.From(new Input(1)) });";
    private const string Initializer = "public static object Execute() => new Result<object> { Value = Command.From(new Input(1)) };";
    private const string ErasedFactoryCallback = "public static object Execute() => Result<object>.From((Func<object>)(() => Command.From(new Input(1))));";
    private const string ReverseFactoryCallback = "public static object Execute() => Command<object>.Create((Func<object>)(() => Result.Succeed(1)));";
    private const string ConditionalErasure = "public static object Execute(bool choose) => Result<object>.Succeed(choose ? (object)Command.From(new Input(1)) : 1);";
    private const string InheritedInterface = "public interface Parent { ICommand Work { get; } } public interface Output : Parent { } public static Result<Output> Execute() => Result<Output>.Fail();";
    private const string ConstrainedDto = "public class Output { public ICommand? Work { get; set; } } public static Result<T> Execute<T>() where T : Output => Result<T>.Fail();";
    private const string AnonymousPayload = "public static object Execute() => Result.Succeed(new { Work = Command.From(new Input(1)) });";
    private const string ReversePayload = "public static Command<Result<int>> Execute() => Command.From(Result.Succeed(1));";
    private const string ReverseInterface = "public static ICommand<IResult<int>> Execute() => Command<IResult<int>>.Create(Result.Succeed(1));";
    private const string ReverseNullable = "public static Command<Result<int>?> Execute() => new();";
    private const string ReverseErased = "public static object Execute() => Command<object>.Create(Result.Succeed(1));";
    private const string ReverseNested = "public sealed record Request(List<IResult<int>> Previous); public static Command<Request> Execute() => new();";
    private const string ConstrainedCommand = "public static Result<T> Execute<T>() where T : ICommand => Result<T>.Fail();";
    private const string ConstrainedResult = "public static Command<T> Execute<T>() where T : IResult => new();";
    private const string Suppressed = """
        #pragma warning disable MCC0003
        public static Result<Command<Input>> Execute() => Result<Command<Input>>.Fail();
        #pragma warning restore MCC0003
        """;
    private const string MetadataPayload = """
        using ManagedCode.Communication;
        namespace Dependency;
        public sealed record Payload(ICommand Work);
        """;
    private const string MetadataConsumer = "public static Result<Dependency.Payload> Execute() => Result<Dependency.Payload>.Fail();";
    private const int SharedGraphDepth = 18;
    private const string SharedGraphNode = "public sealed class SharedPayload{0} {{ public int Amount {{ get; init; }} public SharedPayload{1}? Left {{ get; init; }} public SharedPayload{1}? Right {{ get; init; }} }}";
    private const string SharedGraphLeaf = "public sealed class SharedPayload{0} {{ public int Amount {{ get; init; }} }}";
    private const string SharedGraphExecute = "public static bool Execute() { var command = Command.From(new Input(2)); if (command.Value is not { } input) return false; var result = Result.Succeed(new SharedPayload0 { Amount = input.Amount + 3 }); return result.IsSuccess && result.Value.Amount == 5; }";
    private const string CyclicSharedPayload = "public sealed class Left { public Right? Next { get; set; } public ICommand? Work { get; set; } } public sealed class Right { public Left? Next { get; set; } } public static Result<Left> ReadLeft() => Result<Left>.Fail(); public static Result<Right> ReadRight() => Result<Right>.Fail();";
    private const string LeftPayload = "Result<Left>";
    private const string RightPayload = "Result<Right>";

    private const string ValidFlow = """
        public sealed record Output(int Amount);
        public sealed record CommandStatus(int Code);
        public sealed record Command(int Amount);
        public sealed class Recursive<T>
        {
            public Recursive<Recursive<T>>? Next { get; set; }
            public T? Value { get; set; }
        }
        private static Result<Output> Handle(ManagedCode.Communication.Commands.Command<Input> command)
        {
            return command.Value is { Amount: > 0 } value
                ? Result.Succeed(new Output(value.Amount + 1))
                : Result<Output>.FailBadRequest();
        }
        private static Result<Output> Invoke(
            Func<ManagedCode.Communication.Commands.Command<Input>, Result<Output>> handler,
            ManagedCode.Communication.Commands.Command<Input> command) => handler(command);
        public static bool Execute()
        {
            var input = ManagedCode.Communication.Commands.Command.From(new Input(2));
            var success = Invoke(Handle, input);
            success.ThrowIfProblem();
            if (success.Value.Amount != 3 || input.Value?.Amount != 2) { return false; }
            var failure = Invoke(Handle, ManagedCode.Communication.Commands.Command.From(new Input(0)));
            if (failure.IsSuccess || failure.Problem.StatusCode != 400) { return false; }
            var status = Result.Succeed(new CommandStatus(4));
            status.ThrowIfProblem();
            if (status.Value.Code != 4) { return false; }
            var namesake = Result.Succeed(new Command(5));
            namesake.ThrowIfProblem();
            if (namesake.Value.Amount != 5) { return false; }
            var recursive = Result.Succeed(new Recursive<Output> { Value = new Output(6) });
            recursive.ThrowIfProblem();
            return recursive.Value.Value?.Amount == 6;
        }
        """;

    [Test]
    [Arguments(ExplicitResult)]
    [Arguments(CommandInterface)]
    [Arguments(TypedCommandInterface)]
    [Arguments(CollectionResult)]
    [Arguments(CollectionInterface)]
    [Arguments(ArrayPayload)]
    [Arguments(NestedList)]
    [Arguments(EnumerablePayload)]
    [Arguments(TuplePayload)]
    [Arguments(NestedProperty)]
    [Arguments(InheritedField)]
    [Arguments(RecursiveProperty)]
    [Arguments(InferredFactory)]
    [Arguments(ErasedFactory)]
    [Arguments(ErasedArray)]
    [Arguments(Initializer)]
    [Arguments(AnonymousPayload)]
    [Arguments(ErasedFactoryCallback)]
    [Arguments(ReverseFactoryCallback)]
    [Arguments(ConditionalErasure)]
    [Arguments(InheritedInterface)]
    [Arguments(ConstrainedDto)]
    [Arguments(ReversePayload)]
    [Arguments(ReverseInterface)]
    [Arguments(ReverseNullable)]
    [Arguments(ReverseErased)]
    [Arguments(ReverseNested)]
    [Arguments(ConstrainedCommand)]
    [Arguments(ConstrainedResult)]
    [Arguments(Suppressed)]
    public async Task MixedEnvelopes_CompilerRejects_SeparateInputAndOutputExecute(string source)
    {
        var compilation = CreateCompilation(source);
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostics = await Analyze(compilation);
        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldAllBe(diagnostic => diagnostic.Id == CommandResultSeparationAnalyzer.DiagnosticId &&
            diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Location.IsInSource && !diagnostic.IsSuppressed);
        await ExecuteValidFlow();
    }

    [Test]
    public async Task PublicPayloadInCompiledDependency_CompilerRejects_SeparateInputAndOutputExecute()
    {
        var metadata = CSharpCompilation.Create(MetadataAssemblyName,
            [CSharpSyntaxTree.ParseText(MetadataPayload)], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var image = new MemoryStream();
        var emitted = metadata.Emit(image);
        emitted.Success.ShouldBeTrue(string.Join(Environment.NewLine, emitted.Diagnostics));
        var compilation = CreateCompilation(MetadataConsumer).AddReferences(MetadataReference.CreateFromImage(image.ToArray()));
        var diagnostics = await Analyze(compilation);
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CommandResultSeparationAnalyzer.DiagnosticId &&
            diagnostic.Severity == DiagnosticSeverity.Error);
        await ExecuteValidFlow();
    }

    [Test]
    public async Task ConfiguredSuppression_CompilerStillRejects_SeparateInputAndOutputExecute()
    {
        var compilation = CreateCompilation(ExplicitResult);
        compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(
            compilation.Options.SpecificDiagnosticOptions.SetItem(CommandResultSeparationAnalyzer.DiagnosticId, ReportDiagnostic.Suppress)));
        var diagnostics = await Analyze(compilation);
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CommandResultSeparationAnalyzer.DiagnosticId &&
            diagnostic.Severity == DiagnosticSeverity.Error && !diagnostic.IsSuppressed);
        await ExecuteValidFlow();
    }

    [Test]
    public Task SeparateCommandAndResult_CompilerAccepts_ExecutesTerminalSuccessAndFailure() => ExecuteValidFlow();

    [Test]
    public async Task SharedPayloadDiamond_CompilerAccepts_ExecutesTerminalResult()
    {
        var nodes = string.Concat(Enumerable.Range(0, SharedGraphDepth).Select(index =>
            string.Format(CultureInfo.InvariantCulture, SharedGraphNode, index, index + 1)));
        var leaf = string.Format(CultureInfo.InvariantCulture, SharedGraphLeaf, SharedGraphDepth);
        await ExecuteValidFlow(nodes + leaf + SharedGraphExecute);
    }

    [Test]
    public async Task CyclicSharedPayload_BothResultRootsRejected_SeparateFlowExecutes()
    {
        var compilation = CreateCompilation(CyclicSharedPayload);
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostics = await Analyze(compilation);
        foreach (var payload in new[] { LeftPayload, RightPayload })
        {
            diagnostics.ShouldContain(diagnostic => diagnostic.Id == CommandResultSeparationAnalyzer.DiagnosticId &&
                diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan) == payload);
        }
        await ExecuteValidFlow();
    }

    private static CSharpCompilation CreateCompilation(string source) => CSharpCompilation.Create(AssemblyName,
        [CSharpSyntaxTree.ParseText(Prefix + Environment.NewLine + source + Environment.NewLine + Suffix)], References(),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static MetadataReference[] References() => ((string)AppContext.GetData(TrustedAssembliesKey)!).Split(Path.PathSeparator)
        .Append(typeof(Result).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path)).ToArray();

    private static Task<ImmutableArray<Diagnostic>> Analyze(CSharpCompilation compilation) =>
        compilation.WithAnalyzers([new CommandResultSeparationAnalyzer()]).GetAnalyzerDiagnosticsAsync();

    private static async Task ExecuteValidFlow(string source = ValidFlow)
    {
        var compilation = CreateCompilation(source);
        (await Analyze(compilation)).ShouldBeEmpty();
        using var image = new MemoryStream();
        var emitted = compilation.Emit(image);
        emitted.Success.ShouldBeTrue(string.Join(Environment.NewLine, emitted.Diagnostics));
        image.Position = 0;
        var context = new AssemblyLoadContext(AssemblyName, isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(image);
            assembly.GetType(EntryType)!.GetMethod(EntryMethod, BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, null).ShouldBe(true);
        }
        finally
        {
            context.Unload();
        }
    }
}

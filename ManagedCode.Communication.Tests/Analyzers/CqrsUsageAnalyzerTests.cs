using System;
using System.Collections.Immutable;
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

public sealed class CqrsUsageAnalyzerTests
{
    private const string AssemblyName = "CqrsUsageRegression";
    private const string TrustedAssembliesKey = "TRUSTED_PLATFORM_ASSEMBLIES";
    private const string EntryType = "Scenario";
    private const string EntryMethod = "Execute";
    private const string Prefix = """
        #nullable enable
        using System;
        using ManagedCode.Communication;
        using ManagedCode.Communication.CQRS;
        using Chunk = ManagedCode.Communication.CQRS.CqrsStreamChunk<string, string>;
        public static class Scenario {
        """;
    private const string Suffix = "}";
    private const string ConditionalSuccess = "public static Chunk Run(Result<string> result) => result.IsSuccess ? Chunk.Completed(result) : Chunk.Failed(result);";
    private const string ConditionalFailure = "public static Chunk Run(Result<string> result) => result.IsFailed ? Chunk.Failed(result) : Chunk.Completed(result);";
    private const string ConditionalNegated = "public static Chunk Run(Result<string> result) => !result.IsSuccess ? Chunk.Failed(result) : Chunk.Completed(result);";
    private const string ProgressWrapping = "public static Chunk Run(string payload) => Chunk.Progress(Result<string>.Succeed(payload));";
    private const string CompletionWrapping = "public static Chunk Run(string payload) => Chunk.Completed(Result.Succeed(payload));";
    private const string CompletedFailure = "public static Chunk Run() => Chunk.Completed(Result<string>.Fail(), sequence: 2);";
    private const string FailedSuccess = "public static Chunk Run() => Chunk.Failed(Result<string>.Succeed(string.Empty));";
    private const string ChangedMetadata = "public static Chunk Run(Result<string> result) => result.IsSuccess ? Chunk.Completed(result, sequence: 2) : Chunk.Failed(result, sequence: 3);";
    private const string DifferentResults = "public static Chunk Run(Result<string> result, Result<string> other) => result.IsSuccess ? Chunk.Completed(result) : Chunk.Failed(other);";
    private const string UnknownStatus = "public static Chunk Run(bool success, Result<string> result) => success ? Chunk.Completed(result) : Chunk.Failed(result);";
    private const string DifferentReceivers = "public static Result<string> Get() => Result<string>.Succeed(string.Empty); public static Chunk Run() => Get().IsSuccess ? Chunk.Completed(Get()) : Chunk.Failed(Get());";
    private const string WrongBranches = "public static Chunk Run(Result<string> result) => result.IsSuccess ? Chunk.Failed(result) : Chunk.Completed(result);";
    private const string ExplicitEvent = "public static Chunk Run(string payload) => Chunk.Progress(Result<string>.Succeed(payload), eventId: payload);";
    private const string ForeignSuccessFactory = "public sealed class Factory { public static Result<string> Succeed(string payload) => Result<string>.Succeed(payload); } public static Chunk Run(string payload) => Chunk.Completed(Factory.Succeed(payload));";
    private const string ResultAsPayload = "public static CqrsStreamChunk<int, Result<string>> Run() => CqrsStreamChunk<int, Result<string>>.Completed(Result<string>.Fail());";
    private const string WidenedProblem = "public static CqrsStreamChunk<object, object> Run(Problem problem) => CqrsStreamChunk<object, object>.Completed(Result<object>.Succeed(problem));";
    private const string ForeignFactories = "public sealed class Foreign { public static string Progress(Result<string> result) => string.Empty; } public static string Run(string payload) => Foreign.Progress(Result<string>.Succeed(payload));";
    internal const string ConvertedStatusIterator = """
        public static async System.Threading.Tasks.Task<bool> Execute()
        {
            var problem = Problem.GenericError();
            var count = 0;
            await foreach (var chunk in Stream(Result.Fail(problem)))
            {
                if (!chunk.IsFailed || !ReferenceEquals(chunk.Problem, problem)) return false;
                count++;
            }
            return count == 1;
        }
        private static async System.Collections.Generic.IAsyncEnumerable<Chunk> Stream(Result result)
        {
            await System.Threading.Tasks.Task.Yield();
            yield return result.IsSuccess ? Chunk.Completed(result) : Chunk.Failed(result);
        }
        """;
    private const string ConvertedSuccessStatus = "public static bool Execute() { var chunk = Chunk.Failed(Result.Succeed()); return chunk.IsFailed && chunk.TryGetProblem(out _); }";
    private const string RawResultConditional = "public static CqrsStreamChunk<int, Result<string>> Run(Result<string> result) => result.IsSuccess ? CqrsStreamChunk<int, Result<string>>.Completed(result) : CqrsStreamChunk<int, Result<string>>.Failed(result);";
    private const string ShadowedChunk = """
        using ManagedCode.Communication;
        namespace ManagedCode.Communication.CQRS
        {
            public sealed class CqrsStreamChunk<TProgress, TResult>
            {
                public static CqrsStreamChunk<TProgress, TResult> Progress(Result<TProgress> result) => new();
            }
        }
        public static class Scenario
        {
            public static object Run() => ManagedCode.Communication.CQRS.CqrsStreamChunk<int, int>.Progress(Result<int>.Succeed(7));
        }
        """;
    private const string CorrectedFlow = """
        public static bool Execute()
        {
            var result = Result<string>.Succeed(string.Empty);
            Chunk success = result;
            var problem = Problem.GenericError();
            var failed = Result<string>.Fail(problem);
            var failure = Chunk.FromResult(failed, sequence: 2);
            var progress = Chunk.Progress(string.Empty);
            var completed = Chunk.Completed(string.Empty);
            return success.IsCompleted && success.Final == result && failure.IsFailed &&
                ReferenceEquals(failure.Problem, problem) && failure.Sequence == 2 &&
                progress.IsProgress && completed.IsCompleted;
        }
        """;
    private const string RepeatedMethod = "public static Result<string> Run{0}(Result<string> result) {{ System.GC.KeepAlive(System.Array.Empty<int>()); return Result<string>.From(result); }}";

    [Test]
    [Arguments(ConditionalSuccess, CqrsUsageAnalyzer.ResultConversionId)]
    [Arguments(ConditionalFailure, CqrsUsageAnalyzer.ResultConversionId)]
    [Arguments(ConditionalNegated, CqrsUsageAnalyzer.ResultConversionId)]
    [Arguments(ProgressWrapping, CqrsUsageAnalyzer.PayloadFactoryId)]
    [Arguments(CompletionWrapping, CqrsUsageAnalyzer.PayloadFactoryId)]
    [Arguments(CompletedFailure, CqrsUsageAnalyzer.TerminalStatusId)]
    [Arguments(FailedSuccess, CqrsUsageAnalyzer.TerminalStatusId)]
    public async Task ChunkConstruction_ReportsDiagnostic_ReplacementExecutes(string source, string expected)
    {
        var diagnostics = await Analyze(Create(source), new CqrsUsageAnalyzer());
        diagnostics.Length.ShouldBe(1);
        diagnostics[0].Id.ShouldBe(expected);
        diagnostics[0].Severity.ShouldBe(expected == CqrsUsageAnalyzer.TerminalStatusId ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning);
        await ExecuteCorrectedFlow();
    }

    [Test]
    [Arguments(ChangedMetadata)]
    [Arguments(DifferentResults)]
    [Arguments(UnknownStatus)]
    [Arguments(DifferentReceivers)]
    [Arguments(WrongBranches)]
    [Arguments(ExplicitEvent)]
    [Arguments(ForeignSuccessFactory)]
    [Arguments(ForeignFactories)]
    [Arguments(WidenedProblem)]
    [Arguments(ResultAsPayload)]
    [Arguments(RawResultConditional)]
    public async Task MeaningfulConstruction_DoesNotRecommendUnsafeReplacement(string source)
    {
        (await Analyze(Create(source), new CqrsUsageAnalyzer())).ShouldBeEmpty();
        await ExecuteCorrectedFlow();
    }

    [Test]
    [Arguments(ConvertedSuccessStatus)]
    public async Task NativeResultConversion_PreservesItsActualTerminalOutcome(string source)
    {
        var compilation = Create(source);
        (await Analyze(compilation, new CqrsUsageAnalyzer())).ShouldBeEmpty();
        await ExecuteAsync(compilation);
    }

    [Test]
    public async Task ForeignChunkWithIdenticalNamespace_DoesNotReceiveNativeRecommendations()
    {
        (await Analyze(Create(ShadowedChunk, includeWrapper: false), new CqrsUsageAnalyzer())).ShouldBeEmpty();
        await ExecuteCorrectedFlow();
    }

    [Test]
    public async Task RepeatedTypesAcrossCompilations_PreserveAllRecommendations()
    {
        var source = string.Join(Environment.NewLine, Enumerable.Range(0, 100).Select(index =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, RepeatedMethod, index)));
        // Reuse the analyzer instance, but never reuse symbol caches across compilations.
        var analyzer = new ResultUsageAnalyzer();
        for (var iteration = 0; iteration < 2; iteration++)
        {
            var diagnostics = await Analyze(Create(source), analyzer);
            diagnostics.Length.ShouldBe(100);
            diagnostics.ShouldAllBe(diagnostic => diagnostic.Id == ResultUsageAnalyzer.RedundantWrappingId);
        }
        await ExecuteCorrectedFlow();
    }

    private static async Task ExecuteCorrectedFlow()
    {
        var compilation = Create(CorrectedFlow);
        (await Analyze(compilation, new CqrsUsageAnalyzer(), new ResultUsageAnalyzer())).ShouldBeEmpty();
        await ExecuteAsync(compilation);
    }

    private static async Task ExecuteAsync(CSharpCompilation compilation)
    {
        using var image = new MemoryStream();
        var emitted = compilation.Emit(image);
        emitted.Success.ShouldBeTrue(string.Join(Environment.NewLine, emitted.Diagnostics));
        image.Position = 0;
        var context = new AssemblyLoadContext(AssemblyName, isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(image);
            var outcome = assembly.GetType(EntryType)!.GetMethod(EntryMethod, BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, null);
            if (outcome is Task<bool> asyncOutcome) (await asyncOutcome).ShouldBeTrue();
            else outcome.ShouldBe(true);
        }
        finally
        {
            context.Unload();
        }
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(CSharpCompilation compilation, params DiagnosticAnalyzer[] analyzers)
    {
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return await compilation.WithAnalyzers(analyzers.ToImmutableArray()).GetAnalyzerDiagnosticsAsync();
    }

    private static CSharpCompilation Create(string source, bool includeWrapper = true)
    {
        var references = ((string)AppContext.GetData(TrustedAssembliesKey)!).Split(Path.PathSeparator)
            .Append(typeof(Result).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path));
        return CSharpCompilation.Create(AssemblyName,
            [CSharpSyntaxTree.ParseText(includeWrapper ? Prefix + Environment.NewLine + source + Environment.NewLine + Suffix : source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }
}

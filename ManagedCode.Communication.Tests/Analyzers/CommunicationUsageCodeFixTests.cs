using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Shouldly;

namespace ManagedCode.Communication.Tests.Analyzers;

public sealed class CommunicationUsageCodeFixTests
{
    private const string AssemblyName = "CommunicationCodeFixRegression";
    private const string TrustedAssembliesKey = "TRUSTED_PLATFORM_ASSEMBLIES";
    private const string EntryType = "Scenario";
    private const string EntryMethod = "Execute";
    private const string FileName = "Scenario.cs";
    private const string Prefix = """
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using ManagedCode.Communication;
        using ManagedCode.Communication.CQRS;
        using Chunk = ManagedCode.Communication.CQRS.CqrsStreamChunk<string, string>;
        public static class Scenario {
        """;
    private const string Suffix = "}";
    private const string Negated = "public static bool Execute() { var result = Result.Fail(); return !result.IsSuccess; }";
    private const string Compared = "public static bool Execute() { var result = Result.Fail(); return false == result.IsSuccess; }";
    private const string Pattern = "public static bool Execute() { var result = Result.Fail(); return result.IsSuccess is not true; }";
    private const string NullCheck = "public static bool Execute() { var result = Result<string>.Succeed(string.Empty); if (result.IsFailed) return false; return result.Value != null; }";
    private const string NullPattern = "public static bool Execute() { var result = Result<string>.Succeed(string.Empty); if (result.IsFailed) return false; return result.Value is { }; }";
    private const string Suppression = "public static bool Execute() { var result = Result<string>.Succeed(string.Empty); result.ThrowIfProblem(); return result.Value!.Length == 0; }";
    private const string ConditionalAccess = "public static bool Execute() { var result = Result<string>.Succeed(string.Empty); if (result.IsFailed) return false; return result.Value?.Length == 0; }";
    private const string NullThrow = "public static bool Execute() { var result = Result<string>.Succeed(string.Empty); result.ThrowIfProblem(); ArgumentNullException.ThrowIfNull(result.Value); return result.Value.Length == 0; }";
    private const string Reconstruction = "public static bool Execute() { var result = Result<string>.Succeed(string.Empty); if (result.IsFailed) return false; var copy = Result<string>.Succeed(result.Value); return copy.IsSuccess && copy.Value.Length == 0; }";
    private const string From = "public static bool Execute() { var result = Result<string>.Succeed(string.Empty); var copy = Result<string>.From(result); return copy.IsSuccess && copy.Value.Length == 0; }";
    private const string ConditionalChunk = "public static bool Execute() { var result = Result<string>.Succeed(string.Empty); var chunk = result.IsSuccess ? Chunk.Completed(result) : Chunk.Failed(result); return chunk.IsCompleted; }";
    private const string CompletedFailure = "public static bool Execute() { var chunk = Chunk.Completed(Result<string>.Fail(), sequence: 2); return chunk.IsFailed && chunk.Sequence == 2; }";
    private const string CompletedConvertedStatus = "public static bool Execute() { var chunk = Chunk.Completed(Result.Succeed()); return chunk.IsFailed && chunk.TryGetProblem(out _); }";
    private const string FailedSuccess = "public static bool Execute() { var chunk = Chunk.Failed(Result<string>.Succeed(string.Empty)); return chunk.IsCompleted; }";
    private const string ChunkPayload = "public static bool Execute() { var chunk = Chunk.Progress(Result<string>.Succeed(string.Empty)); return chunk.IsProgress; }";
    private const string Capture = "public static bool Execute() { var result = Result.Fail(); return result.IsFailed && result.Problem is { } problem && problem.StatusCode == 500; }";
    private const string NullThrowWithEffect = "private static int calls; private static string Name() { calls++; return string.Empty; } public static bool Execute() { var result = Result<string>.Succeed(string.Empty); result.ThrowIfProblem(); ArgumentNullException.ThrowIfNull(result.Value, Name()); return calls == 1; }";
    private const string Nested = "public static bool Execute() { var result = Result.Succeed(Result.Succeed(7)); return result.IsSuccess && result.Value.IsSuccess && result.Value.Value == 7; }";
    private const string Iterator = """
        public static async Task<bool> Execute()
        {
            var count = 0;
            await foreach (var chunk in Stream(Result<string>.Succeed(string.Empty)))
            {
                if (!chunk.IsCompleted) return false;
                count++;
            }
            return count == 1;
        }
        private static async IAsyncEnumerable<Chunk> Stream(Result<string> result)
        {
            await Task.Yield();
            yield return result.IsSuccess ? Chunk.Completed(result) : Chunk.Failed(result);
        }
        """;

    [Test]
    [Arguments(Negated, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(Compared, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(Pattern, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(NullCheck, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(NullPattern, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(Suppression, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(ConditionalAccess, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(NullThrow, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(Reconstruction, ResultUsageAnalyzer.RedundantWrappingId)]
    [Arguments(From, ResultUsageAnalyzer.RedundantWrappingId)]
    [Arguments(ConditionalChunk, CqrsUsageAnalyzer.ResultConversionId)]
    [Arguments(ChunkPayload, CqrsUsageAnalyzer.PayloadFactoryId)]
    [Arguments(Iterator, CqrsUsageAnalyzer.ResultConversionId)]
    [Arguments(CompletedFailure, CqrsUsageAnalyzer.TerminalStatusId)]
    [Arguments(FailedSuccess, CqrsUsageAnalyzer.TerminalStatusId)]
    [Arguments(CompletedConvertedStatus, CqrsUsageAnalyzer.TerminalStatusId)]
    [Arguments(CqrsUsageAnalyzerTests.ConvertedStatusIterator, CqrsUsageAnalyzer.ResultConversionId)]
    public async Task Diagnostic_RealCodeFix_CompilesAndExecutes(string source, string expected)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(AssemblyName, LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReferences(((string)AppContext.GetData(TrustedAssembliesKey)!).Split(Path.PathSeparator)
                .Append(typeof(Result).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path)));
        var document = project.AddDocument(FileName, SourceText.From(Prefix + Environment.NewLine + source + Environment.NewLine + Suffix));
        var compilation = (await document.Project.GetCompilationAsync())!;
        var diagnostic = (await Analyze(compilation)).Single(item => item.Id == expected);
        var actions = new List<CodeAction>();
        await new CommunicationUsageCodeFixProvider().RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic,
            (action, _) => actions.Add(action), CancellationToken.None));
        actions.Count.ShouldBe(1);
        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var fixedDocument = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id)!;
        var fixedCompilation = (await fixedDocument.Project.GetCompilationAsync())!;
        (await Analyze(fixedCompilation)).ShouldBeEmpty();
        await ExecuteAsync(fixedCompilation);
    }

    [Test]
    [Arguments(Capture)]
    [Arguments(NullThrowWithEffect)]
    [Arguments(Nested)]
    public async Task UnsafeAutomaticEdit_IsNotOffered_OriginalBehaviorExecutes(string source)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(AssemblyName, LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReferences(((string)AppContext.GetData(TrustedAssembliesKey)!).Split(Path.PathSeparator)
                .Append(typeof(Result).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path)));
        var document = project.AddDocument(FileName, SourceText.From(Prefix + Environment.NewLine + source + Environment.NewLine + Suffix));
        var compilation = (await document.Project.GetCompilationAsync())!;
        var diagnostics = await Analyze(compilation);
        diagnostics.ShouldNotBeEmpty();
        foreach (var diagnostic in diagnostics)
        {
            var actions = new List<CodeAction>();
            await new CommunicationUsageCodeFixProvider().RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic,
                (action, _) => actions.Add(action), CancellationToken.None));
            actions.ShouldBeEmpty();
        }
        await ExecuteAsync(compilation);
    }

    private static async Task ExecuteAsync(Compilation fixedCompilation)
    {
        using var image = new MemoryStream();
        var emitted = fixedCompilation.Emit(image);
        emitted.Success.ShouldBeTrue(string.Join(Environment.NewLine, emitted.Diagnostics));
        image.Position = 0;
        var loadContext = new AssemblyLoadContext(AssemblyName, isCollectible: true);
        try
        {
            var assembly = loadContext.LoadFromStream(image);
            var outcome = assembly.GetType(EntryType)!.GetMethod(EntryMethod, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
            if (outcome is Task<bool> asyncOutcome) (await asyncOutcome).ShouldBeTrue();
            else outcome.ShouldBe(true);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    [Test]
    [Arguments(false, false, FixAllScope.Document)]
    [Arguments(true, false, FixAllScope.Document)]
    [Arguments(false, true, FixAllScope.Document)]
    [Arguments(true, true, FixAllScope.Document)]
    [Arguments(false, false, FixAllScope.Project)]
    [Arguments(true, true, FixAllScope.Project)]
    [Arguments(false, false, FixAllScope.Solution)]
    [Arguments(true, true, FixAllScope.Solution)]
    public async Task BatchFixAll_CompilesAndExecutesIterator(bool fullTypeName, bool convertedResult, FixAllScope scope)
    {
        const string alias = "Chunk";
        const string fullName = "CqrsStreamChunk<string, string>";
        var sample = convertedResult ? CqrsUsageAnalyzerTests.ConvertedStatusIterator : Iterator;
        var source = fullTypeName ? sample.Replace(alias, fullName) : sample;
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(AssemblyName, LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReferences(((string)AppContext.GetData(TrustedAssembliesKey)!).Split(Path.PathSeparator)
                .Append(typeof(Result).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path)));
        var document = project.AddDocument(FileName, SourceText.From(Prefix + Environment.NewLine + source + Environment.NewLine + Suffix));
        var provider = new CommunicationUsageCodeFixProvider();
        var context = new FixAllContext(document, provider, scope, CqrsUsageAnalyzer.ResultConversionId,
            [CqrsUsageAnalyzer.ResultConversionId], new DocumentDiagnostics(), CancellationToken.None);
        var action = await provider.GetFixAllProvider().GetFixAsync(context);
        action.ShouldNotBeNull();
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var fixedDocument = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id)!;
        var fixedCompilation = (await fixedDocument.Project.GetCompilationAsync())!;
        (await Analyze(fixedCompilation)).ShouldBeEmpty();
        await ExecuteAsync(fixedCompilation);
    }

    private sealed class DocumentDiagnostics : FixAllContext.DiagnosticProvider
    {
        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken token)
        {
            var tree = await document.GetSyntaxTreeAsync(token);
            var compilation = (await document.Project.GetCompilationAsync(token))!;
            return (await Analyze(compilation)).Where(diagnostic => diagnostic.Location.SourceTree == tree);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken token) =>
            Task.FromResult<IEnumerable<Diagnostic>>([]);

        public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken token) =>
            await Analyze((await project.GetCompilationAsync(token))!);
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(Compilation compilation) => compilation
        .WithAnalyzers([new ResultUsageAnalyzer(), new CqrsUsageAnalyzer()]).GetAnalyzerDiagnosticsAsync();
}

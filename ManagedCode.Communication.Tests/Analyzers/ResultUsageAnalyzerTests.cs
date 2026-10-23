using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Loader;
using ManagedCode.Communication.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shouldly;

namespace ManagedCode.Communication.Tests.Analyzers;

public sealed class ResultUsageAnalyzerTests
{
    private const string AssemblyName = "CommunicationUsageRegression";
    private const string TrustedAssembliesKey = "TRUSTED_PLATFORM_ASSEMBLIES";
    private const string EntryType = "Scenario";
    private const string EntryMethod = "Execute";
    private const string Prefix = """
        #nullable enable
        using ManagedCode.Communication;
        using ManagedCode.Communication.CollectionResultT;
        public static class Scenario {
        """;
    private const string Suffix = "}";
    private const string InvertedTyped = "public static bool Execute(Result<string> result) => !result.IsSuccess;";
    private const string ComparedFalse = "public static bool Execute(Result<string> result) => result.IsSuccess == false;";
    private const string ComparedTrue = "public static bool Execute(Result<string> result) => true != result.IsSuccess;";
    private const string PatternFalse = "public static bool Execute(Result<string> result) => result.IsSuccess is false;";
    private const string PatternNotTrue = "public static bool Execute(Result<string> result) => result.IsSuccess is not true;";
    private const string UnknownGenericValue = "public static T Execute<T>(Result<T> result) => result.Value!;";
    private const string NestedLambda = "public static System.Func<string> Execute(Result<string> result) { if (result.IsFailed) return () => string.Empty; var captured = () => result.Value!; result = Result<string>.Fail(); return captured; }";
    private const string InterfaceFlow = "public static bool Execute() { IResult<string> success = Result<string>.Succeed(string.Empty); if (success.IsFailed) return success.Problem.StatusCode == 0; if (success.Value.Length != 0) return false; IResult failure = Result.Fail(Problem.GenericError()); if (failure.IsSuccess) return false; return failure.Problem.StatusCode == 500; }";
    private const string InvertedPlain = "public static bool Execute(Result result) => !(result.IsSuccess);";
    private const string InvertedCollection = "public static bool Execute(CollectionResult<string> result) => !result.IsSuccess;";
    private const string InvertedInterface = "public static bool Execute(IResult result) => !result.IsSuccess;";
    private const string ProblemSuppression = "public static Problem? Execute(Result<string> result) { if (result.IsFailed) return result.Problem!; return null; }";
    private const string ProblemCheck = "public static bool Execute(Result result) => result.IsFailed && result.Problem is not null;";
    private const string ProblemCoalesce = "public static Problem? Execute(Result result) { if (result.IsFailed) return result.Problem ?? Problem.GenericError(); return null; }";
    private const string ProblemConditional = "public static string? Execute(Result result) { if (result.IsFailed) return result.Problem?.Title; return null; }";
    private const string ValueSuppression = "public static string? Execute(Result<string?> result) { if (result.IsFailed) return null; return result.Value!; }";
    private const string ValueCheck = "public static bool Execute(Result<string?> result) => result.IsSuccess && result.Value != null;";
    private const string CollectionCheck = "public static bool Execute(CollectionResult<string> result) => result.Collection is null;";
    private const string UnknownProblem = "public static Problem Execute(Result result) => result.Problem!;";
    private const string UnknownValue = "public static string Execute(Result<string> result) => result.Value!;";
    private const string NullableMember = "public sealed class Payload { public string? Name { get; set; } } public static bool Execute(Result<Payload> result) => result.IsSuccess && result.Value.Name is null;";
    private const string NullableElement = "public static bool Execute(CollectionResult<string?> result) => result.Collection[0] is null;";
    private const string Reassigned = "public static string Execute(Result<string> result, Result<string> other) { if (result.IsFailed) return string.Empty; result = other; return result.Value!; }";
    private const string SeparateLambdas = "public static System.Collections.Generic.IEnumerable<string> Execute(System.Collections.Generic.IEnumerable<Result<string>> results) => System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(results, r => r.IsSuccess), r => r.Value!);";
    private const string Unrelated = "public sealed class Other { public bool IsSuccess { get; set; } public string? Value { get; set; } } public static bool Execute(Other other) => !other.IsSuccess && other.Value is null;";
    private const string DisabledNullability = "#nullable disable\npublic static string Execute(Result<string> result) { if (result.IsSuccess) return result.Value!; return null; }";
    private const string ValidFlow = """
        public static bool Execute() {
            var expected = Problem.GenericError();
            var failure = Result<string>.Fail(expected);
            var propagated = Convert(failure);
            if (propagated.IsSuccess) return false;
            if (!object.ReferenceEquals(propagated.Problem, expected)) return false;
            var success = Convert(Result<string>.Succeed("payload"));
            if (success.IsFailed) return false;
            return success.Value == 7;
        }
        private static Result<int> Convert(Result<string> result) {
            if (result.IsFailed) return Result<int>.Fail(result.Problem);
            return Result<int>.Succeed(result.Value.Length);
        }
        """;

    [Test]
    [Arguments(InvertedTyped, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(ComparedFalse, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(ComparedTrue, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(PatternFalse, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(PatternNotTrue, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(InvertedPlain, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(InvertedCollection, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(InvertedInterface, ResultUsageAnalyzer.NegatedSuccessId)]
    [Arguments(ProblemSuppression, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(ProblemCheck, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(ProblemCoalesce, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(ProblemConditional, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(ValueSuppression, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(ValueCheck, ResultUsageAnalyzer.RedundantNullGuardId)]
    [Arguments(CollectionCheck, ResultUsageAnalyzer.RedundantNullGuardId)]
    public async Task InvalidUsage_Compilation_RejectsWithError(string source, string expectedId)
    {
        var diagnostics = await Analyze(CreateCompilation(source));
        diagnostics.ShouldHaveSingleItem();
        diagnostics[0].Id.ShouldBe(expectedId);
        diagnostics[0].Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostics[0].Location.IsInSource.ShouldBeTrue();
    }

    [Test]
    [Arguments(UnknownProblem)]
    [Arguments(UnknownValue)]
    [Arguments(UnknownGenericValue)]
    [Arguments(NestedLambda)]
    [Arguments(NullableMember)]
    [Arguments(NullableElement)]
    [Arguments(Reassigned)]
    [Arguments(SeparateLambdas)]
    [Arguments(Unrelated)]
    [Arguments(DisabledNullability)]
    public async Task NecessaryAbsenceHandling_Compilation_Accepts(string source)
    {
        var compilation = CreateCompilation(source);
        (await Analyze(compilation)).ShouldBeEmpty();
        using var image = new MemoryStream();
        var emitted = compilation.Emit(image);
        emitted.Success.ShouldBeTrue(string.Join(Environment.NewLine, emitted.Diagnostics));
    }

    [Test]
    [Arguments(ValidFlow)]
    [Arguments(InterfaceFlow)]
    public async Task CorrectGuard_CompilationAndExecution_PreservesProblemAndConsumesValue(string source)
    {
        var compilation = CreateCompilation(source);
        (await Analyze(compilation)).ShouldBeEmpty();
        using var image = new MemoryStream();
        var emitted = compilation.Emit(image);
        emitted.Success.ShouldBeTrue(string.Join(Environment.NewLine, emitted.Diagnostics));
        image.Position = 0;
        var loadContext = new AssemblyLoadContext(AssemblyName, isCollectible: true);
        try
        {
            var assembly = loadContext.LoadFromStream(image);
            assembly.GetType(EntryType)!.GetMethod(EntryMethod, BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, null).ShouldBe(true);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var references = ((string)AppContext.GetData(TrustedAssembliesKey)!).Split(Path.PathSeparator)
            .Append(typeof(Result).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path));
        return CSharpCompilation.Create(AssemblyName,
            [CSharpSyntaxTree.ParseText(Prefix + Environment.NewLine + source + Environment.NewLine + Suffix)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(CSharpCompilation compilation)
    {
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return await compilation.WithAnalyzers([new ResultUsageAnalyzer()]).GetAnalyzerDiagnosticsAsync();
    }
}

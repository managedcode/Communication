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

public sealed class ResultUsageContractRegressionTests
{
    private const string AssemblyName = "CommunicationResultContractRegression";
    private const string TrustedAssembliesKey = "TRUSTED_PLATFORM_ASSEMBLIES";
    private const string EntryType = "Scenario";
    private const string EntryMethod = "Execute";
    private const string Prefix = """
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using ManagedCode.Communication;
        using ManagedCode.Communication.CollectionResultT;
        public static class Scenario {
        """;
    private const string Suffix = "}";
    private const string ProblemCapture = "public static bool Observe(Result result) => result.IsFailed && result.Problem is { } problem;";
    private const string ProblemInterfaceCapture = "public static bool Observe(IResult<string> result) => result.IsFailed && result.Problem is { } problem;";
    private const string ProblemNegativeCapture = "public static bool Observe(Result result) => result.IsFailed && result.Problem is not { };";
    private const string ValueCapture = "public static bool Observe(Result<string> result) => result.IsSuccess && result.Value is { } value;";
    private const string GenericValueCapture = "public static bool Observe<T>(IResult<T> result) => result.IsSuccess && result.Value is { } value;";
    private const string ValueCaptureAfterThrow = "public static bool Observe(Result<string?> result) { result.ThrowIfProblem(); return result.Value is { } value; }";
    private const string ValueSuppressionAfterThrow = "public static string Observe(Result<string> result) { result.ThrowIfProblem(); return result.Value!; }";
    private const string ValueNullThrow = "public static void Observe(Result<string> result) { if (result.IsFailed) return; ArgumentNullException.ThrowIfNull(result.Value); }";
    private const string ProblemNullThrow = "public static void Observe(IResult result) { if (result.IsFailed) ArgumentNullException.ThrowIfNull(result.Problem); }";
    private const string ValueNullThrowAfterThrow = "public static void Observe(Result<string> result) { result.ThrowIfProblem(); ArgumentNullException.ThrowIfNull(result.Value); }";
    private const string UnboundResultMetadata = "public static Type Observe() => typeof(Result<>);";
    private const string UnboundCollectionMetadata = "public static Type Observe() => typeof(CollectionResult<>);";
    private const string UnboundInterfaceMetadata = "public static Type Observe() => typeof(IResult<>);";
    private const string UnknownCaptures = "public static bool Observe(Result<string> result) => result.Value is { } value || result.Problem is { } problem;";
    private const string UnknownNullThrow = "public static void Observe(Result<string> result) => ArgumentNullException.ThrowIfNull(result.Value);";
    private const string ReassignedAfterThrow = "public static string Observe(Result<string> result, Result<string> other) { result.ThrowIfProblem(); result = other; return result.Value!; }";
    private const string RefAliasAfterThrow = "public static string Observe(Result<string> result, Result<string> other) { ref var alias = ref result; result.ThrowIfProblem(); alias = other; return result.Value!; }";
    private const string OutAfterSuccess = "private static void Replace(out Result<string> result) => result = Result<string>.Fail(); public static string Observe(Result<string> result) { if (result.IsFailed) return string.Empty; Replace(out result); return result.Value!; }";
    private const string ConditionalThrow = "public static string Observe(Result<string> result, bool condition) { if (condition) result.ThrowIfProblem(); return result.Value!; }";
    private const string CaughtThrow = "public static string Observe(Result<string> result) { try { result.ThrowIfProblem(); } catch (ProblemException) { } return result.Value!; }";
    private const string CapturedLambda = "public static Func<string> Observe(Result<string> result) { result.ThrowIfProblem(); return () => result.Value!; }";
    private const string DomainMember = "public sealed record Payload(string? Name); public static bool Observe(Result<Payload> result) { result.ThrowIfProblem(); return result.Value.Name is { } name; }";
    private const string Projection = "public static Result<int> Observe(Result<string> result) { if (result.IsFailed) return Result<int>.Fail(result.Problem); return Result.Succeed(result.Value.Length); }";
    private const string ChangedNullability = "public static Result<string> Observe(Result<string?> result) { result.ThrowIfProblem(); return Result<string>.Succeed(result.Value); }";
    private const string UnknownNumericStatus = "public static Result<int> Observe(Result<int> result) => Result.Succeed(result.Value);";
    private const string ExplicitPagination = "public static CollectionResult<string> Observe(CollectionResult<string> result) { if (result.IsFailed) return result; return CollectionResult<string>.Succeed(result.Collection, 1, result.Collection.Length, result.Collection.Length); }";
    private const string SameSuccess = "public static Result<string> Observe(Result<string> result) { if (result.IsFailed) return result; return Result.Succeed(result.Value); }";
    private const string SameFailure = "public static Result<string> Observe(Result<string> result) { if (result.IsFailed) return Result<string>.Fail(result.Problem); return result; }";
    private const string SamePlainFailure = "public static Result Observe(Result result) { if (result.IsFailed) return Result.Fail(result.Problem); return result; }";
    private const string SameSuccessConditional = "public static Result<string> Observe(Result<string> result) => result.IsSuccess ? Result.Succeed(result.Value) : result;";
    private const string SameSuccessCompared = "public static Result<string> Observe(Result<string> result) { if (result.IsSuccess == true) return Result.Succeed(result.Value); return result; }";
    private const string SameSuccessAfterThrow = "public static Result<string> Observe(Result<string> result) { result.ThrowIfProblem(); return Result.Succeed(result.Value); }";
    private const string SameCollection = "public static CollectionResult<string> Observe(CollectionResult<string> result) { if (result.IsFailed) return result; return CollectionResult<string>.Succeed(result.Collection); }";
    private const string SameFrom = "public static Result<string> Observe(Result<string> result) => Result<string>.From(result);";
    private const string NestedInferred = "public static object Observe() => Result.Succeed(Result.Succeed(1));";
    private const string NestedDeclared = "public static Result<Result<string>> Observe() => default;";
    private const string NestedCollection = "public static CollectionResult<Result<string>> Observe() => default;";
    private const string NestedInterface = "public static IResult<IResult<string>> Observe() => Result<IResult<string>>.Fail();";
    private const string NestedArray = "public static Result<Result<string>[]> Observe() => default;";
    private const string NestedTask = "public static Result<Task<Result<string>>> Observe() => default;";
    private const string CorrectedFlow = """
        private const string Payload = "payload";
        public sealed record Domain(string? Name);
        private static Result<string> Preserve(Result<string> result) => result;
        private static Result<int> Project(IResult<string> result)
        {
            if (result.IsFailed) { return Result<int>.Fail(result.Problem); }
            return Result.Succeed(result.Value.Length);
        }
        private static int Read(Result<string> result)
        {
            result.ThrowIfProblem();
            return result.Value.Length;
        }
        public static bool Execute()
        {
            var source = Result.Succeed(Payload);
            var unchanged = Preserve(source);
            unchanged.ThrowIfProblem();
            if (unchanged.Value != Payload || Read(source) != Payload.Length) { return false; }
            var projection = Project(source);
            projection.ThrowIfProblem();
            if (projection.Value != Payload.Length) { return false; }
            var problem = Problem.GenericError();
            var failure = Preserve(Result<string>.Fail(problem));
            if (failure.IsSuccess || !ReferenceEquals(failure.Problem, problem)) { return false; }
            var failedProjection = Project(failure);
            if (failedProjection.IsSuccess || !ReferenceEquals(failedProjection.Problem, problem)) { return false; }
            try { _ = Read(failure); return false; }
            catch (ProblemException exception)
            {
                if (!ReferenceEquals(exception.Problem, problem)) { return false; }
            }
            var domain = Result.Succeed(new Domain(null));
            domain.ThrowIfProblem();
            return domain.Value.Name is null;
        }
        """;
    private const string FromExecution = """
        private const string Payload = "payload";
        public sealed record FailurePayload(int Amount);
        public static bool Execute()
        {
            var payload = new FailurePayload(7);
            var source = Result<FailurePayload>.Fail(payload);
            var unchanged = Result<FailurePayload>.From(source);
            if (unchanged.IsSuccess || !ReferenceEquals(unchanged.Problem, source.Problem) || !ReferenceEquals(unchanged.Value, payload)) { return false; }
            var page = CollectionResult<string>.Succeed([Payload], 2, 1, 5);
            var unchangedPage = CollectionResult<string>.From(page);
            if (unchangedPage.PageNumber != 2 || unchangedPage.TotalItems != 5 || unchangedPage.Collection[0] != Payload) { return false; }
            var failedPage = CollectionResult<string>.Fail(Problem.GenericError(), [Payload]);
            var unchangedFailure = CollectionResult<string>.From(failedPage);
            return unchangedFailure.IsFailed && ReferenceEquals(unchangedFailure.Problem, failedPage.Problem) &&
                unchangedFailure.Collection[0] == Payload;
        }
        """;

    [Test]
    [Arguments(ProblemCapture)]
    [Arguments(ProblemInterfaceCapture)]
    [Arguments(ProblemNegativeCapture)]
    [Arguments(ValueCapture)]
    [Arguments(GenericValueCapture)]
    [Arguments(ValueCaptureAfterThrow)]
    [Arguments(ValueSuppressionAfterThrow)]
    [Arguments(ValueNullThrow)]
    [Arguments(ProblemNullThrow)]
    [Arguments(ValueNullThrowAfterThrow)]
    public async Task ProvenNullGuard_IsCompilerError_CorrectedFlowExecutes(string source)
    {
        var diagnostics = await Analyze(Create(source));
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == ResultUsageAnalyzer.RedundantNullGuardId &&
            diagnostic.Severity == DiagnosticSeverity.Error);
        diagnostics.ShouldAllBe(diagnostic => diagnostic.Id == ResultUsageAnalyzer.RedundantNullGuardId);
        await ExecuteCorrectedFlow();
    }

    [Test]
    [Arguments(SameSuccess)]
    [Arguments(SameFailure)]
    [Arguments(SamePlainFailure)]
    [Arguments(SameSuccessConditional)]
    [Arguments(SameSuccessCompared)]
    [Arguments(SameSuccessAfterThrow)]
    [Arguments(SameCollection)]
    [Arguments(SameFrom)]
    [Arguments(NestedInferred)]
    [Arguments(NestedDeclared)]
    [Arguments(NestedCollection)]
    [Arguments(NestedInterface)]
    [Arguments(NestedArray)]
    [Arguments(NestedTask)]
    public async Task RedundantWrapping_IsCompilerWarning_CorrectedFlowExecutes(string source)
    {
        var diagnostics = await Analyze(Create(source));
        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldAllBe(diagnostic => diagnostic.Id == ResultUsageAnalyzer.RedundantWrappingId &&
            diagnostic.Severity == DiagnosticSeverity.Warning);
        await ExecuteCorrectedFlow();
    }

    [Test]
    [Arguments(UnknownCaptures)]
    [Arguments(UnboundResultMetadata)]
    [Arguments(UnboundCollectionMetadata)]
    [Arguments(UnboundInterfaceMetadata)]
    [Arguments(UnknownNullThrow)]
    [Arguments(ReassignedAfterThrow)]
    [Arguments(RefAliasAfterThrow)]
    [Arguments(OutAfterSuccess)]
    [Arguments(ConditionalThrow)]
    [Arguments(CaughtThrow)]
    [Arguments(CapturedLambda)]
    [Arguments(DomainMember)]
    [Arguments(Projection)]
    [Arguments(ChangedNullability)]
    [Arguments(UnknownNumericStatus)]
    [Arguments(ExplicitPagination)]
    public async Task NecessaryHandlingOrProjection_Compiles_CorrectedFlowExecutes(string source)
    {
        var compilation = Create(source);
        (await Analyze(compilation)).ShouldBeEmpty();
        using var image = new MemoryStream();
        compilation.Emit(image).Success.ShouldBeTrue();
        await ExecuteCorrectedFlow();
    }

    [Test]
    public async Task NativeFrom_PreservesFailurePayloadProblemAndPagination_CompiledConsumerExecutes()
    {
        var compilation = Create(FromExecution);
        var diagnostics = await Analyze(compilation);
        diagnostics.ShouldNotBeEmpty();
        diagnostics.ShouldAllBe(diagnostic => diagnostic.Id == ResultUsageAnalyzer.RedundantWrappingId &&
            diagnostic.Severity == DiagnosticSeverity.Warning);
        Execute(compilation);
    }

    private static async Task ExecuteCorrectedFlow()
    {
        var compilation = Create(CorrectedFlow);
        (await Analyze(compilation)).ShouldBeEmpty();
        Execute(compilation);
    }

    private static void Execute(CSharpCompilation compilation)
    {
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

    private static async Task<ImmutableArray<Diagnostic>> Analyze(CSharpCompilation compilation)
    {
        compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return await compilation.WithAnalyzers([new ResultUsageAnalyzer()]).GetAnalyzerDiagnosticsAsync();
    }

    private static CSharpCompilation Create(string source)
    {
        var references = ((string)AppContext.GetData(TrustedAssembliesKey)!).Split(Path.PathSeparator)
            .Append(typeof(Result).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path));
        return CSharpCompilation.Create(AssemblyName,
            [CSharpSyntaxTree.ParseText(Prefix + Environment.NewLine + source + Environment.NewLine + Suffix)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }
}

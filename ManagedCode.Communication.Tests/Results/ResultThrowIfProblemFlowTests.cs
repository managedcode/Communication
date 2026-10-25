using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace ManagedCode.Communication.Tests.Results;

public sealed class ResultThrowIfProblemFlowTests
{
    private const string AssemblyName = "CommunicationThrowIfProblemRegression";
    private const string TrustedAssembliesKey = "TRUSTED_PLATFORM_ASSEMBLIES";
    private const string EntryType = "Scenario";
    private const string EntryMethod = "Execute";
    private const string Consumer = """
        #nullable enable
        using ManagedCode.Communication;
        using ManagedCode.Communication.CollectionResultT;

        public static class Scenario
        {
            private const string Payload = "payload";
            private const string FailureTitle = "The entry could not be read.";
            private const string FailureDetail = "The requested entry does not exist.";

            private static int Read(Result<string> result)
            {
                result.ThrowIfProblem();
                return result.Value.Length;
            }

            private static int ReadNullable(Result<string?> result)
            {
                result.ThrowIfProblem();
                return result.Value.Length;
            }

            private static int ReadCollection(CollectionResult<string> result)
            {
                result.ThrowIfProblem();
                return result.Value.Length + result.Collection.Length;
            }

            public static bool Execute()
            {
                if (Read(Result<string>.Succeed(Payload)) != Payload.Length ||
                    ReadNullable(Result<string?>.Succeed(Payload)) != Payload.Length ||
                    ReadCollection(CollectionResult<string>.Succeed([Payload])) != 2 ||
                    ReadCollection(CollectionResult<string>.Succeed([])) != 0)
                {
                    return false;
                }

                var problem = Problem.Create(FailureTitle, FailureDetail);
                var failed = Result<string>.Fail(problem);
                try { _ = Read(failed); return false; }
                catch (ProblemException exception)
                {
                    if (!ReferenceEquals(exception.Problem, problem)) { return false; }
                }

                var nullable = Result<string?>.Fail(problem);
                try { _ = ReadNullable(nullable); return false; }
                catch (ProblemException exception)
                {
                    if (!ReferenceEquals(exception.Problem, problem)) { return false; }
                }

                var page = CollectionResult<string>.Fail(problem);
                try { _ = ReadCollection(page); return false; }
                catch (ProblemException exception)
                {
                    if (!ReferenceEquals(exception.Problem, problem)) { return false; }
                }

                var emptyPage = default(CollectionResult<string>);
                if (emptyPage.Value.Length != 0 || emptyPage.Collection.Length != 0) { return false; }
                try { _ = Read(default); return false; }
                catch (ProblemException exception)
                {
                    if (exception.Problem is null) { return false; }
                }
                try { _ = ReadNullable(default); return false; }
                catch (ProblemException exception)
                {
                    if (exception.Problem is null) { return false; }
                }
                try { _ = ReadCollection(emptyPage); return false; }
                catch (ProblemException exception)
                {
                    return exception.Problem is not null;
                }
            }
        }
        """;

    [Test]
    public void ThrowIfProblem_ConsumerCompilesWithoutNullableWarnings_ExecutesSuccessFailureAndDefault()
    {
        var references = ((string)AppContext.GetData(TrustedAssembliesKey)!).Split(Path.PathSeparator)
            .Append(typeof(Result).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(AssemblyName,
            [CSharpSyntaxTree.ParseText(Consumer)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable, generalDiagnosticOption: ReportDiagnostic.Error));
        using var image = new MemoryStream();
        var emitted = compilation.Emit(image);
        emitted.Success.ShouldBeTrue(string.Join(Environment.NewLine, emitted.Diagnostics));
        emitted.Diagnostics.ShouldBeEmpty();
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

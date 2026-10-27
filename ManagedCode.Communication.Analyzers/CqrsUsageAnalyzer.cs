using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ManagedCode.Communication.Analyzers;

/// <summary>Recommends the native CQRS result conversion and payload factories.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CqrsUsageAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Convert the existing result directly to a terminal chunk.</summary>
    public const string ResultConversionId = "MCC0005";
    /// <summary>Use the chunk factory's raw payload overload.</summary>
    public const string PayloadFactoryId = "MCC0006";
    /// <summary>The terminal factory contradicts its result status.</summary>
    public const string TerminalStatusId = "MCC0007";
    internal const string ChunkMetadataName = "ManagedCode.Communication.CQRS.CqrsStreamChunk`2";
    private const string Category = "Usage";
    private const string ResultConversionTitle = "Use the native CQRS result conversion";
    private const string ResultConversionMessage = "Convert '{0}' directly to a terminal chunk; use 'yield return {0};' in an iterator or the FromResult factory";
    private const string PayloadFactoryTitle = "Use the CQRS payload factory";
    private const string PayloadFactoryMessage = "Pass the payload directly to {0}; the chunk factory already creates the successful result";
    private const string SuccessName = "IsSuccess";
    private const string FailureName = "IsFailed";
    private const string CompletedName = "Completed";
    private const string FailedName = "Failed";
    private const string ProgressName = "Progress";
    private const string SucceedName = "Succeed";
    private const string FailPrefix = "Fail";
    private const string TerminalStatusTitle = "Match the terminal chunk to the result status";
    private const string TerminalStatusMessage = "{0} contradicts the supplied result status and will throw; use FromResult to preserve the outcome";
    private static readonly DiagnosticDescriptor ResultConversion = new(
        ResultConversionId, ResultConversionTitle, ResultConversionMessage, Category, DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor PayloadFactory = new(
        PayloadFactoryId, PayloadFactoryTitle, PayloadFactoryMessage, Category, DiagnosticSeverity.Warning, true);

    private static readonly DiagnosticDescriptor TerminalStatus = new(
        TerminalStatusId, TerminalStatusTitle, TerminalStatusMessage, Category, DiagnosticSeverity.Error, true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ResultConversion, PayloadFactory, TerminalStatus);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(start =>
        {
            var chunkType = start.Compilation.GetTypeByMetadataName(ChunkMetadataName);
            if (chunkType?.ContainingAssembly.Name != ResultUsageAnalyzer.AssemblyName)
            {
                return;
            }
            start.RegisterOperationAction(node => AnalyzeConditional(node, chunkType), OperationKind.Conditional);
            start.RegisterOperationAction(node => AnalyzePayloadFactory(node, chunkType), OperationKind.Invocation);
        });
    }

    private static void AnalyzeConditional(OperationAnalysisContext context, INamedTypeSymbol chunkType)
    {
        if (context.Operation is not IConditionalOperation conditional ||
            Unwrap(conditional.WhenTrue) is not IInvocationOperation completed ||
            Unwrap(conditional.WhenFalse) is not IInvocationOperation failed ||
            !IsChunkFactory(completed, chunkType) || !IsChunkFactory(failed, chunkType) ||
            !SymbolEqualityComparer.IncludeNullability.Equals(completed.Type, failed.Type) ||
            SymbolEqualityComparer.Default.Equals(context.ContainingSymbol.ContainingType?.OriginalDefinition, chunkType) ||
            !TryStatus(conditional.Condition, out var receiver, out var success))
        {
            return;
        }
        if (completed.TargetMethod.Name != (success ? CompletedName : FailedName) ||
            failed.TargetMethod.Name != (success ? FailedName : CompletedName) ||
            !TrySingleArgument(completed, out var first) || !TrySingleArgument(failed, out var second) ||
            !SymbolEqualityComparer.Default.Equals(ReferencedSymbol(first), receiver) ||
            !SymbolEqualityComparer.Default.Equals(ReferencedSymbol(second), receiver))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(ResultConversion, conditional.Syntax.GetLocation(), receiver.Name));
    }

    private static void AnalyzePayloadFactory(OperationAnalysisContext context, INamedTypeSymbol chunkType)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name is CompletedName or FailedName && IsChunkFactory(invocation, chunkType) &&
            ResultUsageAnalyzer.IsNativeResultType(invocation.TargetMethod.OriginalDefinition.Parameters[0].Type))
        {
            foreach (var terminalArgument in invocation.Arguments)
            {
                if (terminalArgument.Parameter?.Ordinal != 0 || Unwrap(terminalArgument.Value) is not IInvocationOperation resultFactory ||
                    !ResultUsageAnalyzer.IsNativeResultType(resultFactory.TargetMethod.ContainingType)) continue;
                if (invocation.TargetMethod.Name == CompletedName && resultFactory.TargetMethod.Name.StartsWith(FailPrefix, StringComparison.Ordinal) ||
                    invocation.TargetMethod.Name == FailedName && resultFactory.TargetMethod.Name == SucceedName)
                    context.ReportDiagnostic(Diagnostic.Create(TerminalStatus, invocation.Syntax.GetLocation(), invocation.TargetMethod.Name));
                break;
            }
        }
        if (invocation.TargetMethod.Name is not (ProgressName or CompletedName) ||
            !IsChunkFactory(invocation, chunkType) ||
            SymbolEqualityComparer.Default.Equals(context.ContainingSymbol.ContainingType?.OriginalDefinition, chunkType) ||
            !TrySingleArgument(invocation, out var argument) || Unwrap(argument) is not IInvocationOperation factory ||
            factory.TargetMethod.Name != SucceedName || !ResultUsageAnalyzer.IsNativeResultType(factory.TargetMethod.ContainingType) ||
            factory.Type is not INamedTypeSymbol { TypeArguments.Length: 1 } resultType ||
            !TrySingleArgument(factory, out var payload) ||
            !SymbolEqualityComparer.IncludeNullability.Equals(Unwrap(payload)?.Type, resultType.TypeArguments[0]))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(PayloadFactory, factory.Syntax.GetLocation(), invocation.TargetMethod.Name));
    }

    private static bool IsChunkFactory(IInvocationOperation invocation, INamedTypeSymbol chunkType) =>
        invocation.TargetMethod.IsStatic &&
        SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType.OriginalDefinition, chunkType);

    private static bool TrySingleArgument(IInvocationOperation invocation, out IOperation argument)
    {
        argument = null!;
        foreach (var item in invocation.Arguments)
        {
            if (item.IsImplicit)
            {
                continue;
            }
            if (argument is not null || item.Parameter?.Ordinal != 0)
            {
                return false;
            }
            argument = item.Value;
        }
        return argument is not null;
    }

    private static bool TryStatus(IOperation operation, out ISymbol receiver, out bool success)
    {
        var condition = Unwrap(operation);
        var negated = false;
        while (condition is IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } unary)
        {
            negated = !negated;
            condition = Unwrap(unary.Operand);
        }
        if (condition is IPropertyReferenceOperation property &&
            property.Property.Name is SuccessName or FailureName &&
            ResultUsageAnalyzer.IsNativeResultType(property.Property.ContainingType) &&
            property.Instance is { } instance && ReferencedSymbol(instance) is { } symbol)
        {
            receiver = symbol;
            success = (property.Property.Name == SuccessName) != negated;
            return true;
        }
        receiver = null!;
        success = false;
        return false;
    }

    private static ISymbol? ReferencedSymbol(IOperation operation) => Unwrap(operation) switch
    {
        ILocalReferenceOperation local => local.Local,
        IParameterReferenceOperation parameter => parameter.Parameter,
        _ => null
    };

    private static IOperation? Unwrap(IOperation? operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation { IsImplicit: true } conversion:
                    operation = conversion.Operand;
                    continue;
                case IParenthesizedOperation parentheses:
                    operation = parentheses.Operand;
                    continue;
                default:
                    return operation;
            }
        }
    }
}

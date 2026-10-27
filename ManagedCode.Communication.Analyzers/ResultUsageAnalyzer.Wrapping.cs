using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ManagedCode.Communication.Analyzers;

public sealed partial class ResultUsageAnalyzer
{
    /// <summary>Avoid another result layer or rebuilding an existing result of the same type.</summary>
    public const string RedundantWrappingId = "MCC0004";
    private const string WrappingTitle = "Avoid redundant result wrapping";
    private const string WrappingMessage = "{0}";
    private const string NestedResultMessage = "A result payload already contains a result; return one result layer";
    private const string ReuseResultMessage = "Return '{0}' directly instead of rebuilding the same result";
    private const string SucceedMethodName = "Succeed";
    private const string FailMethodName = "Fail";
    private const string FromMethodName = "From";
    private const string CollectionTypeName = "CollectionResult";
    private const string CollectionInterfaceName = "IResultCollection";
    internal const string ProblemMetadataName = "ManagedCode.Communication.Problem";
    private static readonly DiagnosticDescriptor RedundantWrapping = new(
        RedundantWrappingId, WrappingTitle, WrappingMessage, Category, DiagnosticSeverity.Warning, true);

    private static void AnalyzeNestedResult(SyntaxNodeAnalysisContext context, ConcurrentDictionary<ITypeSymbol, bool> nestedResults)
    {
        if (((GenericNameSyntax)context.Node).Identifier.ValueText is not (ResultType or ResultInterface or CollectionTypeName or CollectionInterfaceName))
        {
            return;
        }
        if (context.SemanticModel.GetTypeInfo(context.Node, context.CancellationToken).Type is { } type && nestedResults.GetOrAdd(type, HasNestedResult))
        {
            context.ReportDiagnostic(Diagnostic.Create(RedundantWrapping, context.Node.GetLocation(), NestedResultMessage));
        }
    }

    private static void AnalyzeResultWrapping(OperationAnalysisContext context, ConcurrentDictionary<ITypeSymbol, bool> nestedResults, INamedTypeSymbol? problemType)
    {
        if (context.Operation.Type is { } type && IsNativeResultType(type) && nestedResults.GetOrAdd(type, HasNestedResult))
        {
            context.ReportDiagnostic(Diagnostic.Create(RedundantWrapping, context.Operation.Syntax.GetLocation(), NestedResultMessage));
            return;
        }
        if (context.Operation is not IInvocationOperation invocation ||
            !IsNativeResultType(invocation.TargetMethod.ContainingType) ||
            !IsNativeResultType(invocation.Type) || invocation.Arguments.Length != 1)
        {
            return;
        }
        var argument = invocation.Arguments[0].Value;
        while (argument is IConversionOperation conversion)
        {
            argument = conversion.Operand;
        }
        var model = invocation.SemanticModel;
        if (model is null)
        {
            return;
        }
        ExpressionSyntax? receiver = null;
        if (invocation.TargetMethod.Name == FromMethodName && argument.Syntax is ExpressionSyntax unchanged &&
            SymbolEqualityComparer.IncludeNullability.Equals(invocation.Type, argument.Type))
        {
            receiver = unchanged;
        }
        else if (argument.Syntax is ExpressionSyntax expression && Unwrap(expression) is MemberAccessExpressionSyntax member &&
            IsResultProperty(member, model, context.CancellationToken) &&
            SymbolEqualityComparer.IncludeNullability.Equals(invocation.Type,
                model.GetTypeInfo(member.Expression, context.CancellationToken).Type) &&
            (invocation.TargetMethod.Name == SucceedMethodName && member.Name.Identifier.ValueText is ValueName or CollectionName &&
                ResultStatusFlow.ProvesStatus(invocation.Syntax, member.Expression, true, model, context.CancellationToken) ||
             invocation.TargetMethod.Name == FailMethodName && member.Name.Identifier.ValueText == ProblemName &&
                SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.Parameters[0].OriginalDefinition.Type,
                    problemType) &&
                ResultStatusFlow.ProvesStatus(invocation.Syntax, member.Expression, false, model, context.CancellationToken)))
        {
            receiver = member.Expression;
        }
        if (receiver is not null && model.GetSymbolInfo(receiver, context.CancellationToken).Symbol is ILocalSymbol or IParameterSymbol)
        {
            context.ReportDiagnostic(Diagnostic.Create(RedundantWrapping, invocation.Syntax.GetLocation(),
                string.Format(System.Globalization.CultureInfo.InvariantCulture, ReuseResultMessage, receiver.ToString())));
        }
    }

    private static bool HasNestedResult(ITypeSymbol type)
    {
        if (!IsNativeResultType(type) || type is not INamedTypeSymbol { IsUnboundGenericType: false } named)
        {
            return false;
        }
        foreach (var argument in named.TypeArguments)
        {
            if (ContainsResult(argument, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)))
            {
                return true;
            }
        }
        return false;
    }

    private static bool ContainsResult(ITypeSymbol type, HashSet<ITypeSymbol> visited)
    {
        if (!visited.Add(type))
        {
            return false;
        }
        if (IsNativeResultType(type))
        {
            return true;
        }
        if (type is IArrayTypeSymbol array)
        {
            return ContainsResult(array.ElementType, visited);
        }
        if (type is ITypeParameterSymbol parameter)
        {
            foreach (var constraint in parameter.ConstraintTypes)
            {
                if (ContainsResult(constraint, visited))
                {
                    return true;
                }
            }
        }
        if (type is INamedTypeSymbol named)
        {
            foreach (var argument in named.TypeArguments)
            {
                if (ContainsResult(argument, visited))
                {
                    return true;
                }
            }
        }
        return false;
    }
}

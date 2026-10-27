using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ManagedCode.Communication.Analyzers;

/// <summary>Enforces the status and nullable payload contracts of Communication results.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class ResultUsageAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Use the dedicated failure status property.</summary>
    public const string NegatedSuccessId = "MCC0001";
    /// <summary>Trust the result member's proven non-null contract.</summary>
    public const string RedundantNullGuardId = "MCC0002";
    private const string Category = "Usage";
    internal const string AssemblyName = "ManagedCode.Communication";
    private const string ResultMetadataName = "ManagedCode.Communication.Result";
    private const string ResultNamespace = "ManagedCode.Communication";
    private const string CollectionNamespace = "ManagedCode.Communication.CollectionResultT";
    private const string SuccessName = "IsSuccess";
    private const string ProblemName = "Problem";
    private const string ValueName = "Value";
    private const string CollectionName = "Collection";
    private const string ResultType = "Result";
    private const string GenericResultType = "Result`1";
    private const string CollectionType = "CollectionResult`1";
    private const string ResultInterface = "IResult";
    private const string GenericResultInterface = "IResult`1";
    private const string ProblemInterface = "IResultProblem";
    private const string CollectionInterface = "IResultCollection`1";
    private const string NegatedTitle = "Use IsFailed for failure checks";
    private const string NegatedMessage = "Use IsFailed instead of negating IsSuccess";
    private const string NullThrowMethodName = "ThrowIfNull";
    private const string NullThrowMetadataName = "System.ArgumentNullException";
    private const string NullGuardTitle = "Trust the result nullability contract";
    private const string NullGuardMessage = "'{0}' is already non-null here; remove the redundant null guard or suppression";

    private static readonly DiagnosticDescriptor NegatedSuccess = new(
        NegatedSuccessId, NegatedTitle, NegatedMessage, Category, DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RedundantNullGuard = new(
        RedundantNullGuardId, NullGuardTitle, NullGuardMessage, Category, DiagnosticSeverity.Error, true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(NegatedSuccess, RedundantNullGuard, RedundantWrapping);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(start =>
        {
            if (start.Compilation.GetTypeByMetadataName(ResultMetadataName) is null)
            {
                return;
            }
            var nestedResults = new ConcurrentDictionary<ITypeSymbol, bool>(SymbolEqualityComparer.Default);
            var nullThrowType = start.Compilation.GetTypeByMetadataName(NullThrowMetadataName);
            var problemType = start.Compilation.GetTypeByMetadataName(ProblemMetadataName);
            start.RegisterSyntaxNodeAction(AnalyzeNegation, SyntaxKind.LogicalNotExpression,
                SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression, SyntaxKind.IsPatternExpression);
            start.RegisterSyntaxNodeAction(AnalyzeGuard, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression,
                SyntaxKind.IsPatternExpression, SyntaxKind.CoalesceExpression, SyntaxKind.ConditionalAccessExpression,
                SyntaxKind.SuppressNullableWarningExpression);
            start.RegisterSyntaxNodeAction(node => AnalyzeNullThrowGuard(node, nullThrowType), SyntaxKind.InvocationExpression);
            start.RegisterSyntaxNodeAction(node => AnalyzeNestedResult(node, nestedResults), SyntaxKind.GenericName);
            start.RegisterOperationAction(node => AnalyzeResultWrapping(node, nestedResults, problemType),
                OperationKind.Invocation, OperationKind.ObjectCreation);
        });
    }

    private static void AnalyzeNegation(SyntaxNodeAnalysisContext context)
    {
        ExpressionSyntax? expression = context.Node switch
        {
            PrefixUnaryExpressionSyntax prefix => Unwrap(prefix.Operand),
            BinaryExpressionSyntax binary when IsNegatingConstant(binary.Right, binary.Kind()) => Unwrap(binary.Left),
            BinaryExpressionSyntax binary when IsNegatingConstant(binary.Left, binary.Kind()) => Unwrap(binary.Right),
            IsPatternExpressionSyntax pattern when IsFailurePattern(pattern.Pattern) => Unwrap(pattern.Expression),
            _ => null
        };
        if (expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == SuccessName &&
            IsResultProperty(member, context.SemanticModel, context.CancellationToken))
        {
            context.ReportDiagnostic(Diagnostic.Create(NegatedSuccess, context.Node.GetLocation()));
        }
    }

    private static bool IsNegatingConstant(ExpressionSyntax expression, SyntaxKind comparison) =>
        comparison == SyntaxKind.EqualsExpression && Unwrap(expression).IsKind(SyntaxKind.FalseLiteralExpression) ||
        comparison == SyntaxKind.NotEqualsExpression && Unwrap(expression).IsKind(SyntaxKind.TrueLiteralExpression);

    private static bool IsFailurePattern(PatternSyntax pattern) => pattern switch
    {
        ParenthesizedPatternSyntax parentheses => IsFailurePattern(parentheses.Pattern),
        ConstantPatternSyntax constant => constant.Expression.IsKind(SyntaxKind.FalseLiteralExpression),
        UnaryPatternSyntax { Pattern: ConstantPatternSyntax constant } unary when unary.IsKind(SyntaxKind.NotPattern) =>
            constant.Expression.IsKind(SyntaxKind.TrueLiteralExpression),
        _ => false
    };

    private static void AnalyzeGuard(SyntaxNodeAnalysisContext context)
    {
        ExpressionSyntax? expression = context.Node switch
        {
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.CoalesceExpression) => binary.Left,
            BinaryExpressionSyntax binary when binary.Left.IsKind(SyntaxKind.NullLiteralExpression) => binary.Right,
            BinaryExpressionSyntax binary when binary.Right.IsKind(SyntaxKind.NullLiteralExpression) => binary.Left,
            IsPatternExpressionSyntax pattern when IsNullPattern(pattern.Pattern) => pattern.Expression,
            ConditionalAccessExpressionSyntax conditional => conditional.Expression,
            PostfixUnaryExpressionSyntax postfix => postfix.Operand,
            _ => null
        };
        ReportGuard(context, expression);
    }

    private static void AnalyzeNullThrowGuard(SyntaxNodeAnalysisContext context, INamedTypeSymbol? nullThrowType)
    {
        // Most invocations cannot be ThrowIfNull; avoid constructing their operation trees here.
        var invocation = (InvocationExpressionSyntax)context.Node;
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null
        };
        if (name != NullThrowMethodName ||
            context.SemanticModel.GetOperation(context.Node, context.CancellationToken) is not IInvocationOperation operation ||
            operation.TargetMethod.Name != NullThrowMethodName ||
            !SymbolEqualityComparer.Default.Equals(operation.TargetMethod.ContainingType,
                nullThrowType))
        {
            return;
        }
        foreach (var argument in operation.Arguments)
        {
            if (argument.Parameter?.Ordinal == 0)
            {
                ReportGuard(context, argument.Value.Syntax as ExpressionSyntax);
                return;
            }
        }
    }

    private static void ReportGuard(SyntaxNodeAnalysisContext context, ExpressionSyntax? expression)
    {
        if (expression is null || Unwrap(expression) is not MemberAccessExpressionSyntax member ||
            member.Name.Identifier.ValueText is not (ProblemName or ValueName or CollectionName) ||
            !IsResultProperty(member, context.SemanticModel, context.CancellationToken))
        {
            return;
        }
        // User-defined equality can mean more than a null check.
        if (context.Node is BinaryExpressionSyntax comparison && !comparison.IsKind(SyntaxKind.CoalesceExpression) &&
            context.SemanticModel.GetOperation(comparison, context.CancellationToken) is IBinaryOperation { OperatorMethod: { } method } &&
            method.ContainingType.SpecialType != SpecialType.System_String)
        {
            return;
        }
        var nullability = context.SemanticModel.GetTypeInfo(member, context.CancellationToken).Nullability;
        var property = (IPropertySymbol)context.SemanticModel.GetSymbolInfo(member, context.CancellationToken).Symbol!;
        var proven = property.NullableAnnotation == NullableAnnotation.NotAnnotated && property.Type.IsReferenceType ||
            ResultStatusFlow.ProvesMember(context.Node, member, context.SemanticModel, context.CancellationToken);
        if (!context.Node.IsKind(SyntaxKind.SuppressNullableWarningExpression))
        {
            proven |= nullability.FlowState == NullableFlowState.NotNull;
        }
        if (nullability.Annotation != NullableAnnotation.None && proven)
        {
            context.ReportDiagnostic(Diagnostic.Create(RedundantNullGuard, context.Node.GetLocation(), member.Name.Identifier.ValueText));
        }
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parentheses)
        {
            expression = parentheses.Expression;
        }
        return expression;
    }

    private static bool IsNullPattern(PatternSyntax pattern)
    {
        while (true)
        {
            switch (pattern)
            {
                case ParenthesizedPatternSyntax parentheses:
                    pattern = parentheses.Pattern;
                    continue;
                case UnaryPatternSyntax unary when unary.IsKind(SyntaxKind.NotPattern):
                    pattern = unary.Pattern;
                    continue;
                default:
                    return pattern is ConstantPatternSyntax constant && constant.Expression.IsKind(SyntaxKind.NullLiteralExpression) ||
                        pattern is RecursivePatternSyntax
                        {
                            Type: null, PositionalPatternClause: null,
                            PropertyPatternClause.Subpatterns.Count: 0
                        };
            }
        }
    }

    internal static bool IsNativeResultType(ITypeSymbol? symbol) => symbol is INamedTypeSymbol type &&
        type.ContainingAssembly?.Name == AssemblyName &&
        type.MetadataName is ResultType or GenericResultType or CollectionType or
            ResultInterface or GenericResultInterface or CollectionInterface &&
        type.ContainingNamespace?.ToDisplayString() is ResultNamespace or CollectionNamespace;

    internal static bool IsResultProperty(MemberAccessExpressionSyntax member, SemanticModel model, System.Threading.CancellationToken token)
    {
        return model.GetSymbolInfo(member, token).Symbol is IPropertySymbol property &&
            property.ContainingAssembly.Name == AssemblyName &&
            property.ContainingType.MetadataName is ResultType or GenericResultType or CollectionType or
                ResultInterface or GenericResultInterface or ProblemInterface or CollectionInterface &&
            property.ContainingNamespace.ToDisplayString() is ResultNamespace or CollectionNamespace;
    }
}

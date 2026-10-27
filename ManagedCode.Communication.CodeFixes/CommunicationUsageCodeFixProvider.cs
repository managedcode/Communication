using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ManagedCode.Communication.Analyzers;

/// <summary>Applies Communication simplifications proven by compiler diagnostics.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommunicationUsageCodeFixProvider)), Shared]
public sealed class CommunicationUsageCodeFixProvider : CodeFixProvider
{
    private const string FailureName = "IsFailed";
    private const string FromResultName = "FromResult";
    private const string FromName = "From";
    private const string ValueName = "Value";
    private const string ProblemName = "Problem";
    private const string CollectionName = "Collection";
    private const string SuccessTitle = "Use IsFailed";
    private const string ReuseTitle = "Reuse the existing result";
    private const string ConversionTitle = "Use the native terminal result conversion";
    private const string PayloadTitle = "Pass the payload directly";
    private const string NullGuardTitle = "Remove the redundant result null guard";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        ResultUsageAnalyzer.NegatedSuccessId, ResultUsageAnalyzer.RedundantNullGuardId,
        ResultUsageAnalyzer.RedundantWrappingId, CqrsUsageAnalyzer.ResultConversionId, CqrsUsageAnalyzer.PayloadFactoryId, CqrsUsageAnalyzer.TerminalStatusId);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;
        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        if (node.ContainsDirectives) return;
        SyntaxNode? replacement = null;
        var title = diagnostic.Id switch
        {
            ResultUsageAnalyzer.NegatedSuccessId => SuccessTitle,
            ResultUsageAnalyzer.RedundantNullGuardId => NullGuardTitle,
            ResultUsageAnalyzer.RedundantWrappingId => ReuseTitle,
            CqrsUsageAnalyzer.ResultConversionId => ConversionTitle,
            CqrsUsageAnalyzer.TerminalStatusId => ConversionTitle,
            _ => PayloadTitle
        };
        switch (diagnostic.Id)
        {
            case ResultUsageAnalyzer.NegatedSuccessId:
                var checkedExpression = node switch
                {
                    PrefixUnaryExpressionSyntax prefix => Unwrap(prefix.Operand),
                    BinaryExpressionSyntax binary => Unwrap(binary.Left) is MemberAccessExpressionSyntax ? Unwrap(binary.Left) : Unwrap(binary.Right),
                    IsPatternExpressionSyntax pattern => Unwrap(pattern.Expression),
                    _ => null
                };
                if (checkedExpression is MemberAccessExpressionSyntax member)
                    replacement = member.WithName(SyntaxFactory.IdentifierName(FailureName).WithTriviaFrom(member.Name));
                break;
            case ResultUsageAnalyzer.RedundantNullGuardId:
                replacement = RemoveNullGuard(node);
                break;
            case ResultUsageAnalyzer.RedundantWrappingId:
                if (node is InvocationExpressionSyntax invocation && invocation.ArgumentList.Arguments.Count == 1)
                {
                    var argument = Unwrap(invocation.ArgumentList.Arguments[0].Expression);
                    var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                    if (model is not null && SymbolEqualityComparer.IncludeNullability.Equals(
                            model.GetTypeInfo(invocation, context.CancellationToken).Type,
                            model.GetTypeInfo(argument, context.CancellationToken).Type) &&
                        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: FromName })
                        replacement = argument;
                    else if (argument is MemberAccessExpressionSyntax { Name.Identifier.ValueText: ValueName or ProblemName or CollectionName } payload &&
                        model is not null && SymbolEqualityComparer.IncludeNullability.Equals(
                            model.GetTypeInfo(invocation, context.CancellationToken).Type,
                            model.GetTypeInfo(payload.Expression, context.CancellationToken).Type))
                        replacement = payload.Expression;
                }
                break;
            case CqrsUsageAnalyzer.ResultConversionId:
                if (node is ConditionalExpressionSyntax conditional && Unwrap(conditional.WhenTrue) is InvocationExpressionSyntax factory &&
                    factory.Expression is MemberAccessExpressionSyntax method && factory.ArgumentList.Arguments.Count == 1)
                    replacement = node.Parent is YieldStatementSyntax
                        ? factory.ArgumentList.Arguments[0].Expression
                        : factory.WithExpression(method.WithName(SyntaxFactory.IdentifierName(FromResultName).WithTriviaFrom(method.Name)));
                break;
            case CqrsUsageAnalyzer.TerminalStatusId:
                if (node is InvocationExpressionSyntax terminal && terminal.Expression is MemberAccessExpressionSyntax terminalMethod)
                    replacement = terminal.WithExpression(terminalMethod.WithName(SyntaxFactory.IdentifierName(FromResultName).WithTriviaFrom(terminalMethod.Name)));
                break;
            case CqrsUsageAnalyzer.PayloadFactoryId:
                if (node is InvocationExpressionSyntax succeed && succeed.ArgumentList.Arguments.Count == 1)
                    replacement = succeed.ArgumentList.Arguments[0].Expression;
                break;
        }
        if (replacement is not null)
        {
            var updated = root.ReplaceNode(node, replacement.WithTriviaFrom(node));
            context.RegisterCodeFix(CodeAction.Create(title,
                _ => Task.FromResult(context.Document.WithSyntaxRoot(updated)), diagnostic.Id), diagnostic);
        }
        else if (diagnostic.Id == ResultUsageAnalyzer.RedundantNullGuardId && node is InvocationExpressionSyntax call &&
            call.ArgumentList.Arguments.Count == 1 && call.Parent is ExpressionStatementSyntax statement && statement.Parent is BlockSyntax)
        {
            var updated = root.RemoveNode(statement, SyntaxRemoveOptions.KeepExteriorTrivia);
            if (updated is not null)
                context.RegisterCodeFix(CodeAction.Create(title,
                    _ => Task.FromResult(context.Document.WithSyntaxRoot(updated)), diagnostic.Id), diagnostic);
        }
    }

    private static ExpressionSyntax? RemoveNullGuard(SyntaxNode node)
    {
        if (node is PostfixUnaryExpressionSyntax suppression) return suppression.Operand;
        if (node is BinaryExpressionSyntax binary)
        {
            if (binary.IsKind(SyntaxKind.CoalesceExpression)) return binary.Left;
            return SyntaxFactory.LiteralExpression(binary.IsKind(SyntaxKind.NotEqualsExpression)
                ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
        }
        if (node is IsPatternExpressionSyntax pattern && !pattern.Pattern.DescendantNodesAndSelf().OfType<VariableDesignationSyntax>().Any())
        {
            PatternSyntax current = pattern.Pattern;
            var notNull = false;
            while (current is ParenthesizedPatternSyntax or UnaryPatternSyntax)
            {
                if (current is ParenthesizedPatternSyntax parentheses) current = parentheses.Pattern;
                else if (current is UnaryPatternSyntax unary)
                {
                    notNull = !notNull;
                    current = unary.Pattern;
                }
            }
            var matches = current is RecursivePatternSyntax ? !notNull : notNull;
            return SyntaxFactory.LiteralExpression(matches ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
        }
        if (node is ConditionalAccessExpressionSyntax conditional)
        {
            var binding = conditional.WhenNotNull.DescendantNodesAndSelf().OfType<MemberBindingExpressionSyntax>().FirstOrDefault();
            if (binding is not null)
                return conditional.WhenNotNull.ReplaceNode(binding,
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, conditional.Expression, binding.Name));
        }
        return null;
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parentheses) expression = parentheses.Expression;
        return expression;
    }
}

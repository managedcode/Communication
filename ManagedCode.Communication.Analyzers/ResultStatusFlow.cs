using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ManagedCode.Communication.Analyzers;

internal static class ResultStatusFlow
{
    private const string AnnotationType = "System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute";

    internal static bool ProvesMember(SyntaxNode node, MemberAccessExpressionSyntax member, SemanticModel model, CancellationToken token)
    {
        var receiver = model.GetSymbolInfo(member.Expression, token).Symbol;
        if (receiver is not (ILocalSymbol or IParameterSymbol))
        {
            return false;
        }
        for (var current = node; current.Parent is not null; current = current.Parent)
        {
            if (current is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax || Writes(current, receiver, model, token))
            {
                return false;
            }
            if (current.Parent is BinaryExpressionSyntax binary && current == binary.Right &&
                (binary.IsKind(SyntaxKind.LogicalAndExpression) && Guarantees(binary.Left, true, member, receiver, model, token) ||
                 binary.IsKind(SyntaxKind.LogicalOrExpression) && Guarantees(binary.Left, false, member, receiver, model, token)))
            {
                return true;
            }
            if (current.Parent is ConditionalExpressionSyntax conditional &&
                (current == conditional.WhenTrue && Guarantees(conditional.Condition, true, member, receiver, model, token) ||
                 current == conditional.WhenFalse && Guarantees(conditional.Condition, false, member, receiver, model, token)))
            {
                return true;
            }
            if (current.Parent is IfStatementSyntax parentIf && current == parentIf.Statement &&
                Guarantees(parentIf.Condition, true, member, receiver, model, token) ||
                current.Parent is ElseClauseSyntax { Parent: IfStatementSyntax elseIf } &&
                Guarantees(elseIf.Condition, false, member, receiver, model, token))
            {
                return true;
            }
            if (current is StatementSyntax statement && statement.Parent is BlockSyntax block)
            {
                for (int index = block.Statements.IndexOf(statement) - 1; index >= 0; index--)
                {
                    var preceding = block.Statements[index];
                    if (Writes(preceding, receiver, model, token))
                    {
                        return false;
                    }
                    if (preceding is IfStatementSyntax { Else: null } guard &&
                        model.AnalyzeControlFlow(guard.Statement) is { Succeeded: true, EndPointIsReachable: false } &&
                        Guarantees(guard.Condition, false, member, receiver, model, token))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool Writes(SyntaxNode node, ISymbol receiver, SemanticModel model, CancellationToken token)
    {
        return node.DescendantNodesAndSelf().Any(part => part switch
        {
            AssignmentExpressionSyntax assignment => References(assignment.Left, receiver, model, token),
            ArgumentSyntax argument when argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) =>
                References(argument.Expression, receiver, model, token),
            RefExpressionSyntax reference => References(reference.Expression, receiver, model, token),
            PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression) =>
                References(prefix.Operand, receiver, model, token),
            PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression) =>
                References(postfix.Operand, receiver, model, token),
            VariableDeclaratorSyntax variable => SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(variable, token), receiver),
            _ => false
        });
    }

    private static bool References(ExpressionSyntax expression, ISymbol receiver, SemanticModel model, CancellationToken token) =>
        expression.DescendantNodesAndSelf().OfType<ExpressionSyntax>()
            .Any(part => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(part, token).Symbol, receiver));

    private static bool Guarantees(ExpressionSyntax condition, bool outcome, MemberAccessExpressionSyntax member,
        ISymbol receiver, SemanticModel model, CancellationToken token)
    {
        if (Writes(condition, receiver, model, token))
        {
            return false;
        }
        if (condition is ParenthesizedExpressionSyntax parentheses)
        {
            return Guarantees(parentheses.Expression, outcome, member, receiver, model, token);
        }
        if (condition is PrefixUnaryExpressionSyntax negation && negation.IsKind(SyntaxKind.LogicalNotExpression))
        {
            return Guarantees(negation.Operand, !outcome, member, receiver, model, token);
        }
        if (condition is BinaryExpressionSyntax binary &&
            (outcome && binary.IsKind(SyntaxKind.LogicalAndExpression) || !outcome && binary.IsKind(SyntaxKind.LogicalOrExpression)))
        {
            return Guarantees(binary.Left, outcome, member, receiver, model, token) ||
                   Guarantees(binary.Right, outcome, member, receiver, model, token);
        }
        if (condition is not MemberAccessExpressionSyntax status || !ResultUsageAnalyzer.IsResultProperty(status, model, token) ||
            !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(status.Expression, token).Symbol, receiver) ||
            model.GetSymbolInfo(status, token).Symbol is not IPropertySymbol property)
        {
            return false;
        }
        return property.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == AnnotationType &&
            attribute.ConstructorArguments.Length == 2 && attribute.ConstructorArguments[0].Value is bool expected && expected == outcome &&
            (attribute.ConstructorArguments[1].Value is string name && name == member.Name.Identifier.ValueText ||
             attribute.ConstructorArguments[1].Kind == TypedConstantKind.Array &&
             attribute.ConstructorArguments[1].Values.Any(value => value.Value is string item && item == member.Name.Identifier.ValueText)));
    }
}

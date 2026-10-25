using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ManagedCode.Communication.Analyzers;

internal static class ResultStatusFlow
{
    private const string AnnotationType = "System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute";
    private const string PostconditionAnnotationType = "System.Diagnostics.CodeAnalysis.MemberNotNullAttribute";
    private const string SuccessName = "IsSuccess";
    private const string FailureName = "IsFailed";
    private const string ThrowIfProblemName = "ThrowIfProblem";

    internal static bool ProvesMember(SyntaxNode node, MemberAccessExpressionSyntax member, SemanticModel model, CancellationToken token)
    {
        var receiver = model.GetSymbolInfo(member.Expression, token).Symbol;
        return receiver is ILocalSymbol or IParameterSymbol && Proves(node, receiver, model, token,
            (condition, outcome) => Guarantees(condition, outcome, member, receiver, model, token),
            preceding => InvocationGuaranteesMember(preceding, member, receiver, model, token));
    }

    internal static bool ProvesStatus(SyntaxNode node, ExpressionSyntax expression, bool success,
        SemanticModel model, CancellationToken token)
    {
        var receiver = model.GetSymbolInfo(expression, token).Symbol;
        return receiver is ILocalSymbol or IParameterSymbol && Proves(node, receiver, model, token,
            (condition, outcome) => GuaranteesStatus(condition, outcome, success, receiver, model, token),
            preceding => success && IsThrowIfProblem(preceding, receiver, model, token));
    }

    private static bool Proves(SyntaxNode node, ISymbol receiver, SemanticModel model, CancellationToken token,
        Func<ExpressionSyntax, bool, bool> guarantees, Func<StatementSyntax, bool> postcondition)
    {
        var scope = node.Ancestors().FirstOrDefault(ancestor => ancestor is BaseMethodDeclarationSyntax or
            AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax) ?? node.SyntaxTree.GetRoot(token);
        if (scope.DescendantNodes().OfType<RefExpressionSyntax>().Any(reference =>
                References(reference.Expression, receiver, model, token)))
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
                (binary.IsKind(SyntaxKind.LogicalAndExpression) && guarantees(binary.Left, true) ||
                 binary.IsKind(SyntaxKind.LogicalOrExpression) && guarantees(binary.Left, false)))
            {
                return true;
            }
            if (current.Parent is ConditionalExpressionSyntax conditional &&
                (current == conditional.WhenTrue && guarantees(conditional.Condition, true) ||
                 current == conditional.WhenFalse && guarantees(conditional.Condition, false)))
            {
                return true;
            }
            if (current.Parent is IfStatementSyntax parentIf && current == parentIf.Statement &&
                guarantees(parentIf.Condition, true) ||
                current.Parent is ElseClauseSyntax { Parent: IfStatementSyntax elseIf } &&
                guarantees(elseIf.Condition, false))
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
                    if (postcondition(preceding) || preceding is IfStatementSyntax { Else: null } guard &&
                        model.AnalyzeControlFlow(guard.Statement) is { Succeeded: true, EndPointIsReachable: false } &&
                        guarantees(guard.Condition, false))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool InvocationGuaranteesMember(StatementSyntax statement, MemberAccessExpressionSyntax member,
        ISymbol receiver, SemanticModel model, CancellationToken token)
    {
        return TryGetInstanceInvocation(statement, receiver, model, token, out var method) &&
            method.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == PostconditionAnnotationType &&
                attribute.ConstructorArguments.Length == 1 && NamesMember(attribute.ConstructorArguments[0], member.Name.Identifier.ValueText));
    }

    private static bool IsThrowIfProblem(StatementSyntax statement, ISymbol receiver, SemanticModel model, CancellationToken token) =>
        TryGetInstanceInvocation(statement, receiver, model, token, out var method) &&
        method.Name == ThrowIfProblemName && method.Parameters.Length == 0;

    private static bool TryGetInstanceInvocation(StatementSyntax statement, ISymbol receiver, SemanticModel model,
        CancellationToken token, out IMethodSymbol method)
    {
        method = null!;
        if (statement is not ExpressionStatementSyntax
            { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax invocation } } ||
            !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(invocation.Expression, token).Symbol, receiver) ||
            model.GetSymbolInfo(invocation, token).Symbol is not IMethodSymbol resolved ||
            !ResultUsageAnalyzer.IsNativeResultType(resolved.ContainingType))
        {
            return false;
        }
        method = resolved;
        return true;
    }

    private static bool NamesMember(TypedConstant constant, string member) =>
        constant.Kind == TypedConstantKind.Array
            ? constant.Values.Any(value => value.Value is string item && item == member)
            : constant.Value is string name && name == member;

    private static bool GuaranteesStatus(ExpressionSyntax condition, bool outcome, bool success,
        ISymbol receiver, SemanticModel model, CancellationToken token)
    {
        if (Writes(condition, receiver, model, token))
        {
            return false;
        }
        if (condition is ParenthesizedExpressionSyntax parentheses)
        {
            return GuaranteesStatus(parentheses.Expression, outcome, success, receiver, model, token);
        }
        if (condition is PrefixUnaryExpressionSyntax negation && negation.IsKind(SyntaxKind.LogicalNotExpression))
        {
            return GuaranteesStatus(negation.Operand, !outcome, success, receiver, model, token);
        }
        if (condition is BinaryExpressionSyntax binary)
        {
            if (outcome && binary.IsKind(SyntaxKind.LogicalAndExpression) || !outcome && binary.IsKind(SyntaxKind.LogicalOrExpression))
            {
                return GuaranteesStatus(binary.Left, outcome, success, receiver, model, token) ||
                    GuaranteesStatus(binary.Right, outcome, success, receiver, model, token);
            }
            if (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
            {
                var compared = binary.Right.IsKind(SyntaxKind.TrueLiteralExpression) ? true :
                    binary.Right.IsKind(SyntaxKind.FalseLiteralExpression) ? false : (bool?)null;
                if (compared is { } expected)
                {
                    return GuaranteesStatus(binary.Left, outcome == (expected == binary.IsKind(SyntaxKind.EqualsExpression)),
                        success, receiver, model, token);
                }
            }
        }
        return condition is MemberAccessExpressionSyntax status && ResultUsageAnalyzer.IsResultProperty(status, model, token) &&
            SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(status.Expression, token).Symbol, receiver) &&
            (status.Name.Identifier.ValueText == SuccessName && outcome == success ||
             status.Name.Identifier.ValueText == FailureName && outcome != success);
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
            NamesMember(attribute.ConstructorArguments[1], member.Name.Identifier.ValueText));
    }
}

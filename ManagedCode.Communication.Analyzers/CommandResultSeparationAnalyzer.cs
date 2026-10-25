using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ManagedCode.Communication.Analyzers;

/// <summary>Prevents command and result envelopes from becoming each other's payloads.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandResultSeparationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Keep operation inputs and operation outcomes separate.</summary>
    public const string DiagnosticId = "MCC0003";
    private const string Title = "Keep command and result envelopes separate";
    private const string Message = "{0} payloads cannot contain {1} envelopes; pass commands as inputs and return business values";
    private const string Category = "Usage";
    private const string ResultName = "Result";
    private const string CommandName = "Command";
    private const string ValueName = "Value";
    private const string CollectionName = "Collection";
    private const string SucceedName = "Succeed";
    private const string CreateName = "Create";
    private const string FromName = "From";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, Message, Category,
        DiagnosticSeverity.Error, true, customTags: new[] { WellKnownDiagnosticTags.NotConfigurable });

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(start =>
        {
            var graph = CommandResultPayloadGraph.Create(start.Compilation);
            if (graph is null)
            {
                return;
            }
            start.RegisterSyntaxNodeAction(node =>
            {
                var type = node.SemanticModel.GetTypeInfo(node.Node, node.CancellationToken).Type;
                Report(node.ReportDiagnostic, graph, type, node.Node.GetLocation());
            }, SyntaxKind.GenericName);
            start.RegisterSymbolAction(symbol => AnalyzeSymbol(symbol, graph),
                SymbolKind.Method, SymbolKind.Property, SymbolKind.Field, SymbolKind.Event, SymbolKind.NamedType);
            start.RegisterOperationAction(operation => AnalyzeOperation(operation, graph),
                OperationKind.Invocation, OperationKind.ObjectCreation, OperationKind.Conversion,
                OperationKind.VariableDeclarator, OperationKind.SimpleAssignment, OperationKind.Argument);
        });
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, CommandResultPayloadGraph graph)
    {
        switch (context.Symbol)
        {
            case IMethodSymbol method:
                Report(context.ReportDiagnostic, graph, method.ReturnType, SourceLocation(method));
                foreach (var parameter in method.Parameters)
                {
                    Report(context.ReportDiagnostic, graph, parameter.Type, SourceLocation(parameter));
                }
                break;
            case IPropertySymbol property:
                Report(context.ReportDiagnostic, graph, property.Type, SourceLocation(property));
                break;
            case IFieldSymbol field:
                Report(context.ReportDiagnostic, graph, field.Type, SourceLocation(field));
                break;
            case IEventSymbol eventSymbol:
                Report(context.ReportDiagnostic, graph, eventSymbol.Type, SourceLocation(eventSymbol));
                break;
            case INamedTypeSymbol type:
                Report(context.ReportDiagnostic, graph, type, SourceLocation(type));
                break;
        }
    }

    private static Location? SourceLocation(ISymbol symbol)
    {
        foreach (var location in symbol.Locations)
        {
            if (location.IsInSource)
            {
                return location;
            }
        }
        return null;
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, CommandResultPayloadGraph graph)
    {
        var type = context.Operation is IVariableDeclaratorOperation variable ? variable.Symbol.Type : context.Operation.Type;
        Report(context.ReportDiagnostic, graph, type, context.Operation.Syntax.GetLocation());

        switch (context.Operation)
        {
            case IArgumentOperation argument when argument.Parameter is { } parameter &&
                argument.Parent is IInvocationOperation invocation &&
                invocation.TargetMethod.Name is SucceedName or CreateName or FromName &&
                graph.IsLibraryEnvelope(invocation.TargetMethod.ContainingType) &&
                HasPayloadParameter(parameter.OriginalDefinition.Type):
                ReportPayload(context, graph, invocation.Type, argument.Value);
                break;
            case ISimpleAssignmentOperation { Target: IPropertyReferenceOperation target } assignment when
                target.Property.Name is ValueName or CollectionName &&
                graph.IsLibraryEnvelope(target.Property.ContainingType) &&
                HasPayloadParameter(target.Property.OriginalDefinition.Type):
                ReportPayload(context, graph, target.Instance?.Type ?? target.Property.ContainingType, assignment.Value);
                break;
            case IConversionOperation { OperatorMethod: { } method } conversion when
                graph.IsLibraryEnvelope(method.ContainingType):
                ReportPayload(context, graph, conversion.Type, conversion.Operand);
                break;
        }
    }

    private static bool HasPayloadParameter(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol)
        {
            return true;
        }
        if (type is IArrayTypeSymbol array)
        {
            return HasPayloadParameter(array.ElementType);
        }
        if (type is INamedTypeSymbol named)
        {
            foreach (var argument in named.TypeArguments)
            {
                if (HasPayloadParameter(argument))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static void ReportPayload(OperationAnalysisContext context, CommandResultPayloadGraph graph,
        ITypeSymbol? envelope, IOperation value)
    {
        while (value is IConversionOperation conversion)
        {
            value = conversion.Operand;
        }
        if (graph.PayloadViolates(envelope, value.Type, out var commandPayload))
        {
            context.ReportDiagnostic(CreateDiagnostic(value.Syntax.GetLocation(), commandPayload));
            return;
        }
        switch (value)
        {
            case IDelegateCreationOperation { Target: IAnonymousFunctionOperation callback }:
                ReportCallbackReturns(context, graph, envelope, callback.Body);
                break;
            case IDelegateCreationOperation { Target: IMethodReferenceOperation method }:
                if (graph.PayloadViolates(envelope, method.Method.ReturnType, out commandPayload))
                {
                    context.ReportDiagnostic(CreateDiagnostic(value.Syntax.GetLocation(), commandPayload));
                }
                break;
            case IConditionalOperation conditional:
                ReportPayload(context, graph, envelope, conditional.WhenTrue);
                if (conditional.WhenFalse is { } whenFalse)
                {
                    ReportPayload(context, graph, envelope, whenFalse);
                }
                break;
            case ICoalesceOperation coalesce:
                ReportPayload(context, graph, envelope, coalesce.Value);
                ReportPayload(context, graph, envelope, coalesce.WhenNull);
                break;
        }
    }

    private static void ReportCallbackReturns(OperationAnalysisContext context, CommandResultPayloadGraph graph,
        ITypeSymbol? envelope, IOperation body)
    {
        if (body is IAnonymousFunctionOperation or ILocalFunctionOperation)
        {
            return;
        }
        if (body is IReturnOperation { ReturnedValue: { } returned })
        {
            ReportPayload(context, graph, envelope, returned);
            return;
        }
        foreach (var child in body.ChildOperations)
        {
            ReportCallbackReturns(context, graph, envelope, child);
        }
    }

    private static void Report(System.Action<Diagnostic> report, CommandResultPayloadGraph graph,
        ITypeSymbol? type, Location? location)
    {
        if (location is { IsInSource: true } && graph.Violates(type, out var commandPayload))
        {
            report(CreateDiagnostic(location, commandPayload));
        }
    }

    private static Diagnostic CreateDiagnostic(Location location, bool commandPayload) =>
        Diagnostic.Create(Rule, location, commandPayload ? CommandName : ResultName,
            commandPayload ? ResultName : CommandName);
}

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ManagedCode.Communication.Analyzers;

internal sealed class CommandResultPayloadGraph
{
    private const string AssemblyName = "ManagedCode.Communication";
    private const string CommandMetadata = "ManagedCode.Communication.ICommand";
    private const string GenericCommandMetadata = "ManagedCode.Communication.ICommand`1";
    private const string ResultMetadata = "ManagedCode.Communication.IResult";
    private const string GenericResultMetadata = "ManagedCode.Communication.IResult`1";
    private readonly ConcurrentDictionary<ITypeSymbol, byte> findings = new(SymbolEqualityComparer.Default);
    private readonly INamedTypeSymbol command;
    private readonly INamedTypeSymbol result;
    private readonly INamedTypeSymbol genericCommand;
    private readonly INamedTypeSymbol genericResult;

    private CommandResultPayloadGraph(INamedTypeSymbol command, INamedTypeSymbol result,
        INamedTypeSymbol genericCommand, INamedTypeSymbol genericResult)
    {
        this.command = command;
        this.result = result;
        this.genericCommand = genericCommand;
        this.genericResult = genericResult;
    }

    internal static CommandResultPayloadGraph? Create(Compilation compilation)
    {
        var command = compilation.GetTypeByMetadataName(CommandMetadata);
        var result = compilation.GetTypeByMetadataName(ResultMetadata);
        var genericCommand = compilation.GetTypeByMetadataName(GenericCommandMetadata);
        var genericResult = compilation.GetTypeByMetadataName(GenericResultMetadata);
        return command?.ContainingAssembly.Name == AssemblyName && result?.ContainingAssembly.Name == AssemblyName &&
            genericCommand?.ContainingAssembly.Name == AssemblyName && genericResult?.ContainingAssembly.Name == AssemblyName
            ? new CommandResultPayloadGraph(command, result, genericCommand, genericResult) : null;
    }

    internal bool IsLibraryEnvelope(INamedTypeSymbol type) => type.ContainingAssembly.Name == AssemblyName &&
        (Implements(type, command) || Implements(type, result));

    internal bool Violates(ITypeSymbol? type, out bool commandPayload)
    {
        commandPayload = false;
        if (type is null)
        {
            return false;
        }
        if (findings.TryGetValue(type, out var cached))
        {
            commandPayload = cached == 2;
            return cached != 0;
        }
        var violates = FindViolation(type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), out commandPayload);
        findings.TryAdd(type, violates ? (commandPayload ? (byte)2 : (byte)1) : (byte)0);
        return violates;
    }

    private bool FindViolation(ITypeSymbol? type, HashSet<ITypeSymbol> visited, out bool commandPayload)
    {
        commandPayload = false;
        if (type is null || !visited.Add(type))
        {
            return false;
        }
        if (type is IArrayTypeSymbol array)
        {
            return FindViolation(array.ElementType, visited, out commandPayload);
        }
        if (type is ITypeParameterSymbol parameter)
        {
            foreach (var constraint in parameter.ConstraintTypes)
            {
                if (FindViolation(constraint, visited, out commandPayload))
                {
                    return true;
                }
            }
            return false;
        }
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }
        foreach (var contract in Contracts(named))
        {
            var definition = contract.OriginalDefinition;
            if ((SymbolEqualityComparer.Default.Equals(definition, genericCommand) ||
                 SymbolEqualityComparer.Default.Equals(definition, genericResult)) &&
                PayloadViolates(named, contract.TypeArguments[0], out commandPayload))
            {
                return true;
            }
        }
        if (PayloadViolates(named, named, out commandPayload))
        {
            return true;
        }
        foreach (var argument in named.TypeArguments)
        {
            if (FindViolation(argument, visited, out commandPayload))
            {
                return true;
            }
        }
        return false;
    }

    internal bool PayloadViolates(ITypeSymbol? envelope, ITypeSymbol? payload, out bool commandPayload)
    {
        commandPayload = false;
        if (envelope is not INamedTypeSymbol named || payload is null)
        {
            return false;
        }
        if (Implements(named, result) && Contains(payload, command, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)))
        {
            return true;
        }
        commandPayload = true;
        return Implements(named, command) && Contains(payload, result, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
    }

    private static bool Contains(ITypeSymbol type, INamedTypeSymbol forbidden, HashSet<ITypeSymbol> visiting)
    {
        if (type is IArrayTypeSymbol array)
        {
            return Contains(array.ElementType, forbidden, visiting);
        }
        if (type is ITypeParameterSymbol parameter)
        {
            if (!visiting.Add(parameter))
            {
                return false;
            }
            try
            {
                foreach (var constraint in parameter.ConstraintTypes)
                {
                    if (Contains(constraint, forbidden, visiting))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                visiting.Remove(parameter);
            }
        }
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }
        if (Implements(named, forbidden))
        {
            return true;
        }
        foreach (var argument in named.TypeArguments)
        {
            if (Contains(argument, forbidden, visiting))
            {
                return true;
            }
        }
        if (named.SpecialType != SpecialType.None)
        {
            return false;
        }
        // Use the definition on the current member path so recursively expanding generic DTOs terminate.
        if (!visiting.Add(named.OriginalDefinition))
        {
            return false;
        }
        try
        {
            foreach (var member in named.GetMembers())
            {
                if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }
                var memberType = member switch
                {
                    IFieldSymbol field when !field.IsImplicitlyDeclared => field.Type,
                    IPropertySymbol { IsIndexer: false, GetMethod: { DeclaredAccessibility: Accessibility.Public } } property => property.Type,
                    _ => null
                };
                if (memberType is not null && Contains(memberType, forbidden, visiting))
                {
                    return true;
                }
            }
            if (named.BaseType is { } baseType && Contains(baseType, forbidden, visiting))
            {
                return true;
            }
            foreach (var inherited in named.Interfaces)
            {
                if (Contains(inherited, forbidden, visiting))
                {
                    return true;
                }
            }
            return false;
        }
        finally
        {
            visiting.Remove(named.OriginalDefinition);
        }
    }

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol contract)
    {
        if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, contract))
        {
            return true;
        }
        foreach (var implemented in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, contract))
            {
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<INamedTypeSymbol> Contracts(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var contract in type.AllInterfaces)
        {
            yield return contract;
        }
    }
}

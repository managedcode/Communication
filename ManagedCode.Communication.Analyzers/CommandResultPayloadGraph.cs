using System;
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
    private readonly ConcurrentDictionary<ITypeSymbol, Lazy<byte>> findings = new(SymbolEqualityComparer.Default);
    private readonly ConcurrentDictionary<ITypeSymbol, Lazy<bool>> commandPayloads = new(SymbolEqualityComparer.Default);
    private readonly ConcurrentDictionary<ITypeSymbol, Lazy<bool>> resultPayloads = new(SymbolEqualityComparer.Default);
    private readonly ConcurrentDictionary<ITypeSymbol, bool> commandMembers = new(SymbolEqualityComparer.Default);
    private readonly ConcurrentDictionary<ITypeSymbol, bool> resultMembers = new(SymbolEqualityComparer.Default);
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
        var finding = findings.GetOrAdd(type, root => new Lazy<byte>(() =>
        {
            var violates = FindViolation(root, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), out var isCommand);
            return violates ? (isCommand ? (byte)2 : (byte)1) : (byte)0;
        })).Value;
        commandPayload = finding == 2;
        return finding != 0;
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
        if (Implements(named, result) && ContainsPayload(payload, command))
        {
            return true;
        }
        commandPayload = true;
        return Implements(named, command) && ContainsPayload(payload, result);
    }

    private bool ContainsPayload(ITypeSymbol payload, INamedTypeSymbol forbidden)
    {
        var roots = SymbolEqualityComparer.Default.Equals(forbidden, command) ? commandPayloads : resultPayloads;
        return roots.GetOrAdd(payload, root => new Lazy<bool>(() =>
            Contains(root, forbidden, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), out _))).Value;
    }

    private bool Contains(ITypeSymbol type, INamedTypeSymbol forbidden, HashSet<ITypeSymbol> visiting,
        out bool cycle)
    {
        var completed = SymbolEqualityComparer.Default.Equals(forbidden, command) ? commandMembers : resultMembers;
        if (completed.TryGetValue(type, out var cached))
        {
            cycle = false;
            return cached;
        }
        var found = Inspect(type, forbidden, visiting, out cycle);
        // A path stopped at an ancestor cannot prove a negative for that child.
        // Acyclic subgraphs and positive paths are context-independent and reusable.
        if (found || !cycle)
        {
            completed.TryAdd(type, found);
        }
        return found;
    }

    private bool Inspect(ITypeSymbol type, INamedTypeSymbol forbidden, HashSet<ITypeSymbol> visiting,
        out bool cycle)
    {
        cycle = false;
        if (type is IArrayTypeSymbol array)
        {
            return Contains(array.ElementType, forbidden, visiting, out cycle);
        }
        if (type is ITypeParameterSymbol parameter)
        {
            if (!visiting.Add(parameter))
            {
                cycle = true;
                return false;
            }
            try
            {
                foreach (var constraint in parameter.ConstraintTypes)
                {
                    if (Contains(constraint, forbidden, visiting, out var childCycle))
                    {
                        return true;
                    }
                    cycle |= childCycle;
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
            if (Contains(argument, forbidden, visiting, out var childCycle))
            {
                return true;
            }
            cycle |= childCycle;
        }
        if (named.SpecialType != SpecialType.None)
        {
            return false;
        }
        if (!visiting.Add(named.OriginalDefinition))
        {
            cycle = true;
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
                if (memberType is not null)
                {
                    if (Contains(memberType, forbidden, visiting, out var childCycle))
                    {
                        return true;
                    }
                    cycle |= childCycle;
                }
            }
            if (named.BaseType is { } baseType)
            {
                if (Contains(baseType, forbidden, visiting, out var baseCycle))
                {
                    return true;
                }
                cycle |= baseCycle;
            }
            foreach (var inherited in named.Interfaces)
            {
                if (Contains(inherited, forbidden, visiting, out var childCycle))
                {
                    return true;
                }
                cycle |= childCycle;
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

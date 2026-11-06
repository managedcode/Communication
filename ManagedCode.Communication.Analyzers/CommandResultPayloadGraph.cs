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
        {
            var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var found = Contains(root, forbidden, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), seen, out var cycles);
            if (!found && (cycles & PayloadCycles.Expanding) == 0)
            {
                // The complete closed graph was checked from its root. A cycle cannot hide
                // an unexplored edge now, so every reached type has the same safe outcome.
                var completed = SymbolEqualityComparer.Default.Equals(forbidden, command) ? commandMembers : resultMembers;
                foreach (var type in seen)
                {
                    completed.TryAdd(type, false);
                }
            }
            return found;
        })).Value;
    }

    private bool Contains(ITypeSymbol type, INamedTypeSymbol forbidden, HashSet<ITypeSymbol> visiting,
        HashSet<ITypeSymbol> seen, out PayloadCycles cycles)
    {
        var completed = SymbolEqualityComparer.Default.Equals(forbidden, command) ? commandMembers : resultMembers;
        if (completed.TryGetValue(type, out var cached))
        {
            cycles = PayloadCycles.None;
            return cached;
        }
        if (!seen.Add(type))
        {
            cycles = PayloadCycles.Closed;
            return false;
        }
        var found = Inspect(type, forbidden, visiting, seen, out cycles);
        if (found || cycles == PayloadCycles.None)
        {
            completed.TryAdd(type, found);
        }
        // A different generic substitution may be reachable outside the current
        // definition's ancestry. Let that path inspect the type again in its own context.
        if ((cycles & PayloadCycles.Expanding) != 0)
        {
            seen.Remove(type);
        }
        return found;
    }

    private bool Inspect(ITypeSymbol type, INamedTypeSymbol forbidden, HashSet<ITypeSymbol> visiting,
        HashSet<ITypeSymbol> seen, out PayloadCycles cycles)
    {
        cycles = PayloadCycles.None;
        if (type is IArrayTypeSymbol array)
        {
            return Contains(array.ElementType, forbidden, visiting, seen, out cycles);
        }
        if (type is ITypeParameterSymbol parameter)
        {
            if (!visiting.Add(parameter))
            {
                cycles = PayloadCycles.Closed;
                return false;
            }
            try
            {
                foreach (var constraint in parameter.ConstraintTypes)
                {
                    if (Contains(constraint, forbidden, visiting, seen, out var childCycles))
                    {
                        return true;
                    }
                    cycles |= childCycles;
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
            if (Contains(argument, forbidden, visiting, seen, out var childCycles))
            {
                return true;
            }
            cycles |= childCycles;
        }
        if (named.SpecialType != SpecialType.None)
        {
            return false;
        }
        if (!visiting.Add(named.OriginalDefinition))
        {
            cycles |= PayloadCycles.Expanding;
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
                    if (Contains(memberType, forbidden, visiting, seen, out var childCycles))
                    {
                        return true;
                    }
                    cycles |= childCycles;
                }
            }
            if (named.BaseType is { } baseType)
            {
                if (Contains(baseType, forbidden, visiting, seen, out var baseCycles))
                {
                    return true;
                }
                cycles |= baseCycles;
            }
            foreach (var inherited in named.Interfaces)
            {
                if (Contains(inherited, forbidden, visiting, seen, out var childCycles))
                {
                    return true;
                }
                cycles |= childCycles;
            }
            return false;
        }
        finally
        {
            visiting.Remove(named.OriginalDefinition);
        }
    }

    [Flags]
    private enum PayloadCycles
    {
        None = 0,
        Closed = 1,
        Expanding = 2
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

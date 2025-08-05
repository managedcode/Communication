using System;
using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Communication.Tests.Common.TestApp.Models;

[GenerateSerializer]
public class ComplexTestModel
{
    [Id(0)]
    public int Id { get; set; }
    
    [Id(1)]
    public string Name { get; set; } = string.Empty;
    
    [Id(2)]
    public DateTime CreatedAt { get; set; }
    
    [Id(3)]
    public List<string> Tags { get; set; } = new();
    
    [Id(4)]
    public Dictionary<string, object> Properties { get; set; } = new();
    
    [Id(5)]
    public NestedModel? Nested { get; set; }
}

[GenerateSerializer]
public class NestedModel
{
    [Id(0)]
    public string Value { get; set; } = string.Empty;
    
    [Id(1)]
    public double Score { get; set; }
}
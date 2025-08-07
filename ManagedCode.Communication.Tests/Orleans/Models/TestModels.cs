using System;
using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Communication.Tests.Orleans.Models;

[GenerateSerializer]
public class PaymentRequest
{
    [Id(0)]
    public string OrderId { get; set; } = string.Empty;
    [Id(1)]
    public decimal Amount { get; set; }
    [Id(2)]
    public string Currency { get; set; } = string.Empty;
    [Id(3)]
    public List<OrderItem> Items { get; set; } = new();
    [Id(4)]
    public Dictionary<string, string> Metadata { get; set; } = new();
}

[GenerateSerializer]
public class OrderItem
{
    [Id(0)]
    public string ProductId { get; set; } = string.Empty;
    [Id(1)]
    public int Quantity { get; set; }
    [Id(2)]
    public decimal Price { get; set; }
}

[GenerateSerializer]
public class PaymentResponse
{
    [Id(0)]
    public string TransactionId { get; set; } = string.Empty;
    [Id(1)]
    public string Status { get; set; } = string.Empty;
    [Id(2)]
    public DateTimeOffset ProcessedAt { get; set; }
    [Id(3)]
    public Dictionary<string, object> Details { get; set; } = new();
}

[GenerateSerializer]
public class TestItem
{
    [Id(0)]
    public int Id { get; set; }
    [Id(1)]
    public string Name { get; set; } = string.Empty;
    [Id(2)]
    public string[] Tags { get; set; } = [];
}

public enum TestCommandType
{
    CreateUser,
    UpdateUser,
    DeleteUser,
    ProcessPayment,
    SendNotification
}

[GenerateSerializer]
public class UserProfile
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string Email { get; set; } = string.Empty;
    [Id(2)]
    public string Name { get; set; } = string.Empty;
    [Id(3)]
    public DateTimeOffset CreatedAt { get; set; }
    [Id(4)]
    public Dictionary<string, object> Attributes { get; set; } = new();
}
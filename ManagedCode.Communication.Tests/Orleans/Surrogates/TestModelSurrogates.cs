using System;
using System.Collections.Generic;
using ManagedCode.Communication.Tests.Orleans.Models;
using Orleans;

namespace ManagedCode.Communication.Tests.Orleans.Surrogates;

[GenerateSerializer]
public struct PaymentRequestSurrogate
{
    [Id(0)] public string OrderId;
    [Id(1)] public decimal Amount;
    [Id(2)] public string Currency;
    [Id(3)] public List<OrderItem> Items;
    [Id(4)] public Dictionary<string, string> Metadata;
}

[RegisterConverter]
public sealed class PaymentRequestSurrogateConverter : IConverter<PaymentRequest, PaymentRequestSurrogate>
{
    public PaymentRequest ConvertFromSurrogate(in PaymentRequestSurrogate surrogate)
    {
        return new PaymentRequest
        {
            OrderId = surrogate.OrderId,
            Amount = surrogate.Amount,
            Currency = surrogate.Currency,
            Items = surrogate.Items,
            Metadata = surrogate.Metadata
        };
    }

    public PaymentRequestSurrogate ConvertToSurrogate(in PaymentRequest value)
    {
        return new PaymentRequestSurrogate
        {
            OrderId = value.OrderId,
            Amount = value.Amount,
            Currency = value.Currency,
            Items = value.Items,
            Metadata = value.Metadata
        };
    }
}

[GenerateSerializer]
public struct OrderItemSurrogate
{
    [Id(0)] public string ProductId;
    [Id(1)] public int Quantity;
    [Id(2)] public decimal Price;
}

[RegisterConverter]
public sealed class OrderItemSurrogateConverter : IConverter<OrderItem, OrderItemSurrogate>
{
    public OrderItem ConvertFromSurrogate(in OrderItemSurrogate surrogate)
    {
        return new OrderItem
        {
            ProductId = surrogate.ProductId,
            Quantity = surrogate.Quantity,
            Price = surrogate.Price
        };
    }

    public OrderItemSurrogate ConvertToSurrogate(in OrderItem value)
    {
        return new OrderItemSurrogate
        {
            ProductId = value.ProductId,
            Quantity = value.Quantity,
            Price = value.Price
        };
    }
}

[GenerateSerializer]
public struct PaymentResponseSurrogate
{
    [Id(0)] public string TransactionId;
    [Id(1)] public string Status;
[Id(2)] public DateTime ProcessedAt;
    [Id(3)] public Dictionary<string, object> Details;
}

[RegisterConverter]
public sealed class PaymentResponseSurrogateConverter : IConverter<PaymentResponse, PaymentResponseSurrogate>
{
    public PaymentResponse ConvertFromSurrogate(in PaymentResponseSurrogate surrogate)
    {
        return new PaymentResponse
        {
            TransactionId = surrogate.TransactionId,
            Status = surrogate.Status,
            ProcessedAt = surrogate.ProcessedAt,
            Details = surrogate.Details
        };
    }

    public PaymentResponseSurrogate ConvertToSurrogate(in PaymentResponse value)
    {
        return new PaymentResponseSurrogate
        {
            TransactionId = value.TransactionId,
            Status = value.Status,
            ProcessedAt = value.ProcessedAt,
            Details = value.Details
        };
    }
}

[GenerateSerializer]
public struct TestItemSurrogate
{
    [Id(0)] public int Id;
    [Id(1)] public string Name;
    [Id(2)] public string[] Tags;
}

[RegisterConverter]
public sealed class TestItemSurrogateConverter : IConverter<TestItem, TestItemSurrogate>
{
    public TestItem ConvertFromSurrogate(in TestItemSurrogate surrogate)
    {
        return new TestItem
        {
            Id = surrogate.Id,
            Name = surrogate.Name,
            Tags = surrogate.Tags
        };
    }

    public TestItemSurrogate ConvertToSurrogate(in TestItem value)
    {
        return new TestItemSurrogate
        {
            Id = value.Id,
            Name = value.Name,
            Tags = value.Tags
        };
    }
}

[GenerateSerializer]
public struct UserProfileSurrogate
{
    [Id(0)] public Guid Id;
    [Id(1)] public string Email;
    [Id(2)] public string Name;
[Id(3)] public DateTime CreatedAt;
    [Id(4)] public Dictionary<string, object> Attributes;
}

[RegisterConverter]
public sealed class UserProfileSurrogateConverter : IConverter<UserProfile, UserProfileSurrogate>
{
    public UserProfile ConvertFromSurrogate(in UserProfileSurrogate surrogate)
    {
        return new UserProfile
        {
            Id = surrogate.Id,
            Email = surrogate.Email,
            Name = surrogate.Name,
            CreatedAt = surrogate.CreatedAt,
            Attributes = surrogate.Attributes
        };
    }

    public UserProfileSurrogate ConvertToSurrogate(in UserProfile value)
    {
        return new UserProfileSurrogate
        {
            Id = value.Id,
            Email = value.Email,
            Name = value.Name,
            CreatedAt = value.CreatedAt,
            Attributes = value.Attributes
        };
    }
}

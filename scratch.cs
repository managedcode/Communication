using System;

public interface IFactory<TSelf>
    where TSelf : struct, IFactory<TSelf>
{
    static abstract TSelf CreateCore(int value);

    static virtual TSelf Create(int value)
    {
        return TSelf.CreateCore(value);
    }
}

public struct Foo : IFactory<Foo>
{
    public int Value { get; }
    public Foo(int value) => Value = value;

    public static Foo CreateCore(int value) => new Foo(value);
}

public static class FactoryExtensions
{
    public static TSelf Make<TSelf>(int value)
        where TSelf : struct, IFactory<TSelf>
    {
        return IFactory<TSelf>.Create(value);
    }
}

public static class Program
{
    public static void Main()
    {
        var foo = FactoryExtensions.Make<Foo>(42);
        Console.WriteLine(foo.Value);
    }
}

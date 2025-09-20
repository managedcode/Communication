using System;

public interface IFoo<TSelf>
    where TSelf : struct, IFoo<TSelf>
{
    static abstract TSelf Base();

    static virtual TSelf Derived()
    {
        Console.WriteLine("In interface default");
        return TSelf.Base();
    }
}

public readonly struct Foo : IFoo<Foo>
{
    public static Foo Base()
    {
        Console.WriteLine("In Foo.Base");
        return new Foo();
    }
}

partial class Program
{
    static void Main()
    {
        var foo = Foo.Derived();
        Console.WriteLine(foo);
    }
}

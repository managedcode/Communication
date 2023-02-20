using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using ManagedCode.Communication;

public class Program
{
    public static void Main(string[] args)
    {
        
        var x1 = (Result)Activator.CreateInstance(typeof(Result));
        x1.Errors = new[] { Error.Create("message") };
        
        var x2 = (Result<int>)Activator.CreateInstance(typeof(Result<int>));
        x2.Errors = new[] { Error.Create("message") };
        
        
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args

#if  DEBUG
            ,new DebugInProcessConfig()  
#endif
        );
    }
}
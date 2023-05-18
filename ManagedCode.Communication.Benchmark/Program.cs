using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace ManagedCode.Communication.Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args

#if DEBUG
            , new DebugInProcessConfig()
#endif
        );
    }
}
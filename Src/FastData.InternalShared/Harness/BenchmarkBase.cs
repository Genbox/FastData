using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class BenchmarkBase<T>(T bootstrap) : BenchmarkBase(bootstrap) where T : BootstrapBase
{
    public T Bootstrap { get; } = bootstrap;
}

public abstract class BenchmarkBase(BootstrapBase bootstrap) : HarnessBase(bootstrap)
{
    public abstract BenchmarkSuite CreateFiles(IEnumerable<ITestData> data);
    public abstract void Run(BenchmarkSuite suite, bool useBencher, bool useShell);
}
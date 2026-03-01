using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class HarnessBase(BootstrapBase bootstrap)
{
    public string Name => bootstrap.Name;
    public string RootDir => bootstrap.RootDir;
    public Func<string, ICodeGenerator> CreateGenerator => bootstrap.GeneratorFactory;
    public override string ToString() => bootstrap.Name;
}
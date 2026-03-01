using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class BootstrapBase
{
    protected BootstrapBase(string name, HarnessType type)
    {
        Name = name;
        RootDir = Path.Combine(Path.GetTempPath(), "FastData", name, type.ToString());

        if (!Directory.Exists(RootDir))
            Directory.CreateDirectory(RootDir);
    }

    public string Name { get; }
    public string RootDir { get; }

    public abstract ICodeGenerator GeneratorFactory(string id);
    public override string ToString() => Name;
}
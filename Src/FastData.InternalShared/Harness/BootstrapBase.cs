using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class BootstrapBase
{
    protected BootstrapBase(string name, string ext, HarnessType type, string dockerImage, string commandTemplate)
    {
        Name = name;
        Ext = ext;
        Type = type;
        DockerImage = dockerImage;
        CommandTemplate = commandTemplate;
        RootDir = Path.Combine(Path.GetTempPath(), "FastData", name, type.ToString());

        if (!Directory.Exists(RootDir))
            Directory.CreateDirectory(RootDir);
    }

    public string Name { get; }
    public string Ext { get; }
    public HarnessType Type { get; }
    public string DockerImage { get; }
    public string CommandTemplate { get; }
    public string RootDir { get; }
    public abstract ICodeGenerator Generator { get; }
    public override string ToString() => Name;
}
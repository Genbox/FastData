using Genbox.FastData.Generator;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness.Enums;

namespace Genbox.FastData.InternalShared.Harness;

public abstract class BootstrapBase
{
    protected BootstrapBase(string name, string ext, HarnessType type, TypeMap map, string dockerImage, string commandTemplate, string? buildCommandTemplate = null, string? runCommandTemplate = null)
    {
        Name = name;
        Ext = ext;
        Type = type;
        Map = map;
        DockerImage = dockerImage;
        CommandTemplate = commandTemplate;
        BuildCommandTemplate = buildCommandTemplate;
        RunCommandTemplate = runCommandTemplate;
        RootDir = Path.Combine(Path.GetTempPath(), "FastData", name, type.ToString());

        if (!Directory.Exists(RootDir))
            Directory.CreateDirectory(RootDir);
    }

    public string Name { get; }
    public string Ext { get; }
    public HarnessType Type { get; }
    public TypeMap Map { get; }
    public string DockerImage { get; }
    public string CommandTemplate { get; }
    public string? BuildCommandTemplate { get; }
    public string? RunCommandTemplate { get; }
    public string RootDir { get; }
    public abstract ICodeGenerator Generator { get; }

    public abstract string Wrap(string code);

    public override string ToString() => Name;
}
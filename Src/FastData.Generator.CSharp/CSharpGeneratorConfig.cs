using Genbox.FastData.Generator.CSharp.Enums;

namespace Genbox.FastData.Generator.CSharp;

public sealed class CSharpGeneratorConfig
{
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; }
    public ClassType ClassType { get; set; }
    public CSharpOptions GeneratorOptions { get; set; }
    public BranchType UniqueLengthBranchType { get; set; }
}
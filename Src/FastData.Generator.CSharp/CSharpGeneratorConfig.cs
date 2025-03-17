using Genbox.FastData.Generator.CSharp.Enums;

namespace Genbox.FastData.Generator.CSharp;

public sealed class CSharpGeneratorConfig
{
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; } = ClassVisibility.Internal;
    public ClassType ClassType { get; set; } = ClassType.Static;
    public CSharpOptions GeneratorOptions { get; set; }
    public BranchType UniqueLengthBranchType { get; set; } = BranchType.If;
    public BranchType ConditionalBranchType { get; set; } = BranchType.Switch;
}
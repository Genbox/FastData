using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Enums;

namespace Genbox.FastData.Generator.CSharp;

public sealed class CSharpGeneratorConfig(string className)
{
    public string ClassName { get; set; } = className;
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; } = ClassVisibility.Internal;
    public ClassType ClassType { get; set; } = ClassType.Static;
    public CSharpOptions GeneratorOptions { get; set; }
    public BranchType KeyLengthUniqBranchType { get; set; } = BranchType.If;
    public BranchType ConditionalBranchType { get; set; } = BranchType.Switch;
}
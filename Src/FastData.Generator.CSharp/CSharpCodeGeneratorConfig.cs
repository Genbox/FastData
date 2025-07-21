using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.CSharp;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class CSharpCodeGeneratorConfig(string className)
{
    public string ClassName { get; set; } = className;
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; } = ClassVisibility.Internal;
    public ClassType ClassType { get; set; } = ClassType.Static;
    public CSharpOptions GeneratorOptions { get; set; }
    public BranchType ConditionalBranchType { get; set; } = BranchType.Switch;
}
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.CSharp;

/// <summary>Configures C# source generation.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class CSharpCodeGeneratorConfig(string className)
{
    /// <summary>Gets or sets the generated class or struct name.</summary>
    public string ClassName { get; set; } = className;

    /// <summary>Gets or sets the namespace for the generated type.</summary>
    public string? Namespace { get; set; }

    /// <summary>Gets or sets the generated type visibility.</summary>
    public ClassVisibility ClassVisibility { get; set; } = ClassVisibility.Internal;

    /// <summary>Gets or sets the generated type shape.</summary>
    public ClassType ClassType { get; set; } = ClassType.Static;

    /// <summary>Gets or sets C#-specific generator options.</summary>
    public CSharpOptions GeneratorOptions { get; set; }

    /// <summary>Gets or sets how conditional structures are emitted.</summary>
    public BranchType ConditionalBranchType { get; set; } = BranchType.Switch;
}
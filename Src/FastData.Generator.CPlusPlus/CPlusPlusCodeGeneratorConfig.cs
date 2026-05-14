using Genbox.FastData.Generator.CPlusPlus.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.CPlusPlus;

/// <summary>Configures C++ source generation.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class CPlusPlusCodeGeneratorConfig(string className)
{
    /// <summary>Gets or sets the generated class name.</summary>
    public string ClassName { get; set; } = className;

    /// <summary>Gets or sets C++-specific generator options.</summary>
    public CPlusPlusOptions GeneratorOptions { get; set; }
}
using Genbox.FastData.Generator.CPlusPlus.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.CPlusPlus;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class CPlusPlusCodeGeneratorConfig(string className)
{
    public string ClassName { get; set; } = className;
    public CPlusPlusOptions GeneratorOptions { get; set; }
}
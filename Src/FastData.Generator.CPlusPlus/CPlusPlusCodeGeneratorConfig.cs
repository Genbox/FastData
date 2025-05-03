using Genbox.FastData.Generator.CPlusPlus.Enums;

namespace Genbox.FastData.Generator.CPlusPlus;

public sealed class CPlusPlusCodeGeneratorConfig(string className)
{
    public string ClassName { get; set; } = className;
    public CPlusPlusOptions GeneratorOptions { get; set; }
}
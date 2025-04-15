namespace Genbox.FastData.Generator.CPlusPlus;

public sealed class CPlusPlusGeneratorConfig(string className)
{
    public string ClassName { get; set; } = className;
    public CPlusPlusOptions GeneratorOptions { get; set; }
}
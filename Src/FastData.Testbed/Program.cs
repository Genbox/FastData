using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.Testbed;

internal static class Program
{
    private static void Main()
    {
        CSharpCodeGenerator generator = new CSharpCodeGenerator(new CSharpCodeGeneratorConfig("test"));

        FastDataConfig cfg = new FastDataConfig(StructureType.HashSetChain);
        cfg.AnalyzerConfig = new GeneticAnalyzerConfig();

        FastDataGenerator.TryGenerate(["item1", "item2"], cfg, generator, out string? source);

        Console.WriteLine(source);
    }
}
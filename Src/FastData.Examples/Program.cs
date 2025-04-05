using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Generator.CSharp.Enums;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        FastDataConfig config = new FastDataConfig("MyData", ["item1", "item2"]);

        if (!FastDataGenerator.TryGenerate(config, new CSharpCodeGenerator(new CSharpGeneratorConfig()), out string? source))
            Console.WriteLine("Failed to generate source code");

        Console.WriteLine(source);
    }
}
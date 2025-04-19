using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        FastDataConfig config = new FastDataConfig();
        config.StringComparison = StringComparison.OrdinalIgnoreCase;

        CSharpCodeGenerator generator = new CSharpCodeGenerator(new CSharpGeneratorConfig("Dogs"));

        if (!FastDataGenerator.TryGenerate(["Labrador", "German Shepherd", "Golden Retriever"], config, generator, out string? source))
            Console.WriteLine("Failed to generate source code");

        Console.WriteLine(source);
    }
}
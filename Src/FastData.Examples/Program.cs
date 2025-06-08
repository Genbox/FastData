using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        FastDataConfig config = new FastDataConfig();
        CSharpCodeGenerator generator = CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig("Dogs"));

        if (!FastDataGenerator.TryGenerate(["Labrador", "German Shepherd", "Golden Retriever"], config, generator, out string? source))
            Console.WriteLine("Failed to generate source code");

        Console.WriteLine(source);
    }
}
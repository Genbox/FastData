using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        if (!FastDataGenerator.TryGenerate(["item1", "item2"], new FastDataConfig(), new CSharpCodeGenerator(new CSharpGeneratorConfig("MyData")), out string? source))
            Console.WriteLine("Failed to generate source code");

        Console.WriteLine(source);
    }
}
using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        FastDataConfig config = new FastDataConfig();
        CSharpCodeGenerator generator = CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig("Dogs"));

        string source = FastDataGenerator.Generate(["Labrador", "German Shepherd", "Golden Retriever"], config, generator);
        Console.WriteLine(source);
    }
}
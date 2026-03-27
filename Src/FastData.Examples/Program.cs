using Genbox.FastData.Config;
using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        StringDataConfig config = new StringDataConfig();
        CSharpCodeGenerator generator = new CSharpCodeGenerator(new CSharpCodeGeneratorConfig("Dogs"));

        string source = FastDataGenerator.Generate(["Labrador", "German Shepherd", "Golden Retriever"], config, generator);
        Console.WriteLine(source);
    }
}
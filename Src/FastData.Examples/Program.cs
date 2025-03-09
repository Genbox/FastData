using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Generator.CSharp.Enums;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        FastDataConfig config = new FastDataConfig("MyData", ["item1", "item2"]);
        string code = FastDataGenerator.Generate(config, new CSharpCodeGenerator(new CSharpGeneratorConfig
        {
            ClassType = ClassType.Static
        }));
        Console.WriteLine(code);
    }
}
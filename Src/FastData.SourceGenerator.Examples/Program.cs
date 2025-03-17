using Genbox.FastData.SourceGenerator;

[assembly: FastData<string>("StaticData", ["item1", "item2", "item3"])]

namespace Genbox.FastData.SourceGenerator.Examples;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine(StaticData.Contains("item1"));
        Console.WriteLine(StaticData.Contains("notthere"));
    }
}
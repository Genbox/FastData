using Genbox.FastData.SourceGenerator;

[assembly: FastData<string>("Dogs", ["Labrador", "German Shepherd", "Golden Retriever"])]

namespace Genbox.FastData.SourceGenerator.Examples;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine(Dogs.Contains("item1"));
        Console.WriteLine(Dogs.Contains("notthere"));
    }
}
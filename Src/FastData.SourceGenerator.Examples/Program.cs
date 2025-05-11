using Genbox.FastData.SourceGenerator;

[assembly: FastData<string>("Dogs", ["Labrador", "German Shepherd", "Golden Retriever"])]

namespace Genbox.FastData.SourceGenerator.Examples;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("Labrador is a dog? -> " + Dogs.Contains("Labrador"));
        Console.WriteLine("Lamp is a dog? -> " + Dogs.Contains("Lamp"));
    }
}
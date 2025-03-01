using System.Text;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        FastDataConfig config = new FastDataConfig("MyData", ["item1", "item2"]);
        config.StorageMode = StorageMode.Auto;

        StringBuilder sb = new StringBuilder();
        FastDataGenerator.Generate(sb, config);

        Console.WriteLine(sb.ToString());
    }
}
using Genbox.FastData;

[assembly: FastData<string>("ImmutableSet", ["a", "aa", "aaa", "aaaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa"])]

namespace Genbox.FastData.Examples;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("Contains item1: " + ImmutableSet.Contains("a"));
        Console.WriteLine("Contains item2: " + ImmutableSet.Contains("item2"));
        Console.WriteLine("Contains item3: " + ImmutableSet.Contains("aa"));
    }
}
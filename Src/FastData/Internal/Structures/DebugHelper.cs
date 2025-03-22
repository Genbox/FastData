using System.Diagnostics;

namespace Genbox.FastData.Internal.Structures;

internal static class DebugHelper
{
    [Conditional("DebugPrint")]
    internal static void Print(string value) => Console.WriteLine(value);
}
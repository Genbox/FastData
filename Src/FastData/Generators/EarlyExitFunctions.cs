namespace Genbox.FastData.Generators;

public static class EarlyExitFunctions
{
    public static char GetFirstChar(string str) => str[0];
    public static char GetFirstCharLower(string str) => char.ToLowerInvariant(str[0]);
    public static char GetLastChar(string str) => str[str.Length - 1];
    public static char GetLastCharLower(string str) => char.ToLowerInvariant(str[str.Length - 1]);
    public static uint GetLength(string str) => (uint)str.Length;
    public static bool StartsWith(string prefix, string str) => str.StartsWith(prefix, StringComparison.Ordinal);
    public static bool StartsWithIgnoreCase(string prefix, string str) => str.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    public static bool EndsWith(string prefix, string str) => str.EndsWith(prefix, StringComparison.Ordinal);
    public static bool EndsWithIgnoreCase(string prefix, string str) => str.EndsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
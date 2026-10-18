using System.Globalization;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>Creates stable names for generated implementation details.</summary>
internal static class PreparedNames
{
    public static string Name(string prefix, int index) => prefix + index.ToString(CultureInfo.InvariantCulture);

    public static IReadOnlyList<string> Create(string prefix, int count)
    {
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
            names[i] = Name(prefix, i);
        return names;
    }
}
#if NETSTANDARD2_0
using System.Text;

namespace Genbox.FastData.Generator.Compat;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendJoin<T>(this StringBuilder sb, string? separator, IEnumerable<T> values)
    {
        return sb.Append(string.Join(separator, values));
    }
}
#endif
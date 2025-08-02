using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Genbox.FastData.Generator.Compat;

[SuppressMessage("Roslynator", "RCS1197:Optimize StringBuilder.Append/AppendLine call")]
public static class StringBuilderExtensions
{
    public static StringBuilder AppendJoin<T>(this StringBuilder sb, string? separator, IEnumerable<T> values)
    {
        return sb.Append(string.Join(separator, values));
    }
}
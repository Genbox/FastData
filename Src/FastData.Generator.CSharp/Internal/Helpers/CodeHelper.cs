using System.Globalization;
using System.Text;

namespace Genbox.FastData.Generator.CSharp.Internal.Helpers;

internal static class CodeHelper
{
    internal static string ToValueLabel(object value)
    {
        return value switch
        {
            string val => $"\"{val}\"",
            char val => $"'{val}'",
            bool val => val.ToString().ToLowerInvariant(),
            IFormattable val => val.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    internal static string JoinValues<T>(Span<T> data, Action<StringBuilder, T> render, string delim = ", ")
    {
        Span<T>.Enumerator enumerator = data.GetEnumerator();
        StringBuilder sb = new StringBuilder();

        while (enumerator.MoveNext())
        {
            render(sb, enumerator.Current);
            sb.Append(delim);
        }

        sb.Remove(sb.Length - delim.Length, delim.Length);
        return sb.ToString();
    }

    internal static string JoinValues<T>(IEnumerable<T> data, Action<StringBuilder, T> render, string delim = ", ")
    {
        using IEnumerator<T> enumerator = data.GetEnumerator();
        StringBuilder sb = new StringBuilder();

        while (enumerator.MoveNext())
        {
            render(sb, enumerator.Current);
            sb.Append(delim);
        }

        sb.Remove(sb.Length - delim.Length, delim.Length);
        return sb.ToString();
    }
}
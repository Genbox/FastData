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

    internal static string JoinValues<T>(T[] data, Action<StringBuilder, T> render, string delim = ", ")
    {
        return JoinValues(data.AsSpan(), render, delim);
    }

    internal static string JoinValues<T>(Span<T> data, Action<StringBuilder, T> render, string delim = ", ")
    {
        StringBuilder sb = new StringBuilder();

        int i = 0;
        foreach (T obj in data)
        {
            render(sb, obj);

            if (i != data.Length - 1)
                sb.Append(delim);

            i++;
        }

        return sb.ToString();
    }
}
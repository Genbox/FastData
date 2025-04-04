using System.Globalization;
using System.Text;

namespace Genbox.FastData.Generator.CSharp.Internal.Helpers;

internal static class CodeHelper
{
    internal static string GetSmallestUnsignedType(long value) => GetSmallestUnsignedType((ulong)value);

    internal static string GetSmallestUnsignedType(ulong value) => value switch
    {
        <= byte.MaxValue => "byte",
        <= ushort.MaxValue => "ushort",
        <= uint.MaxValue => "uint",
        _ => "ulong"
    };

    internal static string GetSmallestSignedType(long value) => value switch
    {
        <= sbyte.MaxValue => "sbyte",
        <= short.MaxValue => "short",
        <= int.MaxValue => "int",
        _ => "long"
    };

    internal static string ToValueLabel(object? value)
    {
        return value switch
        {
            null => "null",
            string val => $"\"{val}\"",
            char val => $"'{val}'",
            bool val => val.ToString().ToLowerInvariant(),
            IFormattable val => val.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    internal static string FormatColumns<T>(IEnumerable<T> items, Action<StringBuilder, T> Render, int indent = 8, int columns = 10)
    {
        StringBuilder sb = new StringBuilder();
        int count = 0;

        string indentStr = new string(' ', indent);

        foreach (T item in items)
        {
            if (count == 0)
                sb.Append(indentStr);

            if (count > 0)
            {
                sb.Append(", ");

                if (count % columns == 0)
                {
                    sb.AppendLine();
                    sb.Append(indentStr);
                }
            }

            Render(sb, item);
            count++;
        }

        return sb.ToString();
    }

    internal static string FormatList<T>(Span<T> data, Action<StringBuilder, T> render, string delim = ", ")
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

    internal static string FormatList<T>(IEnumerable<T> data, Action<StringBuilder, T> render, string delim = ", ")
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
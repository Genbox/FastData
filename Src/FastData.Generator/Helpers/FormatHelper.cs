using System.Text;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.Helpers;

public static class FormatHelper
{
    public static string FormatColumns<T>(T[] items, Func<T, string> Render, int indent = 8, int columns = 10)
    {
        return FormatColumns(items.AsReadOnlySpan(), Render, indent, columns);
    }

    public static string FormatColumns<T>(ReadOnlySpan<T> items, Func<T, string> Render, int indent = 8, int columns = 10)
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

            sb.Append(Render(item));
            count++;
        }

        return sb.ToString();
    }

    public static string FormatList<T>(T[] data, Func<T, string> render, string delim = ", ")
    {
        return FormatList(data.AsReadOnlySpan(), render, delim);
    }

    public static string FormatList<T>(ReadOnlySpan<T> data, Func<T, string> render, string delim = ", ")
    {
        StringBuilder sb = new StringBuilder();

        foreach (T item in data)
        {
            sb.Append(render(item));
            sb.Append(delim);
        }

        sb.Remove(sb.Length - delim.Length, delim.Length);
        return sb.ToString();
    }
}
using System.Text;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.Helpers;

public static class FormatHelper
{
    public static string FormatColumns<T>(T[] items, Func<T, string> Render, int indent = 8, int columns = 10)
    {
        return FormatColumns(items.AsReadOnlySpan(), (_, y) => Render(y), indent, columns);
    }

    public static string FormatColumns<T>(T[] items, Func<int, T, string> Render, int indent = 8, int columns = 10)
    {
        return FormatColumns(items.AsReadOnlySpan(), Render, indent, columns);
    }

    public static string FormatColumns<T>(ReadOnlySpan<T> items, Func<T, string> Render, int indent = 8, int columns = 10)
    {
        return FormatColumns(items, (_, y) => Render(y), indent, columns);
    }

    public static string FormatColumns<T>(ReadOnlySpan<T> items, Func<int, T, string> Render, int indent = 8, int columns = 10)
    {
        if (items.Length == 0)
            return string.Empty;

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

            sb.Append(Render(count++, item));
        }

        return sb.ToString();
    }

    public static string FormatColumns(IEnumerable<object> items, int itemCount)
    {
        return FormatColumns(items, itemCount, (_, item) => item.ToString() ?? "null");
    }

    public static string FormatColumns(IEnumerable<object> items, int itemCount, Func<object?, string> Render, int indent = 8, int columns = 10)
    {
        return FormatColumns(items, itemCount, (_, item) => Render(item), indent, columns);
    }

    public static string FormatColumns(IEnumerable<object> items, int itemCount, Func<int, object, string> Render, int indent = 8, int columns = 10)
    {
        if (itemCount == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder();
        int count = 0;

        string indentStr = new string(' ', indent);

        foreach (object item in items)
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

            sb.Append(Render(count++, item));
        }

        return sb.ToString();
    }

    public static string FormatList<T>(T[] items, Func<T, string> render, string delim = ", ")
    {
        return FormatList(items.AsReadOnlySpan(), render, delim);
    }

    public static string FormatList<T>(ReadOnlySpan<T> items, Func<T, string> render, string delim = ", ")
    {
        if (items.Length == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (T item in items)
        {
            sb.Append(render(item));
            sb.Append(delim);
        }

        sb.Remove(sb.Length - delim.Length, delim.Length);
        return sb.ToString();
    }
}
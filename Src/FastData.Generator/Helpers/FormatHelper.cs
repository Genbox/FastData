using System.Text;
using Genbox.FastData.Generator.Extensions;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Helpers;

/// <summary>Formats generated values into compact lists and aligned columns.</summary>
[PublicAPI]
public static class FormatHelper
{
    /// <summary>Formats an array into columns.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items to format.</param>
    /// <param name="Render">The function used to render each item.</param>
    /// <param name="indent">The number of spaces at the start of each line.</param>
    /// <param name="columns">The maximum number of items per line.</param>
    /// <returns>The formatted columns.</returns>
    public static string FormatColumns<T>(T[] items, Func<T, string> Render, int indent = 4, int columns = 10)
    {
        return FormatColumns(items.AsReadOnlySpan(), (_, y) => Render(y), indent, columns);
    }

    /// <summary>Formats an array into columns with an index-aware renderer.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items to format.</param>
    /// <param name="Render">The function used to render each indexed item.</param>
    /// <param name="indent">The number of spaces at the start of each line.</param>
    /// <param name="columns">The maximum number of items per line.</param>
    /// <returns>The formatted columns.</returns>
    public static string FormatColumns<T>(T[] items, Func<int, T, string> Render, int indent = 4, int columns = 10) => FormatColumns(items.AsReadOnlySpan(), Render, indent, columns);

    /// <summary>Formats a read-only span into columns.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items to format.</param>
    /// <param name="Render">The function used to render each item.</param>
    /// <param name="indent">The number of spaces at the start of each line.</param>
    /// <param name="columns">The maximum number of items per line.</param>
    /// <returns>The formatted columns.</returns>
    public static string FormatColumns<T>(ReadOnlySpan<T> items, Func<T, string> Render, int indent = 4, int columns = 10)
    {
        return FormatColumns(items, (_, y) => Render(y), indent, columns);
    }

    /// <summary>Formats a read-only span into columns with an index-aware renderer.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items to format.</param>
    /// <param name="Render">The function used to render each indexed item.</param>
    /// <param name="indent">The number of spaces at the start of each line.</param>
    /// <param name="columns">The maximum number of items per line.</param>
    /// <returns>The formatted columns.</returns>
    public static string FormatColumns<T>(ReadOnlySpan<T> items, Func<int, T, string> Render, int indent = 4, int columns = 10)
    {
        if (Render == null)
            throw new ArgumentNullException(nameof(Render), "The render callback cannot be null.");

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

    /// <summary>Formats a known number of enumerable items into columns.</summary>
    /// <param name="items">The items to format.</param>
    /// <param name="itemCount">The number of items in <paramref name="items"/>.</param>
    /// <param name="Render">The function used to render each item.</param>
    /// <param name="indent">The number of spaces at the start of each line.</param>
    /// <param name="columns">The maximum number of items per line.</param>
    /// <returns>The formatted columns.</returns>
    public static string FormatColumns(IEnumerable<object> items, int itemCount, Func<object?, string> Render, int indent = 4, int columns = 10)
    {
        return FormatColumns(items, itemCount, (_, item) => Render(item), indent, columns);
    }

    /// <summary>Formats a known number of enumerable items into columns with an index-aware renderer.</summary>
    /// <param name="items">The items to format.</param>
    /// <param name="itemCount">The number of items in <paramref name="items"/>.</param>
    /// <param name="Render">The function used to render each indexed item.</param>
    /// <param name="indent">The number of spaces at the start of each line.</param>
    /// <param name="columns">The maximum number of items per line.</param>
    /// <returns>The formatted columns.</returns>
    public static string FormatColumns(IEnumerable<object> items, int itemCount, Func<int, object, string> Render, int indent = 4, int columns = 10)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items), "The item sequence cannot be null.");

        if (Render == null)
            throw new ArgumentNullException(nameof(Render), "The render callback cannot be null.");

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

    /// <summary>Formats an array as a delimited list.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items to format.</param>
    /// <param name="render">The function used to render each item.</param>
    /// <param name="delim">The delimiter placed between items.</param>
    /// <returns>The formatted list.</returns>
    public static string FormatList<T>(T[] items, Func<T, string> render, string delim = ", ") => FormatList(items.AsReadOnlySpan(), render, delim);

    /// <summary>Formats a read-only span as a delimited list.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items to format.</param>
    /// <param name="render">The function used to render each item.</param>
    /// <param name="delim">The delimiter placed between items.</param>
    /// <returns>The formatted list.</returns>
    public static string FormatList<T>(ReadOnlySpan<T> items, Func<T, string> render, string delim = ", ")
    {
        if (render == null)
            throw new ArgumentNullException(nameof(render), "The render callback cannot be null.");

        if (delim == null)
            throw new ArgumentNullException(nameof(delim), "The delimiter cannot be null.");

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
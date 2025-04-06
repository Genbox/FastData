using System.Text;

namespace Genbox.FastData.Generator.Helpers;

public static class FormatHelper
{
    public static string FormatColumns<T>(IEnumerable<T> items, Action<StringBuilder, T> Render, int indent = 8, int columns = 10)
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

    public static string FormatList<T>(IEnumerable<T> data, Action<StringBuilder, T> render, string delim = ", ")
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
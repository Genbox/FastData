using System.Text;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Writes indented source, delimited blocks, and wrapped initializer elements.</summary>
public sealed class SourceWriter(int indentSize = 4, int initialIndent = 0)
{
    private readonly StringBuilder _output = new StringBuilder();
    private int _indent = initialIndent;

    /// <summary>Joins source fragments with LF line endings and the requested indentation.</summary>
    public static string JoinLines(IEnumerable<string> fragments, int indent = 0)
    {
        SourceWriter writer = new SourceWriter(initialIndent: indent);
        foreach (string fragment in fragments)
            writer.Line(fragment);
        return writer.ToString();
    }

    /// <summary>Returns a source fragment indented by the requested number of levels.</summary>
    public static string Indent(string source, int indent = 1) => JoinLines([source], indent);

    public void Indent() => _indent++;
    public void Unindent() => _indent--;

    /// <summary>Writes source text, indenting each nonempty line at the current level.</summary>
    public void Line(string text = "")
    {
        foreach (ReadOnlySpan<char> line in text.AsSpan().EnumerateLines())
        {
            if (!line.IsEmpty) // Don't indent empty lines
                _output.Append(' ', _indent * indentSize).Append(line);

            _output.Append('\n');
        }
    }

    public void Open()
    {
        Line("{");
        Indent();
    }

    public void Close(string suffix = "")
    {
        Unindent();
        Line("}" + suffix);
    }

    /// <summary>Formats initializer elements into rows at the requested indentation.</summary>
    public static string FormatElements(IEnumerable<string> elements, int count, int perLine, int indent = 1)
    {
        SourceWriter writer = new SourceWriter(initialIndent: indent);
        writer.Elements(elements, count, perLine);
        return writer.ToString();
    }

    public void Elements(IEnumerable<string> elements, int count, int perLine)
    {
        int position = 0;

        foreach (string element in elements)
        {
            if (position % perLine == 0)
                _output.Append(' ', _indent * indentSize);
            else
                _output.Append(' ');

            _output.Append(element);
            position++;

            if (position < count)
                _output.Append(',');

            if (position % perLine == 0 || position == count)
                _output.Append('\n');
        }
    }

    /// <summary>Returns source without trailing line breaks, ready for composition or saving.</summary>
    public override string ToString()
    {
        int length = _output.Length;
        while (length > 0 && _output[length - 1] == '\n')
            length--;
        return _output.ToString(0, length);
    }
}
using System.Text;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Writes indented source, delimited blocks, and wrapped initializer elements.</summary>
public sealed class SourceWriter(int indentSize = 4)
{
    private readonly StringBuilder _output = new StringBuilder();
    private int _indent;

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

    public void Open(string delimiter = "{")
    {
        Line(delimiter);
        Indent();
    }

    public void Close(string suffix = "", string delimiter = "}")
    {
        Unindent();
        Line(delimiter + suffix);
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

    public override string ToString() => _output.ToString();
}
using System.Text;

namespace Genbox.FastData.Generator;

/// <summary>A StringBuilder that supports indentation for easy code printing</summary>
public sealed class IndentedStringBuilder
{
    private readonly StringBuilder _sb = new StringBuilder();
    private bool _indentPending = true;

    public int Indent { get; set; }

    public IndentedStringBuilder Append(object value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(string value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(FormattableString value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(char value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    public IndentedStringBuilder AppendLine()
    {
        AppendLine(string.Empty);
        return this;
    }

    public IndentedStringBuilder AppendLine(string value)
    {
        if (value.Length != 0)
            DoIndent();

        _sb.AppendLine(value);
        _indentPending = true;

        return this;
    }

    public IndentedStringBuilder AppendLine(FormattableString value)
    {
        DoIndent();
        _sb.Append(value);
        _indentPending = true;
        return this;
    }

    public IndentedStringBuilder Clear()
    {
        _sb.Clear();
        Indent = 0;

        return this;
    }

    public IndentedStringBuilder IncrementIndent()
    {
        Indent++;
        return this;
    }

    public IndentedStringBuilder DecrementIndent()
    {
        if (Indent > 0)
            Indent--;

        return this;
    }

    public override string ToString() => _sb.ToString();

    private void DoIndent()
    {
        if (_indentPending && Indent > 0)
            _sb.Append(' ', Indent);

        _indentPending = false;
    }
}
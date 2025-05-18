using System.Text;

namespace Genbox.FastData.Generator;

public sealed class FastStringBuilder
{
    private const byte IndentSize = 4;
    private int _indent;
    private bool _indentPending = true;
    private readonly StringBuilder _sb = new StringBuilder();

    public FastStringBuilder Append(object value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    public FastStringBuilder Append(string value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    public FastStringBuilder Append(FormattableString value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    public FastStringBuilder Append(char value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    public FastStringBuilder AppendLine()
    {
        AppendLine(string.Empty);
        return this;
    }

    public FastStringBuilder AppendLine(string value)
    {
        if (value.Length != 0)
            DoIndent();

        _sb.AppendLine(value);
        _indentPending = true;

        return this;
    }

    public FastStringBuilder AppendLine(FormattableString value)
    {
        DoIndent();
        _sb.Append(value);
        _indentPending = true;
        return this;
    }

    public FastStringBuilder Clear()
    {
        _sb.Clear();
        _indent = 0;

        return this;
    }

    public FastStringBuilder IncrementIndent()
    {
        _indent++;
        return this;
    }

    public FastStringBuilder DecrementIndent()
    {
        if (_indent > 0)
            _indent--;

        return this;
    }

    public IDisposable Indent() => new Indenter(this);
    public IDisposable SuspendIndent() => new IndentSuspender(this);

    public override string ToString() => _sb.ToString();

    private void DoIndent()
    {
        if (_indentPending && _indent > 0)
            _sb.Append(' ', _indent * IndentSize);

        _indentPending = false;
    }

    private sealed class Indenter : IDisposable
    {
        private readonly FastStringBuilder _stringBuilder;

        public Indenter(FastStringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
            _stringBuilder.IncrementIndent();
        }

        public void Dispose() => _stringBuilder.DecrementIndent();
    }

    private sealed class IndentSuspender : IDisposable
    {
        private readonly FastStringBuilder _stringBuilder;
        private readonly int _indent;

        public IndentSuspender(FastStringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
            _indent = _stringBuilder._indent;
            _stringBuilder._indent = 0;
        }

        public void Dispose() => _stringBuilder._indent = _indent;
    }
}
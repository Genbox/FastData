using System.Text;

namespace Genbox.FastData.Generator;

/// <summary>Builds generated text while applying indentation at the start of each line.</summary>
public sealed class IndentedStringBuilder
{
    private readonly StringBuilder _sb = new StringBuilder();
    private bool _indentPending = true;

    /// <summary>Gets or sets the number of spaces written before the next non-empty line.</summary>
    public int Indent { get; set; }

    /// <summary>Appends an object's string representation.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>This builder.</returns>
    public IndentedStringBuilder Append(object value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    /// <summary>Appends a string.</summary>
    /// <param name="value">The string to append.</param>
    /// <returns>This builder.</returns>
    public IndentedStringBuilder Append(string value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    /// <summary>Appends an interpolated string using its default formatting behavior.</summary>
    /// <param name="value">The interpolated string to append.</param>
    /// <returns>This builder.</returns>
    public IndentedStringBuilder Append(FormattableString value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    /// <summary>Appends a character.</summary>
    /// <param name="value">The character to append.</param>
    /// <returns>This builder.</returns>
    public IndentedStringBuilder Append(char value)
    {
        DoIndent();
        _sb.Append(value);
        return this;
    }

    /// <summary>Appends an empty line.</summary>
    /// <returns>This builder.</returns>
    public IndentedStringBuilder AppendLine()
    {
        AppendLine(string.Empty);
        return this;
    }

    /// <summary>Appends a string followed by a line terminator.</summary>
    /// <param name="value">The string to append.</param>
    /// <returns>This builder.</returns>
    public IndentedStringBuilder AppendLine(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "The appended string cannot be null.");

        if (value.Length != 0)
            DoIndent();

        _sb.AppendLine(value);
        _indentPending = true;

        return this;
    }

    /// <summary>Appends an interpolated string and marks the next append as the start of a line.</summary>
    /// <param name="value">The interpolated string to append.</param>
    /// <returns>This builder.</returns>
    public IndentedStringBuilder AppendLine(FormattableString value)
    {
        DoIndent();
        _sb.Append(value);
        _indentPending = true;
        return this;
    }

    /// <summary>Removes all text and resets indentation to zero.</summary>
    /// <returns>This builder.</returns>
    public IndentedStringBuilder Clear()
    {
        _sb.Clear();
        Indent = 0;

        return this;
    }

    /// <summary>Increases indentation by one space.</summary>
    /// <returns>This builder.</returns>
    public IndentedStringBuilder IncrementIndent()
    {
        Indent++;
        return this;
    }

    /// <summary>Decreases indentation by one space without allowing it to become negative.</summary>
    /// <returns>This builder.</returns>
    public IndentedStringBuilder DecrementIndent()
    {
        if (Indent > 0)
            Indent--;

        return this;
    }

    /// <inheritdoc />
    public override string ToString() => _sb.ToString();

    private void DoIndent()
    {
        if (_indentPending && Indent > 0)
            _sb.Append(' ', Indent);

        _indentPending = false;
    }
}
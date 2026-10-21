using System.Buffers;
using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Dispatches scalar literals and traverses packed arrays directly through their typed storage.</summary>
public abstract class LiteralFormatter(Func<IrLiteral, string?>? reference = null)
{
    // Reject non-ASCII text instead of silently replacing characters with '?' and changing keys or values.
    private static readonly Encoding _ascii = Encoding.GetEncoding("us-ascii", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false, true);

    /// <summary>Formats a type using the language's default declaration context.</summary>
    public abstract string Type(IrType type);

    /// <summary>Formats a numeric value using invariant culture.</summary>
    public static string InvariantNumber<T>(T value) where T : IFormattable => value.ToString(null, CultureInfo.InvariantCulture);

    /// <summary>Encodes ASCII or UTF-8 text without replacing unrepresentable characters or invalid Unicode.</summary>
    public static byte[] EncodeBytes(ReadOnlySpan<char> value, GeneratorEncoding encoding)
    {
        Encoding encoder = encoding switch
        {
            GeneratorEncoding.AsciiBytes => _ascii,
            GeneratorEncoding.Utf8Bytes => _utf8,
            _ => throw new NotSupportedException($"Encoding '{encoding}' does not use ASCII or UTF-8 bytes.")
        };
        return encoder.GetBytes(value.ToString());
    }

    /// <summary>Quotes text, escaping delimiters and backslashes and delegating other escapes to the supplied formatter.</summary>
    /// <param name="value">The text to quote.</param>
    /// <param name="quote">The string or character delimiter.</param>
    /// <param name="appendEscape">Appends a language-specific escape for a code unit or Unicode scalar.</param>
    /// <param name="unicodeScalars">Combines surrogate pairs before escaping them and rejects unpaired surrogates.</param>
    public static string Quote(ReadOnlySpan<char> value, char quote, Action<StringBuilder, int> appendEscape, bool unicodeScalars = false)
    {
        StringBuilder text = new StringBuilder(value.Length + 2).Append(quote);

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if (c == quote || c == '\\')
                text.Append('\\').Append(c);
            else if (c is >= ' ' and <= '~')
                text.Append(c);
            else
            {
                int scalar = c;

                if (unicodeScalars && char.IsSurrogate(c))
                {
                    if (!char.IsHighSurrogate(c) || i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                        throw new EncoderFallbackException("Text contains an unpaired UTF-16 surrogate.");
                    scalar = char.ConvertToUtf32(c, value[++i]);
                }

                appendEscape(text, scalar);
            }
        }

        return text.Append(quote).ToString();
    }

    public string Literal(IrLiteral literal) => reference?.Invoke(literal) ?? Initializer(literal);

    /// <summary>Formats a declaration's value, resolving its nested aggregates to prepared storage.</summary>
    public string Initializer(IrLiteral literal) => Value(literal, false);

    /// <summary>Formats values in array initializers and record constructors.</summary>
    public string Element(IrLiteral literal) => reference?.Invoke(literal) ?? Value(literal, true);

    private string Value(IrLiteral literal, bool element) => literal switch
    {
        IrBoolLiteral x => Boolean(x.Value),
        IrI8Literal x => Signed(x.Value, IrTypeKind.I8, element),
        IrU8Literal x => Unsigned(x.Value, IrTypeKind.U8, element),
        IrI16Literal x => Signed(x.Value, IrTypeKind.I16, element),
        IrU16Literal x => Unsigned(x.Value, IrTypeKind.U16, element),
        IrI32Literal x => Signed(x.Value, IrTypeKind.I32, element),
        IrU32Literal x => Unsigned(x.Value, IrTypeKind.U32, element),
        IrI64Literal x => Signed(x.Value, IrTypeKind.I64, element),
        IrU64Literal x => Unsigned(x.Value, IrTypeKind.U64, element),
        IrF32Literal x => Format(x.Value),
        IrF64Literal x => Format(x.Value),
        IrCharLiteral x => FormatCharacter(x.Value, x.CharacterKind),
        IrStringLiteral x => FormatString(x.Value.AsSpan()),
        IrNullLiteral x => FormatNull(x.Type),
        IrArrayLiteralBase or IrRecordLiteral => Aggregate(literal),
        _ => throw new NotSupportedException($"Unsupported literal '{literal.GetType().Name}'.")
    };

    public IEnumerable<string> Elements(IrArrayLiteralBase array) => array switch
    {
        IrArrayLiteral x => x.Elements.Select(Element),
        IrPackedStringArrayLiteral x => FormatStrings(x),
        IrPackedArrayLiteral<bool> x => x.Values.Select(Boolean),
        IrPackedArrayLiteral<sbyte> x => x.Values.Select(value => Signed(value, IrTypeKind.I8, true)),
        IrPackedArrayLiteral<byte> x when x.ElementType.CharacterKind is {} kind => x.Values.Select(value => FormatCharacter(value, kind)),
        IrPackedArrayLiteral<byte> x => x.Values.Select(value => Unsigned(value, IrTypeKind.U8, true)),
        IrPackedArrayLiteral<short> x => x.Values.Select(value => Signed(value, IrTypeKind.I16, true)),
        IrPackedArrayLiteral<ushort> x => x.Values.Select(value => Unsigned(value, IrTypeKind.U16, true)),
        IrPackedArrayLiteral<int> x => x.Values.Select(value => Signed(value, IrTypeKind.I32, true)),
        IrPackedArrayLiteral<uint> x => x.Values.Select(value => Unsigned(value, IrTypeKind.U32, true)),
        IrPackedArrayLiteral<long> x => x.Values.Select(value => Signed(value, IrTypeKind.I64, true)),
        IrPackedArrayLiteral<ulong> x => x.Values.Select(value => Unsigned(value, IrTypeKind.U64, true)),
        IrPackedArrayLiteral<float> x => x.Values.Select(Format),
        IrPackedArrayLiteral<double> x => x.Values.Select(Format),
        IrPackedArrayLiteral<char> x => x.Values.Select(value => FormatCharacter(value, IrCharacterKind.Utf16CodeUnit)),
        _ => throw new NotSupportedException($"Unsupported packed type '{array.ElementType}'.")
    };

    private IEnumerable<string> FormatStrings(IrPackedStringArrayLiteral array)
    {
        char[] characters = ArrayPool<char>.Shared.Rent(256);

        try
        {
            for (int i = 0; i < array.Count; i++)
            {
                if (array.IsNull(i))
                {
                    yield return FormatNull(IrType.String);
                    continue;
                }

                int byteLength = array.Offsets[i + 1] - array.Offsets[i];
                int requiredCapacity = array.Encoding == IrTextEncoding.Utf16 ? byteLength / 2 : byteLength;

                if (characters.Length < requiredCapacity)
                {
                    char[] replacement = ArrayPool<char>.Shared.Rent(requiredCapacity);
                    ArrayPool<char>.Shared.Return(characters);
                    characters = replacement;
                }

                int characterCount = array.Decode(i, characters);
                yield return FormatString(characters.AsSpan(0, characterCount));
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(characters);
        }
    }

    private static string Boolean(bool value) => value ? "true" : "false";

    // Widening preserves values; kind retains the declared width without boxing or allocating IR nodes.
    protected abstract string Signed(long value, IrTypeKind kind, bool element);
    protected abstract string Unsigned(ulong value, IrTypeKind kind, bool element);
    protected abstract string Format(float value);
    protected abstract string Format(double value);
    protected abstract string FormatCharacter(uint value, IrCharacterKind kind);
    protected abstract string FormatString(ReadOnlySpan<char> value);
    protected abstract string FormatNull(IrType type);
    protected abstract string Aggregate(IrLiteral literal);
}
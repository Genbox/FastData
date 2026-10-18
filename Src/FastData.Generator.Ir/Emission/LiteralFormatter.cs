using System.Buffers;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Dispatches scalar literals and traverses packed arrays directly through their typed storage.</summary>
public abstract class LiteralFormatter
{
    internal Func<IrLiteral, string?>? Reference { get; set; }

    public string Literal(IrLiteral literal) => Reference?.Invoke(literal) ?? Initializer(literal);

    /// <summary>Formats a declaration's value, resolving its nested aggregates to prepared storage.</summary>
    public string Initializer(IrLiteral literal) => literal switch
    {
        IrBoolLiteral x => Format(x.Value),
        IrI8Literal x => Format(x.Value, false),
        IrU8Literal x => Format(x.Value, false),
        IrI16Literal x => Format(x.Value, false),
        IrU16Literal x => Format(x.Value, false),
        IrI32Literal x => Format(x.Value),
        IrU32Literal x => Format(x.Value),
        IrI64Literal x => Format(x.Value),
        IrU64Literal x => Format(x.Value),
        IrF32Literal x => Format(x.Value),
        IrF64Literal x => Format(x.Value),
        IrCharLiteral x => FormatCharacter(x.Value, x.CharacterKind),
        IrStringLiteral x => FormatString(x.Value.AsSpan()),
        IrNullLiteral x => FormatNull(x.Type),
        IrArrayLiteralBase x => FormatArray(x),
        IrRecordLiteral x => FormatRecord(x),
        _ => throw new NotSupportedException($"Unsupported literal '{literal.GetType().Name}'.")
    };

    /// <summary>Formats values in array initializers and record constructors.</summary>
    public string Element(IrLiteral literal) => literal switch
    {
        IrI8Literal x => Format(x.Value, true),
        IrU8Literal x => Format(x.Value, true),
        IrI16Literal x => Format(x.Value, true),
        IrU16Literal x => Format(x.Value, true),
        _ => Literal(literal)
    };

    public IEnumerable<string> Elements(IrArrayLiteralBase array) => array switch
    {
        IrArrayLiteral x => x.Elements.Select(Element),
        IrPackedStringArrayLiteral x => FormatStrings(x),
        IrPackedArrayLiteral<bool> x => x.Values.Select(Format),
        IrPackedArrayLiteral<sbyte> x => x.Values.Select(value => Format(value, true)),
        IrPackedArrayLiteral<byte> x when x.ElementType.CharacterKind is {} kind => x.Values.Select(value => FormatCharacter(value, kind)),
        IrPackedArrayLiteral<byte> x => x.Values.Select(value => Format(value, true)),
        IrPackedArrayLiteral<short> x => x.Values.Select(value => Format(value, true)),
        IrPackedArrayLiteral<ushort> x => x.Values.Select(value => Format(value, true)),
        IrPackedArrayLiteral<int> x => x.Values.Select(Format),
        IrPackedArrayLiteral<uint> x => x.Values.Select(Format),
        IrPackedArrayLiteral<long> x => x.Values.Select(Format),
        IrPackedArrayLiteral<ulong> x => x.Values.Select(Format),
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

    protected abstract string Format(bool value);
    protected abstract string Format(sbyte value, bool element);
    protected abstract string Format(byte value, bool element);
    protected abstract string Format(short value, bool element);
    protected abstract string Format(ushort value, bool element);
    protected abstract string Format(int value);
    protected abstract string Format(uint value);
    protected abstract string Format(long value);
    protected abstract string Format(ulong value);
    protected abstract string Format(float value);
    protected abstract string Format(double value);
    protected abstract string FormatCharacter(uint value, IrCharacterKind kind);
    protected abstract string FormatString(ReadOnlySpan<char> value);
    protected abstract string FormatNull(IrType type);
    protected abstract string FormatArray(IrArrayLiteralBase array);
    protected abstract string FormatRecord(IrRecordLiteral record);
}
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Genbox.FastData.Ir.Internal;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Building;

/// <summary>Generation-time text encoding for a fixed program encoding.</summary>
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Callers supply non-null text; UTF-8 encoding rejects unpaired surrogates.")]
public sealed class IrText
{
    private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false, true);

    /// <summary>Creates text helpers for a fixed program encoding.</summary>
    public IrText(IrTextEncoding encoding)
    {
        Validator.IsDefined(encoding);
        Encoding = encoding;
    }

    /// <summary>The program's String representation.</summary>
    public IrTextEncoding Encoding { get; }

    /// <summary>U8 for UTF-8 and U16 for UTF-16.</summary>
    public IrType CodeUnitType => Encoding == IrTextEncoding.Utf8 ? IrType.U8 : IrType.U16;

    /// <summary>Counts encoded units at generation time, requiring text that the selected encoding represents losslessly.</summary>
    /// <exception cref="ArgumentException">UTF-8 text contains an unpaired surrogate.</exception>
    public int GetLength(string text) => Encoding == IrTextEncoding.Utf8 ? _utf8.GetByteCount(text) : text.Length;

    /// <summary>Encodes the literal's code units as a U8 or U16 array for generation-time hashing and backend emission.</summary>
    /// <exception cref="ArgumentException">UTF-8 text contains an unpaired surrogate.</exception>
    public IrLiteral EncodeLiteral(string text)
    {
        if (Encoding == IrTextEncoding.Utf8)
            return new IrPackedArrayLiteral<byte>(_utf8.GetBytes(text));

        ushort[] units = new ushort[text.Length];
        for (int i = 0; i < units.Length; i++)
            units[i] = text[i];

        return new IrPackedArrayLiteral<ushort>(units);
    }

    /// <summary>Packs strings directly, retaining neither the source strings nor per-element literals.</summary>
    public IrPackedStringArrayLiteral PackStrings(IReadOnlyList<string?> values)
    {
        // Measure first so all encoded strings fit in one allocation, with a final end offset.
        int[] offsets = new int[checked(values.Count + 1)];
        byte[] nulls = [];
        int size = 0;

        for (int i = 0; i < values.Count; i++)
        {
            string? value = values[i];

            if (value == null)
            {
                if (nulls.Length == 0)
                    nulls = new byte[(values.Count / 8) + (values.Count % 8 == 0 ? 0 : 1)];

                // Null and empty strings both occupy zero bytes; the bitmap distinguishes them.
                nulls[i >> 3] |= (byte)(1 << (i & 7));
            }
            else
                size = checked(size + (Encoding == IrTextEncoding.Utf8 ? _utf8.GetByteCount(value) : checked(value.Length * 2)));

            offsets[i + 1] = size;
        }

        byte[] data = new byte[size];

        for (int i = 0; i < values.Count; i++)
        {
            string? value = values[i];
            if (value == null)
                continue;

            if (Encoding == IrTextEncoding.Utf8)
                _utf8.GetBytes(value, 0, value.Length, data, offsets[i]);
            else
            {
                int offset = offsets[i];

                // Direct code-unit writes preserve unpaired UTF-16 surrogates.
                foreach (char unit in value)
                {
                    data[offset++] = (byte)(unit & 0xFF);
                    data[offset++] = (byte)(unit >> 8);
                }
            }
        }

        return new IrPackedStringArrayLiteral(Encoding, data, offsets, nulls);
    }

    /// <summary>A literal of the program's code-unit type.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value" /> exceeds the code-unit type's range.</exception>
    public IrLiteral Unit(uint value) => Encoding switch
    {
        IrTextEncoding.Utf8 when value <= byte.MaxValue => new IrU8Literal((byte)value),
        IrTextEncoding.Utf16 when value <= ushort.MaxValue => new IrU16Literal((ushort)value),
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, $"The value does not fit a {Encoding} code unit.")
    };
}
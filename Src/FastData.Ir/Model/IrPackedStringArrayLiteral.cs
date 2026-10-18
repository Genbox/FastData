using System.Text;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Model;

/// <summary>Immutable String array stored as encoded bytes, byte offsets, and an optional null bitmap.</summary>
/// <remarks>UTF-16 uses little-endian code units. Offsets contain Count + 1 entries. Nulls have empty slices and a set bitmap bit.</remarks>
public sealed record IrPackedStringArrayLiteral : IrArrayLiteralBase
{
    private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false, true);
    private readonly byte[] _data;
    private readonly byte[] _nullBitmap;
    private readonly int[] _offsets;

    /// <summary>Takes ownership of encoded storage. Callers must stop modifying the arrays after construction.</summary>
    /// <remarks>Program validation checks boundaries and encoding.</remarks>
    public IrPackedStringArrayLiteral(IrTextEncoding encoding, byte[] data, int[] offsets, byte[] nullBitmap)
    {
        Encoding = encoding;
        _data = data;
        _offsets = offsets;
        _nullBitmap = nullBitmap;
        Data = Array.AsReadOnly(data);
        Offsets = Array.AsReadOnly(offsets);
        NullBitmap = Array.AsReadOnly(nullBitmap);
    }

    public override IrType Type { get; } = IrType.ArrayOf(IrType.String);
    public IrTextEncoding Encoding { get; }
    public override int Count => _offsets.Length - 1;
    public IReadOnlyList<byte> Data { get; }
    public IReadOnlyList<int> Offsets { get; }
    public IReadOnlyList<byte> NullBitmap { get; }

    public bool IsNull(int index) => _nullBitmap.Length != 0 && (_nullBitmap[index >> 3] & (1 << (index & 7))) != 0;

    /// <summary>Returns a span over the encoded slice. Use IsNull to distinguish null from empty.</summary>
    public ReadOnlySpan<byte> GetBytes(int index) => _data.AsSpan(_offsets[index], _offsets[index + 1] - _offsets[index]);

    public ReadOnlySpan<byte> AsSpan() => _data;
    public ReadOnlySpan<int> OffsetSpan() => _offsets;

    /// <summary>Materializes one string for reference execution.</summary>
    public override IrLiteral ElementAt(int index)
    {
        if (IsNull(index))
            return new IrNullLiteral(IrType.String);

        int start = _offsets[index];
        int length = _offsets[index + 1] - start;

        if (Encoding == IrTextEncoding.Utf8)
            return new IrStringLiteral(_utf8.GetString(_data, start, length));

        char[] units = new char[length / 2];
        Decode(index, units);

        return new IrStringLiteral(new string(units));
    }

    /// <summary>Decodes one encoded slice into caller-provided storage and returns the character count.</summary>
    internal int Decode(int index, char[] destination)
    {
        int start = _offsets[index];
        int byteLength = _offsets[index + 1] - start;

        if (Encoding == IrTextEncoding.Utf8)
            return _utf8.GetChars(_data, start, byteLength, destination, 0);

        // Decode code units directly to preserve UTF-16 surrogate values.
        int characterCount = byteLength / 2;
        for (int i = 0; i < characterCount; i++)
            destination[i] = (char)(_data[start + (i * 2)] | (_data[start + (i * 2) + 1] << 8));
        return characterCount;
    }

    internal void ValidateUtf8(int index) => _utf8.GetCharCount(_data, _offsets[index], _offsets[index + 1] - _offsets[index]);
}
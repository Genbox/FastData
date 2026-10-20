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
    /// <remarks>The buffers are retained without copying. Callers must not modify them through any alias, including views obtained from the literal. Construction checks storage boundaries and encoding. Program validation checks compatibility with the program encoding.</remarks>
    public IrPackedStringArrayLiteral(IrTextEncoding encoding, byte[] data, int[] offsets, byte[] nullBitmap)
    {
        Encoding = encoding;
        _data = data;
        _offsets = offsets;
        _nullBitmap = nullBitmap;
        Data = data;
        Offsets = offsets;
        NullBitmap = nullBitmap;

        Require(Enum.IsDefined(encoding), "Unsupported text encoding.");
        Require(offsets.Length > 0 && offsets[0] == 0 && offsets[Count] == data.Length, "Packed string offsets must cover the buffer.");
        Require(nullBitmap.Length == 0 || nullBitmap.Length == (Count / 8) + (Count % 8 == 0 ? 0 : 1), "Packed string null bitmap has the wrong length.");

        for (int i = 0; i < Count; i++)
        {
            int start = offsets[i];
            int end = offsets[i + 1];
            Require(start <= end && end <= data.Length, "Packed string offsets must be ordered and in bounds.");
            Require(!IsNull(i) || start == end, "Null strings must have empty slices.");

            if (encoding == IrTextEncoding.Utf16)
                Require((end & 1) == 0, "UTF-16 string boundaries must align to code units.");
            else
            {
                try
                {
                    // A valid combined buffer can still have an offset that splits a UTF-8 sequence.
                    _utf8.GetCharCount(data, start, end - start);
                }
                catch (DecoderFallbackException exception)
                {
                    throw Fail($"Packed string contains invalid UTF-8: {exception.Message}");
                }
            }
        }
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
}
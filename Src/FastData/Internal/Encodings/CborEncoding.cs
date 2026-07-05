using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// CBOR major type 0 unsigned integer head encoding.
/// Reference: RFC 8949 section 3.1, major type 0, https://www.rfc-editor.org/rfc/rfc8949.html#section-3.1
/// </summary>
internal sealed class CborEncoding : IIntegerEncoding
{
    internal static CborEncoding Instance { get; } = new CborEncoding();

    public int MaxEncodedLength => 9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value) => value switch
    {
        < 24 => 1,
        <= byte.MaxValue => 2,
        <= ushort.MaxValue => 3,
        <= uint.MaxValue => 5,
        _ => 9
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(ulong value, Span<byte> destination)
    {
        unchecked
        {
            if (value < 24)
            {
                destination[0] = (byte)value;
                return 1;
            }

            if (value <= byte.MaxValue)
            {
                destination[0] = 24;
                destination[1] = (byte)value;
                return 2;
            }

            if (value <= ushort.MaxValue)
            {
                destination[0] = 25;
                WriteUInt16BigEndian(destination.Slice(1), (ushort)value);
                return 3;
            }

            if (value <= uint.MaxValue)
            {
                destination[0] = 26;
                WriteUInt32BigEndian(destination.Slice(1), (uint)value);
                return 5;
            }

            destination[0] = 27;
            WriteUInt64BigEndian(destination.Slice(1), value);
            return 9;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;

        if (source.IsEmpty)
            return false;

        byte first = source[0];
        if (first >= 0x20)
            return false;

        if (first < 24)
        {
            value = first;
            bytesRead = 1;
            return true;
        }

        switch (first)
        {
            case 24:
                if (source.Length < 2)
                    return false;

                value = source[1];
                bytesRead = 2;
                return true;
            case 25:
                if (source.Length < 3)
                    return false;

                value = ReadUInt16BigEndian(source.Slice(1));
                bytesRead = 3;
                return true;
            case 26:
                if (source.Length < 5)
                    return false;

                value = ReadUInt32BigEndian(source.Slice(1));
                bytesRead = 5;
                return true;
            case 27:
                if (source.Length < 9)
                    return false;

                value = ReadUInt64BigEndian(source.Slice(1));
                bytesRead = 9;
                return true;
            default:
                return false;
        }
    }
}
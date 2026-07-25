using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// VARU64 tag-byte-framed unsigned 64-bit integer encoding. The first byte either stores values 0-247 directly or encodes the following big-endian payload length.
/// Reference: Aljoscha Meyer varu64-rs specification, https://github.com/AljoschaMeyer/varu64-rs
/// </summary>
internal sealed class Varu64Encoding : IIntegerEncoding
{
    internal static Varu64Encoding Instance { get; } = new Varu64Encoding();

    public int MaxEncodedLength => 9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value) => value switch
    {
        < 248 => 1,
        < 256 => 2,
        < 65536 => 3,
        < 16777216 => 4,
        < 4294967296 => 5,
        < 1099511627776 => 6,
        < 281474976710656 => 7,
        < 72057594037927936 => 8,
        _ => 9
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(ulong value, Span<byte> destination)
    {
        if (value < 248)
        {
            destination[0] = (byte)value;
            return 1;
        }

        if (value < 256)
        {
            destination[0] = 248;
            IntegerEncodingHelpers.WriteUInt64BE(value, 1, destination.Slice(1));
            return 2;
        }

        if (value < 65536)
        {
            destination[0] = 249;
            IntegerEncodingHelpers.WriteUInt64BE(value, 2, destination.Slice(1));
            return 3;
        }

        if (value < 16777216)
        {
            destination[0] = 250;
            IntegerEncodingHelpers.WriteUInt64BE(value, 3, destination.Slice(1));
            return 4;
        }

        if (value < 4294967296)
        {
            destination[0] = 251;
            IntegerEncodingHelpers.WriteUInt64BE(value, 4, destination.Slice(1));
            return 5;
        }

        if (value < 1099511627776)
        {
            destination[0] = 252;
            IntegerEncodingHelpers.WriteUInt64BE(value, 5, destination.Slice(1));
            return 6;
        }

        if (value < 281474976710656)
        {
            destination[0] = 253;
            IntegerEncodingHelpers.WriteUInt64BE(value, 6, destination.Slice(1));
            return 7;
        }

        if (value < 72057594037927936)
        {
            destination[0] = 254;
            IntegerEncodingHelpers.WriteUInt64BE(value, 7, destination.Slice(1));
            return 8;
        }

        destination[0] = 255;
        IntegerEncodingHelpers.WriteUInt64BE(value, 8, destination.Slice(1));
        return 9;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;

        if (source.IsEmpty)
            return false;

        byte tag = source[0];
        if ((tag | 0b0000_0111) != 0b1111_1111)
        {
            value = tag;
            bytesRead = 1;
            return true;
        }

        int payloadLength = tag - 247;
        if (source.Length < 1 + payloadLength)
            return false;

        value = IntegerEncodingHelpers.ReadUInt64BE(source.Slice(1, payloadLength), payloadLength);
        if (GetEncodedLength(value) != 1 + payloadLength)
        {
            value = 0;
            return false;
        }

        bytesRead = 1 + payloadLength;
        return true;
    }
}
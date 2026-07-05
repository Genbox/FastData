using System.Runtime.CompilerServices;
using static System.Buffers.Binary.BinaryPrimitives;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// Dlugosz Revision 2 variable-length integer encoding.
/// References: Dlugosz variable-length integer https://web.archive.org/web/20210224160104/http://www.dlugosz.com/ZIP2/VLI.html
/// </summary>
internal sealed class DlugoszEncoding : IIntegerEncoding
{
    internal static DlugoszEncoding Instance { get; } = new DlugoszEncoding();

    public int MaxEncodedLength => 9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value) => value switch
    {
        <= 0x7fUL => 1,
        <= 0x3fffUL => 2,
        <= 0x1fffffUL => 3,
        <= 0x7ffffffUL => 4,
        <= 0x7ffffffffUL => 5,
        <= 0xffffffffffUL => 6,
        <= 0x7ffffffffffffffUL => 8,
        _ => 9
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(ulong value, Span<byte> destination)
    {
        unchecked
        {
            if (value <= 0x7fUL)
            {
                destination[0] = (byte)value;
                return 1;
            }

            if (value <= 0x3fffUL)
            {
                WriteUInt16BigEndian(destination, (ushort)(0x8000UL | value));
                return 2;
            }

            if (value <= 0x1fffffUL)
            {
                destination[0] = (byte)(0xc0 | (value >> 16));
                WriteUInt16BigEndian(destination.Slice(1), (ushort)value);
                return 3;
            }

            if (value <= 0x7ffffffUL)
            {
                WriteUInt32BigEndian(destination, (uint)(0xe0000000UL | value));
                return 4;
            }

            if (value <= 0x7ffffffffUL)
            {
                destination[0] = (byte)(0xe8 | (value >> 32));
                WriteUInt32BigEndian(destination.Slice(1), (uint)value);
                return 5;
            }

            if (value <= 0xffffffffffUL)
            {
                destination[0] = 0xf8;
                destination[1] = (byte)(value >> 32);
                WriteUInt32BigEndian(destination.Slice(2), (uint)value);
                return 6;
            }

            if (value <= 0x7ffffffffffffffUL)
            {
                destination[0] = (byte)(0xf0 | (value >> 56));
                WriteUInt16BigEndian(destination.Slice(1), (ushort)(value >> 40));
                destination[3] = (byte)(value >> 32);
                WriteUInt32BigEndian(destination.Slice(4), (uint)value);
                return 8;
            }

            destination[0] = 0xf9;
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
        if ((first & 0x80) == 0)
        {
            value = first;
            bytesRead = 1;
            return true;
        }

        if ((first & 0xc0) == 0x80)
        {
            if (source.Length < 2)
                return false;

            value = ((ulong)(first & 0x3f) << 8) | source[1];
            bytesRead = 2;
            return true;
        }

        if ((first & 0xe0) == 0xc0)
        {
            if (source.Length < 3)
                return false;

            value = ((ulong)(first & 0x1f) << 16) | ReadUInt16BigEndian(source.Slice(1));
            bytesRead = 3;
            return true;
        }

        switch (first & 0xf8)
        {
            case 0xe0:
                if (source.Length < 4)
                    return false;

                value = ((ulong)(first & 0x07) << 24) | ((ulong)source[1] << 16) | ReadUInt16BigEndian(source.Slice(2));
                bytesRead = 4;
                return true;
            case 0xe8:
                if (source.Length < 5)
                    return false;

                value = ((ulong)(first & 0x07) << 32) | ReadUInt32BigEndian(source.Slice(1));
                bytesRead = 5;
                return true;
            case 0xf0:
                if (source.Length < 8)
                    return false;

                value = ((ulong)(first & 0x07) << 56) | ((ulong)ReadUInt16BigEndian(source.Slice(1)) << 40) | ((ulong)source[3] << 32) | ReadUInt32BigEndian(source.Slice(4));
                bytesRead = 8;
                return true;
        }

        if (first == 0xf8)
        {
            if (source.Length < 6)
                return false;

            value = ((ulong)source[1] << 32) | ReadUInt32BigEndian(source.Slice(2));
            bytesRead = 6;
            return true;
        }

        if (first == 0xf9)
        {
            if (source.Length < 9)
                return false;

            value = ReadUInt64BigEndian(source.Slice(1));
            bytesRead = 9;
            return true;
        }

        return false;
    }
}
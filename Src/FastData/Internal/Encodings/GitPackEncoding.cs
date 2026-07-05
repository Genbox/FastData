using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// Git pack OBJ_OFS_DELTA offset encoding. Bytes carry big-endian 7-bit groups with MSB continuation and a bijective offset added for multi-byte encodings.
/// Reference: Git pack-format documentation, offset encoding, https://git-scm.com/docs/pack-format
/// </summary>
internal sealed class GitPackEncoding : IIntegerEncoding
{
    internal static GitPackEncoding Instance { get; } = new GitPackEncoding();

    public int MaxEncodedLength => 10;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value) => value switch
    {
        <= 0x7fUL => 1,
        <= 0x407fUL => 2,
        <= 0x20407fUL => 3,
        <= 0x1020407fUL => 4,
        <= 0x081020407fUL => 5,
        <= 0x04081020407fUL => 6,
        <= 0x0204081020407fUL => 7,
        <= 0x010204081020407fUL => 8,
        <= 0x0810204081020407fUL => 9,
        _ => 10
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(ulong value, Span<byte> destination)
    {
        unchecked
        {
            int length = GetEncodedLength(value);
            for (int i = length - 1; i >= 0; i--)
            {
                destination[i] = (byte)(value & 0x7f);
                if (i != length - 1)
                    destination[i] |= 0x80;

                value >>= 7;
                if (i != 0)
                    value--;
            }

            return length;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;

        if (source.IsEmpty)
            return false;

        byte c = source[0];
        value = c & 0x7fUL;
        bytesRead = 1;

        while ((c & 0x80) != 0)
        {
            if (value > (ulong.MaxValue - 1) >> 7)
            {
                value = 0;
                bytesRead = 0;
                return false;
            }

            if (bytesRead >= source.Length || bytesRead >= MaxEncodedLength)
            {
                value = 0;
                bytesRead = 0;
                return false;
            }

            value++;
            c = source[bytesRead++];
            value = (value << 7) | (c & 0x7fUL);
        }

        return true;
    }
}
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// bijou64 bijective offset u64 encoding with VARU64-style tag-byte framing and per-tier offsets.
/// Reference: Ink &amp; Switch bijou64 specification, https://github.com/inkandswitch/bijou/blob/main/bijou64/SPEC.md and article https://www.inkandswitch.com/tangents/bijou64/
/// </summary>
internal sealed class Bijou64Encoding : IIntegerEncoding
{
    private const int TagThreshold = 248;

    private static readonly ulong[] Offsets =
    [
        0x0UL,
        0xf8UL,
        0x1f8UL,
        0x101f8UL,
        0x10101f8UL,
        0x1010101f8UL,
        0x101010101f8UL,
        0x10101010101f8UL,
        0x1010101010101f8UL
    ];

    private static readonly ulong[] Bounds =
    [
        Offsets[1],
        Offsets[2],
        Offsets[3],
        Offsets[4],
        Offsets[5],
        Offsets[6],
        Offsets[7],
        Offsets[8],
        ulong.MaxValue
    ];

    internal static Bijou64Encoding Instance { get; } = new Bijou64Encoding();

    public int MaxEncodedLength => 9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value)
    {
        if (value < Bounds[0])
            return 1;

        int bitWidth = 64 - BitOperations.LeadingZeroCount(value);
        int candidate = ((bitWidth - 1) / 8) + 2;
        return value < Bounds[candidate - 2] ? candidate - 1 : candidate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(ulong value, Span<byte> destination)
    {
        unchecked
        {
            if (value < Bounds[0])
            {
                destination[0] = (byte)value;
                return 1;
            }

            int bitWidth = 64 - BitOperations.LeadingZeroCount(value);
            int tier = ((bitWidth - 1) / 8) + 1;
            if (value < Bounds[tier - 1])
                tier--;

            destination[0] = (byte)((TagThreshold + tier) - 1);
            ulong payload = value - Offsets[tier];
            IntegerEncodingHelpers.WriteUInt64BE(payload, tier, destination.Slice(1));
            return tier + 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;

        if (source.IsEmpty)
            return false;

        byte tag = source[0];
        if (tag < TagThreshold)
        {
            value = tag;
            bytesRead = 1;
            return true;
        }

        return tag switch
        {
            0xf8 => TryDecodeTier(source, 1, Offsets[1], out value, out bytesRead),
            0xf9 => TryDecodeTier(source, 2, Offsets[2], out value, out bytesRead),
            0xfa => TryDecodeTier(source, 3, Offsets[3], out value, out bytesRead),
            0xfb => TryDecodeTier(source, 4, Offsets[4], out value, out bytesRead),
            0xfc => TryDecodeTier(source, 5, Offsets[5], out value, out bytesRead),
            0xfd => TryDecodeTier(source, 6, Offsets[6], out value, out bytesRead),
            0xfe => TryDecodeTier(source, 7, Offsets[7], out value, out bytesRead),
            _ => TryDecodeTier(source, 8, Offsets[8], out value, out bytesRead)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryDecodeTier(ReadOnlySpan<byte> source, int tier, ulong offset, out ulong value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;
        if (source.Length < tier + 1)
            return false;

        ulong payload = IntegerEncodingHelpers.ReadUInt64BE(source.Slice(1), tier);
        if (ulong.MaxValue - offset < payload)
            return false;

        value = offset + payload;
        bytesRead = tier + 1;
        return true;
    }
}
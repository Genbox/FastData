using System.Numerics;
using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// bijou32 bijective offset u32 encoding with tag-byte framing and per-tier offsets.
/// Reference: Ink &amp; Switch bijou32 specification, https://github.com/inkandswitch/bijou/blob/main/bijou32/SPEC.md.
/// </summary>
internal sealed class Bijou32Encoding : IIntegerEncoding
{
    private const int TagThreshold = 252;

    private static readonly uint[] Offsets =
    [
        0U,
        252U,
        508U,
        66_044U,
        16_843_260U
    ];

    private static readonly uint[] Bounds =
    [
        Offsets[1],
        Offsets[2],
        Offsets[3],
        Offsets[4],
        uint.MaxValue
    ];

    internal static Bijou32Encoding Instance { get; } = new Bijou32Encoding();

    public int MaxEncodedLength => 5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(ulong value)
    {
        if (value > uint.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), value, "bijou32 supports values up to 2^32-1.");

        return GetEncodedLength((uint)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(ulong value, Span<byte> destination)
    {
        if (value > uint.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), value, "bijou32 supports values up to 2^32-1.");

        return Encode((uint)value, destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        if (!TryDecode(source, out uint decoded, out bytesRead))
        {
            value = 0;
            return false;
        }

        value = decoded;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetEncodedLength(uint value)
    {
        if (value < Bounds[0])
            return 1;

        int bitWidth = 32 - BitOperations.LeadingZeroCount(value);
        int candidate = ((bitWidth - 1) / 8) + 2;
        return value < Bounds[candidate - 2] ? candidate - 1 : candidate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Encode(uint value, Span<byte> destination)
    {
        unchecked
        {
            if (value < Bounds[0])
            {
                destination[0] = (byte)value;
                return 1;
            }

            int bitWidth = 32 - BitOperations.LeadingZeroCount(value);
            int tier = ((bitWidth - 1) / 8) + 1;
            if (value < Bounds[tier - 1])
                tier--;

            destination[0] = (byte)((TagThreshold + tier) - 1);
            uint payload = value - Offsets[tier];
            IntegerEncodingHelpers.WriteUInt32BE(payload, tier, destination.Slice(1));
            return tier + 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDecode(ReadOnlySpan<byte> source, out uint value, out int bytesRead)
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
            0xfc => TryDecodeTier(source, 1, Offsets[1], out value, out bytesRead),
            0xfd => TryDecodeTier(source, 2, Offsets[2], out value, out bytesRead),
            0xfe => TryDecodeTier(source, 3, Offsets[3], out value, out bytesRead),
            _ => TryDecodeTier(source, 4, Offsets[4], out value, out bytesRead)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryDecodeTier(ReadOnlySpan<byte> source, int tier, uint offset, out uint value, out int bytesRead)
    {
        value = 0;
        bytesRead = 0;
        if (source.Length < tier + 1)
            return false;

        uint payload = IntegerEncodingHelpers.ReadUInt32BE(source.Slice(1), tier);
        if (uint.MaxValue - offset < payload)
            return false;

        value = offset + payload;
        bytesRead = tier + 1;
        return true;
    }
}
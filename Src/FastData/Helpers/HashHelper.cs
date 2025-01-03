using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Helpers;

/// <summary>
/// This helper ensures that we get consistent hashes between compile-time and runtime
/// </summary>
public static class HashHelper
{
    private const int StripeSize = 4 * sizeof(ulong);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Hash(object data, uint seed = 0)
    {
        //We don't want randomized hash codes, so we handle string as a special case
        if (data is string str)
            return Hash(str, seed);

        int code = data.GetHashCode();
        return Murmur_64((ulong)code + seed); //we need bits in both low and high bits
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Murmur_64(ulong h)
    {
        h ^= h >> 33;
        h *= 0xff51afd7ed558ccd;
        h ^= h >> 33;
        h *= 0xc4ceb9fe1a85ec53;
        h ^= h >> 33;
        return h;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Hash(string data, uint seed = 0)
    {
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(data.AsSpan());
        State state = new State(seed);

        while (bytes.Length >= StripeSize)
        {
            state.ProcessStripe(bytes);
            bytes = bytes.Slice(StripeSize);
        }

        return state.Complete((uint)bytes.Length, bytes);
    }

    [StructLayout(LayoutKind.Auto)]
    private struct State
    {
        private const ulong Prime64_1 = 0x9E3779B185EBCA87UL;
        private const ulong Prime64_2 = 0xC2B2AE3D27D4EB4FUL;
        private const ulong Prime64_3 = 0x165667B19E3779F9UL;
        private const ulong Prime64_4 = 0x85EBCA77C2B2AE63UL;
        private const ulong Prime64_5 = 0x27D4EB2F165667C5UL;

        private ulong _acc1;
        private ulong _acc2;
        private ulong _acc3;
        private ulong _acc4;
        private readonly ulong _smallAcc;
        private bool _hadFullStripe;

        internal State(ulong seed)
        {
            _acc1 = seed + unchecked(Prime64_1 + Prime64_2);
            _acc2 = seed + Prime64_2;
            _acc3 = seed;
            _acc4 = seed - Prime64_1;

            _smallAcc = seed + Prime64_5;
            _hadFullStripe = false;
        }

        internal void ProcessStripe(ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length >= StripeSize);
            source = source.Slice(0, StripeSize);

            _acc1 = ApplyRound(_acc1, source);
            _acc2 = ApplyRound(_acc2, source.Slice(sizeof(ulong)));
            _acc3 = ApplyRound(_acc3, source.Slice(2 * sizeof(ulong)));
            _acc4 = ApplyRound(_acc4, source.Slice(3 * sizeof(ulong)));

            _hadFullStripe = true;
        }

        private static ulong MergeAccumulator(ulong acc, ulong accN)
        {
            acc ^= ApplyRound(0, accN);
            acc *= Prime64_1;
            acc += Prime64_4;

            return acc;
        }

        private readonly ulong Converge()
        {
            ulong acc = RotateLeft(_acc1, 1) +
                        RotateLeft(_acc2, 7) +
                        RotateLeft(_acc3, 12) +
                        RotateLeft(_acc4, 18);

            acc = MergeAccumulator(acc, _acc1);
            acc = MergeAccumulator(acc, _acc2);
            acc = MergeAccumulator(acc, _acc3);
            acc = MergeAccumulator(acc, _acc4);

            return acc;
        }

        private static ulong ApplyRound(ulong acc, ReadOnlySpan<byte> lane)
        {
            return ApplyRound(acc, BinaryPrimitives.ReadUInt64LittleEndian(lane));
        }

        private static ulong ApplyRound(ulong acc, ulong lane)
        {
            acc += lane * Prime64_2;
            acc = RotateLeft(acc, 31);
            acc *= Prime64_1;

            return acc;
        }

        internal readonly ulong Complete(long length, ReadOnlySpan<byte> remaining)
        {
            ulong acc = _hadFullStripe ? Converge() : _smallAcc;

            acc += (ulong)length;

            while (remaining.Length >= sizeof(ulong))
            {
                ulong lane = BinaryPrimitives.ReadUInt64LittleEndian(remaining);
                acc ^= ApplyRound(0, lane);
                acc = RotateLeft(acc, 27);
                acc *= Prime64_1;
                acc += Prime64_4;

                remaining = remaining.Slice(sizeof(ulong));
            }

            // Doesn't need to be a while since it can occur at most once.
            if (remaining.Length >= sizeof(uint))
            {
                ulong lane = BinaryPrimitives.ReadUInt32LittleEndian(remaining);
                acc ^= lane * Prime64_1;
                acc = RotateLeft(acc, 23);
                acc *= Prime64_2;
                acc += Prime64_3;

                remaining = remaining.Slice(sizeof(uint));
            }

            for (int i = 0; i < remaining.Length; i++)
            {
                ulong lane = remaining[i];
                acc ^= lane * Prime64_5;
                acc = RotateLeft(acc, 11);
                acc *= Prime64_1;
            }

            return Avalanche(acc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Avalanche(ulong hash)
        {
            hash ^= hash >> 33;
            hash *= Prime64_2;
            hash ^= hash >> 29;
            hash *= Prime64_3;
            hash ^= hash >> 32;
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RotateLeft(ulong value, int offset) => (value << offset) | (value >> (64 - offset));
    }
}
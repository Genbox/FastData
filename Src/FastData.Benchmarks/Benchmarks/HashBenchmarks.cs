using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Order;
using Genbox.FastData.Internal.Hashes;
using static System.Numerics.BitOperations;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class HashBenchmarks
{
    private string[] _array = null!;

    [Params(1_000, 10_000, 100_000, 1_000_000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _array = Enumerable.Range(0, Size).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).ToArray();
    }

    [Benchmark]
    public ulong DJB2HashTest()
    {
        ulong value = 0;

        foreach (string s in _array)
        {
            ref char ptr = ref MemoryMarshal.GetReference(s.AsSpan());
            ref byte ptr2 = ref Unsafe.As<char, byte>(ref ptr);
            value += DJB2Hash.ComputeHash(ref ptr2, s.Length);
        }

        return value;
    }

    [Benchmark]
    public ulong XXHashTest()
    {
        ulong value = 0;

        foreach (string s in _array)
            value += XxHash.ComputeHash(s);

        return value;
    }

    private static class XxHash
    {
        private const ulong PRIME64_1 = 0x9E3779B185EBCA87UL;
        private const ulong PRIME64_2 = 0xC2B2AE3D27D4EB4FUL;
        private const ulong PRIME64_3 = 0x165667B19E3779F9UL;
        private const ulong PRIME64_4 = 0x85EBCA77C2B2AE63UL;
        private const ulong PRIME64_5 = 0x27D4EB2F165667C5UL;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ComputeHash(ReadOnlySpan<char> s, ulong seed = PRIME64_5) => ComputeHash(ref MemoryMarshal.GetReference(s), s.Length, seed);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ComputeHash(ref char ptr, int length, ulong seed = PRIME64_5)
        {
            ulong hash1 = seed + (ulong)length;

            ref ulong ptr64 = ref Unsafe.As<char, ulong>(ref ptr);
            while (length >= 4)
            {
                hash1 ^= Round(0, ptr64);
                hash1 = (RotateLeft(hash1, 27) * PRIME64_1) + PRIME64_4;
                ptr64 = ref Unsafe.Add(ref ptr64, 1);
                length -= 4;
            }

            ref ushort ptr16 = ref Unsafe.As<ulong, ushort>(ref ptr64);
            while (length-- > 0)
            {
                hash1 ^= ptr16 * PRIME64_5;
                hash1 = RotateLeft(hash1, 11) * PRIME64_1;
                ptr16 = ref Unsafe.Add(ref ptr16, 1);
            }

            hash1 ^= hash1 >> 33;
            hash1 *= PRIME64_2;
            hash1 ^= hash1 >> 29;
            hash1 *= PRIME64_3;
            hash1 ^= hash1 >> 32;
            return unchecked((uint)hash1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Round(ulong acc, ulong input)
        {
            acc += input * PRIME64_2;
            acc = RotateLeft(acc, 31);
            acc *= PRIME64_1;
            return acc;
        }
    }
}
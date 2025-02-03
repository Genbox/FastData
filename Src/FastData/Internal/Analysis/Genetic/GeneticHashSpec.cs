using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Compat;

namespace Genbox.FastData.Internal.Analysis.Genetic;

[StructLayout(LayoutKind.Auto)]
[SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
[SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out")]
internal struct GeneticHashSpec : IHashSpec
{
    internal int ExtractorSeed;
    internal int MixerSeed;
    internal int MixerIterations;
    internal int AvalancheSeed;
    internal int AvalancheIterations;
    internal uint Seed;
    internal StringSegment[] Segments;

    public Func<string, uint> GetFunction()
    {
        // We create a hash function dynamically. Which functions that are run are determined on two seeds.

        // First, we build the mixer

        return null!;
    }

    public Expression<Func<ulong, ulong>> GetMixer()
    {
        // We create a hash function dynamically. Which functions that are run are determined on two seeds.

        // First, we build the mixer
        Random mixerRng = new Random(MixerSeed);
        var mixer = CreateMixer(MixerIterations, mixerRng);

        return mixer;
    }

    private static Expression<Func<ulong, ulong>> CreateMixer(int iterations, Random rng)
    {
        Expression body = Expression.Parameter(typeof(ulong), "acc");

        for (int i = 0; i < iterations; i++)
        {
            int choice = rng.Next(1, 6);

            body = choice switch
            {
                1 => Add(body, Seeds[rng.Next(0, Seeds.Length)]),
                2 => Multiply(body, Seeds[rng.Next(0, Seeds.Length)]),
                3 => RotateLeft(body, 31),
                4 => RotateRight(body, 31),
                5 => XorShift(body, 31),
                _ => throw new InvalidOperationException("Value out of range"),
            };
        }

        return Expression.Lambda<Func<ulong, ulong>>(body, Expression.Parameter(typeof(ulong), "acc"));
    }

    private static Expression Add(Expression value, ulong amount)
    {
        // value + amount
        return Expression.Add(value, Expression.Constant(amount));
    }

    private static Expression Multiply(Expression value, ulong amount)
    {
        // value * amount
        return Expression.Multiply(value, Expression.Constant(amount));
    }

    private static Expression Xor(Expression value, byte amount)
    {
        // value ^ amount
        return Expression.ExclusiveOr(value, Expression.Constant(amount));
    }

    private static Expression RotateLeft(Expression value, int offset)
    {
        // (value << offset) | (value >> (64 - offset));
        return Expression.Or(Expression.LeftShift(value, Expression.Constant(offset)), Expression.RightShift(value, Expression.Constant(64 - offset)));
    }

    private static Expression RotateRight(Expression value, int offset)
    {
        // (value >> offset) | (value << (64 - offset));
        return Expression.Or(Expression.RightShift(value, Expression.Constant(offset)), Expression.LeftShift(value, Expression.Constant(64 - offset)));
    }

    private static Expression XorShift(Expression value, int amount)
    {
        // value ^= (value >> amount)
        return Expression.ExclusiveOr(value, Expression.RightShift(value, Expression.Constant(amount)));
    }

    // private static ulong Round(ulong acc, ulong input)
    // {
    //     acc += input * PRIME64_2;
    //     acc = RotateLeft(acc, 31);
    //     acc *= PRIME64_1;
    //     return acc;
    // }
    //
    // private static ulong MergeRound(ulong acc, ulong val)
    // {
    //     val = Round(0, val);
    //     acc ^= val;
    //     acc = (acc * PRIME64_1) + PRIME64_4;
    //     return acc;
    // }
    //
    // public static unsafe uint ComputeHash(ReadOnlySpan<char> s, int length)
    // {
    //     fixed (char* cPtr = s)
    //     {
    //         byte* data = (byte*)cPtr;
    //
    //         ulong h64;
    //         if (length >= 32)
    //         {
    //             byte* bEnd = data + length;
    //             byte* limit = bEnd - 31;
    //
    //             ulong v1 = unchecked(PRIME64_1 + PRIME64_2);
    //             ulong v2 = PRIME64_2;
    //             ulong v3 = 0;
    //             ulong v4 = PRIME64_1;
    //
    //             do
    //             {
    //                 v1 = Round(v1, Read64(data));
    //                 data += 8;
    //                 v2 = Round(v2, Read64(data));
    //                 data += 8;
    //                 v3 = Round(v3, Read64(data));
    //                 data += 8;
    //                 v4 = Round(v4, Read64(data));
    //                 data += 8;
    //             } while (data < limit);
    //
    //             h64 = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
    //             h64 = MergeRound(h64, v1);
    //             h64 = MergeRound(h64, v2);
    //             h64 = MergeRound(h64, v3);
    //             h64 = MergeRound(h64, v4);
    //         }
    //         else
    //             h64 = PRIME64_5;
    //
    //         h64 += (uint)length;
    //
    //         length &= 31;
    //         while (length >= 8)
    //         {
    //             ulong k1 = Round(0, Read64(data));
    //             data += 8;
    //             h64 ^= k1;
    //             h64 = (RotateLeft(h64, 27) * PRIME64_1) + PRIME64_4;
    //             length -= 8;
    //         }
    //
    //         if (length >= 4)
    //         {
    //             h64 ^= Read32(data) * PRIME64_1;
    //             data += 4;
    //             h64 = (RotateLeft(h64, 23) * PRIME64_2) + PRIME64_3;
    //             length -= 4;
    //         }
    //
    //         while (length > 0)
    //         {
    //             h64 ^= Read8(data) * PRIME64_5;
    //             data++;
    //             h64 = RotateLeft(h64, 11) * PRIME64_1;
    //             length--;
    //         }
    //
    //         h64 ^= h64 >> 33;
    //         h64 *= PRIME64_2;
    //         h64 ^= h64 >> 29;
    //         h64 *= PRIME64_3;
    //         h64 ^= h64 >> 32;
    //         return (uint)h64;
    //     }
    // }

    // A good seed has the following properties:
    // - Odd: Avoids getting the mixer stuck in a loop of 0.
    // - Large: Means we push a lot of the lower bits into higher bits. Gives better avalanche.
    // - Low bias: No correlation between input bits and output bits
    private static readonly uint[] Seeds =
    [
        0x85EBCA6B, 0xC2B2AE35, //Murmur
        0x45D9F3B, // Degski
        0x9E3779B9, // FP32
        0x7FEB352D, 0x846CA68B, // Lowbias
        0xED5AD4BB, 0xAC4C1B51, 0x31848BAB, //Triple
        0x85EBCA77, 0xC2B2AE3D, // XXHash2
    ];

    // private static readonly Expression<Func<uint, uint>>[] Mixers =
    // [
    //     static x => x,
    //     static x => x + Seeds[0],
    //     static x => (1u | x) + (x * x),
    //     static x => x ^ (x >> 16),
    //     static x => ((x << 5) + x) ^ x,
    //     static x => BitOperations.RotateRight(x, 16),
    //     static x => BitOperations.RotateLeft(x, 16)
    // ];

    public string Construct()
    {
        return $$"""
                 public static uint Hash(ReadOnlySpan<char> str)
                 {

                 }
                 """;
    }
}
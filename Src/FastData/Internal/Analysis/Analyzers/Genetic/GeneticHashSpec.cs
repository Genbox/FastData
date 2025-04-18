using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Analysis.Misc;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic;

[StructLayout(LayoutKind.Auto)]
[SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
[SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out")]
internal struct GeneticHashSpec(int mixerSeed, int mixerIterations, int avalancheSeed, int avalancheIterations, StringSegment[] segments) : IHashSpec
{
    internal int MixerSeed = mixerSeed;
    internal int MixerIterations = mixerIterations;
    internal int AvalancheSeed = avalancheSeed;
    internal int AvalancheIterations = avalancheIterations;
    internal StringSegment[] Segments = segments;

    public readonly HashFunc GetHashFunction() => Hash;
    public readonly EqualFunc GetEqualFunction() => static (a, b) => a.Equals(b, StringComparison.Ordinal);

    private readonly uint Hash(string str)
    {
        ref byte ptr = ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(str.AsSpan()));
        int length = str.Length * 2;
        ulong acc = 42;

        //TODO: move out for perf
        Func<ulong, ulong, ulong> mixer = GetMixer().Compile();
        Func<ulong, ulong> avalanche = GetAvalanche().Compile();

        if (length >= 32)
        {
            ref byte limit = ref Unsafe.Add(ref ptr, length - 31);

            ulong acc0 = 0; //TODO: Seed correctly
            ulong acc1 = 0;
            ulong acc2 = 0;
            ulong acc3 = 0;

            do
            {
                acc0 = mixer(acc0, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);

                acc1 = mixer(acc1, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);

                acc2 = mixer(acc2, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);

                acc3 = mixer(acc3, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);
            } while (Unsafe.IsAddressLessThan(ref ptr, ref limit));

            acc = mixer(acc, acc0);
            acc = mixer(acc, acc1);
            acc = mixer(acc, acc2);
            acc = mixer(acc, acc3);

            //TODO: acc += length;
            length &= 31;
        }

        if (length >= 16)
        {
            ref byte limit = ref Unsafe.Add(ref ptr, length - 15);

            ulong acc0 = 0; //TODO: Seed correctly
            ulong acc1 = 0;

            do
            {
                acc0 = mixer(acc0, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);

                acc1 = mixer(acc1, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);
            } while (Unsafe.IsAddressLessThan(ref ptr, ref limit));

            acc = mixer(acc, acc0);
            acc = mixer(acc, acc1);

            //TODO: acc += length;
            length &= 15;
        }

        while (length >= 8)
        {
            acc = mixer(acc, Unsafe.ReadUnaligned<ulong>(ref ptr));
            ptr = ref Unsafe.Add(ref ptr, 8);
            length -= 8;
        }

        if (length >= 4)
        {
            acc = mixer(acc, Unsafe.ReadUnaligned<uint>(ref ptr));
            ptr = ref Unsafe.Add(ref ptr, 4);
            length -= 4;
        }

        while (length > 0)
        {
            acc = mixer(acc, Unsafe.ReadUnaligned<byte>(ref ptr));
            ptr = ref Unsafe.Add(ref ptr, 1);
            length--;
        }

        return (uint)avalanche(acc);
    }

    internal readonly Expression<Func<ulong, ulong, ulong>> GetMixer()
    {
        ParameterExpression accParam = Expression.Parameter(typeof(ulong), "accParam");
        ParameterExpression inputParam = Expression.Parameter(typeof(ulong), "inputParam");
        ParameterExpression acc = Expression.Parameter(typeof(ulong), "acc");

        IEnumerable<Expression> expressions = CreateMixer(MixerIterations, new Random(MixerSeed), acc);

        BinaryExpression initLocalAcc = Expression.Assign(acc, Expression.Add(accParam, Expression.Multiply(inputParam, Expression.Constant((ulong)Seeds[0]))));
        BlockExpression block = Expression.Block([acc], expressions.Prepend(initLocalAcc));
        return Expression.Lambda<Func<ulong, ulong, ulong>>(block, accParam, inputParam);
    }

    internal readonly Expression<Func<ulong, ulong>> GetAvalanche()
    {
        ParameterExpression accParam = Expression.Parameter(typeof(ulong), "accParam");
        ParameterExpression acc = Expression.Parameter(typeof(ulong), "acc");

        IEnumerable<Expression> expressions = CreateMixer(AvalancheIterations, new Random(AvalancheSeed), acc);

        BinaryExpression initLocalAcc = Expression.Assign(acc, accParam);
        BlockExpression block = Expression.Block([acc], expressions.Prepend(initLocalAcc));
        return Expression.Lambda<Func<ulong, ulong>>(block, accParam);
    }

    private static IEnumerable<Expression> CreateMixer(int iterations, Random rng, ParameterExpression acc)
    {
        for (int i = 0; i < iterations; i++)
        {
            int choice = rng.Next(1, 7);

            Expression body = acc;
            body = choice switch
            {
                1 => Add(body, Seeds[rng.Next(0, Seeds.Length)]),
                2 => Multiply(body, Seeds[rng.Next(0, Seeds.Length)]),
                3 => RotateLeft(body, rng.Next(1, 64)),
                4 => RotateRight(body, rng.Next(1, 64)),
                5 => XorShift(body, rng.Next(1, 64)),
                6 => Square(body),
                _ => throw new InvalidOperationException("Value out of range")
            };

            yield return Expression.Assign(acc, body);
        }
    }

    private static BinaryExpression Add(Expression e, ulong x) => Expression.Add(e, Expression.Constant(x));
    private static BinaryExpression Multiply(Expression e, ulong x) => Expression.Multiply(e, Expression.Constant(x));
    private static BinaryExpression RotateLeft(Expression e, int x) => Expression.Or(Expression.LeftShift(e, Expression.Constant(x)), Expression.RightShift(e, Expression.Constant(64 - x)));
    private static BinaryExpression RotateRight(Expression e, int x) => Expression.Or(Expression.RightShift(e, Expression.Constant(x)), Expression.LeftShift(e, Expression.Constant(64 - x)));
    private static BinaryExpression XorShift(Expression e, int x) => Expression.ExclusiveOr(e, Expression.RightShift(e, Expression.Constant(x)));
    private static BinaryExpression Square(Expression e) => Expression.Add(Expression.Or(Expression.Constant(1UL), e), Expression.Multiply(e, e));

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
}
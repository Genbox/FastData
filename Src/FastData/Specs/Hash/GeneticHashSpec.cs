using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers;

namespace Genbox.FastData.Specs.Hash;

[StructLayout(LayoutKind.Auto)]
[SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
[SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out")]
public struct GeneticHashSpec(int mixerSeed, int mixerIterations, int avalancheSeed, int avalancheIterations, StringSegment[] segments) : IHashSpec
{
    internal int MixerSeed = mixerSeed;
    internal int MixerIterations = mixerIterations;
    internal int AvalancheSeed = avalancheSeed;
    internal int AvalancheIterations = avalancheIterations;
    internal StringSegment[] Segments = segments;

    public readonly HashFunc GetHashFunction()
    {
        Func<uint, uint, uint> mixer = GetMixer().Compile();
        Func<uint, uint> avalanche = GetAvalanche().Compile();
        return (str, seed) => Hash((string)str, seed, mixer, avalanche);
    }

    private static uint Hash(string str, uint seed, Func<uint, uint, uint> mixer, Func<uint, uint> avalanche)
    {
        uint acc = seed;

        for (int i = 0; i < str.Length; i++)
            acc = mixer(acc, str[i]);

        return avalanche(acc);
    }

    public readonly EqualFunc GetEqualFunction() => static (a, b) => ((string)a).Equals((string)b, StringComparison.Ordinal);

    public readonly Expression<Func<uint, uint, uint>> GetMixer()
    {
        ParameterExpression accParam = Expression.Parameter(typeof(uint), "accParam");
        ParameterExpression inputParam = Expression.Parameter(typeof(uint), "inputParam");
        ParameterExpression acc = Expression.Parameter(typeof(uint), "acc");

        IEnumerable<Expression> expressions = CreateMixer(MixerIterations, new Random(MixerSeed), acc);

        BinaryExpression initLocalAcc = Expression.Assign(acc, Expression.Add(accParam, Expression.Multiply(inputParam, Expression.Constant(Seeds[0]))));
        BlockExpression block = Expression.Block([acc], expressions.Prepend(initLocalAcc));
        return Expression.Lambda<Func<uint, uint, uint>>(block, accParam, inputParam);
    }

    public readonly Expression<Func<uint, uint>> GetAvalanche()
    {
        ParameterExpression accParam = Expression.Parameter(typeof(uint), "accParam");
        ParameterExpression acc = Expression.Parameter(typeof(uint), "acc");

        IEnumerable<Expression> expressions = CreateMixer(AvalancheIterations, new Random(AvalancheSeed), acc);

        BinaryExpression initLocalAcc = Expression.Assign(acc, accParam);
        BlockExpression block = Expression.Block([acc], expressions.Prepend(initLocalAcc));
        return Expression.Lambda<Func<uint, uint>>(block, accParam);
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

    private static BinaryExpression Add(Expression e, uint x) => Expression.Add(e, Expression.Constant(x));
    private static BinaryExpression Multiply(Expression e, uint x) => Expression.Multiply(e, Expression.Constant(x));
    private static BinaryExpression RotateLeft(Expression e, int x) => Expression.Or(Expression.LeftShift(e, Expression.Constant(x)), Expression.RightShift(e, Expression.Constant(64 - x)));
    private static BinaryExpression RotateRight(Expression e, int x) => Expression.Or(Expression.RightShift(e, Expression.Constant(x)), Expression.LeftShift(e, Expression.Constant(64 - x)));
    private static BinaryExpression XorShift(Expression e, int x) => Expression.ExclusiveOr(e, Expression.RightShift(e, Expression.Constant(x)));
    private static BinaryExpression Square(Expression e) => Expression.Add(Expression.Or(Expression.Constant(1U), e), Expression.Multiply(e, e));

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
        0x85EBCA77, 0xC2B2AE3D // XXHash2
    ];
}
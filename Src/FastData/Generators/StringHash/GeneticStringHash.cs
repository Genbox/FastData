using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Expressions;
using Genbox.FastData.Internal.Misc;
using static System.Linq.Expressions.Expression;
using static Genbox.FastData.Internal.Helpers.ExpressionHelper;

namespace Genbox.FastData.Generators.StringHash;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
internal sealed record GeneticStringHash : IStringHash
{
    // A good seed has the following properties:
    // - Odd: Avoids getting the mixer stuck in a loop of 0.
    // - Large: Means we push a lot of the lower bits into higher bits. Gives better avalanche.
    // - Low bias: No correlation between input bits and output bits
    private static readonly ulong[] Seeds =
    [
        0xFF51AFD7ED558CCD, 0xC4CEB9FE1A85EC53 //Murmur
    ];

    internal GeneticStringHash(ArraySegment segment, int mixerSeed, int mixerIterations, int avalancheSeed, int avalancheIterations)
    {
        Segment = segment;
        MixerSeed = mixerSeed;
        MixerIterations = mixerIterations;
        AvalancheSeed = avalancheSeed;
        AvalancheIterations = avalancheIterations;
    }

    internal ArraySegment Segment { get; }
    internal int MixerSeed { get; }
    internal int MixerIterations { get; }
    internal int AvalancheSeed { get; }
    internal int AvalancheIterations { get; }

    public HashFunc<string> GetHashFunction() => GetExpression().Compile();
    public Expression<HashFunc<string>> GetExpression() => ExpressionHashBuilder.Build([Segment], GetMixer(), GetAvalanche());

    private Mixer GetMixer() => (hash, read) =>
    {
        Random rng = new Random(MixerSeed);

        BinaryExpression op = GetOp(rng, hash, read);
        return CreateMixer(MixerIterations, rng, op);
    };

    private Avalanche GetAvalanche() => hash => CreateMixer(AvalancheIterations, new Random(AvalancheSeed), hash);

    private static Expression CreateMixer(int iterations, Random rng, Expression value)
    {
        for (int i = 0; i < iterations; i++)
        {
            value = GetMix(rng, value);
        }

        return value;
    }

    private static BinaryExpression GetMix(Random rng, Expression hash) =>
        rng.Next(1, 7) switch
        {
            1 => MixAdd(hash, Seeds[rng.Next(0, Seeds.Length)]),
            2 => MixMultiply(hash, Seeds[rng.Next(0, Seeds.Length)]),
            3 => MixRotateLeft(hash, rng.Next(1, 64)),
            4 => MixRotateRight(hash, rng.Next(1, 64)),
            5 => MixXorShift(hash, rng.Next(1, 64)),
            6 => MixSquare(hash),
            _ => throw new InvalidOperationException("Value out of range")
        };

    private static BinaryExpression GetOp(Random rng, Expression hash, Expression read) =>
        rng.Next(1, 4) switch
        {
            1 => OpAdd(hash, read),
            2 => OpSubtract(hash, read),
            3 => OpMultiply(hash, read),
            4 => OpXor(hash, read),
            _ => throw new InvalidOperationException("Value out of range")
        };

    private static BinaryExpression OpAdd(Expression e, Expression x) => Add(e, x);
    private static BinaryExpression OpSubtract(Expression e, Expression x) => Subtract(e, x);
    private static BinaryExpression OpMultiply(Expression e, Expression x) => Multiply(e, x);
    private static BinaryExpression OpXor(Expression e, Expression x) => ExclusiveOr(e, x);

    private static BinaryExpression MixAdd(Expression e, ulong x) => Add(e, Constant(x));
    private static BinaryExpression MixMultiply(Expression e, ulong x) => Multiply(e, Constant(x));
    private static BinaryExpression MixRotateLeft(Expression e, int x) => Or(LeftShift(e, Constant(x)), RightShift(e, Constant(64 - x)));
    private static BinaryExpression MixRotateRight(Expression e, int x) => Or(RightShift(e, Constant(x)), LeftShift(e, Constant(64 - x)));
    private static BinaryExpression MixXorShift(Expression e, int x) => ExclusiveOr(e, RightShift(e, Constant(x)));
    private static BinaryExpression MixSquare(Expression e) => Add(Or(Constant(1UL), e), Multiply(e, e));

    public override string ToString() =>
        $"""
         {nameof(Segment)} = {Segment.ToString()}
         {nameof(MixerSeed)} = {MixerSeed}
         {nameof(MixerIterations)} = {MixerIterations}
         {nameof(AvalancheSeed)} = {AvalancheSeed}
         {nameof(AvalancheIterations)} = {AvalancheIterations}
         Mixer = {Print(GetMixer())}
         Avalanche = {Print(GetAvalanche())}
         """;
}
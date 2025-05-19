using System.Linq.Expressions;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Specs.Misc;
using static Genbox.FastData.Internal.Helpers.ExpressionHelper;

namespace Genbox.FastData.Specs.Hash;

public sealed record BruteForceStringHash : IExpressionStringHash
{
    //We need this ctor when resuing the object
    internal BruteForceStringHash() { }

    public BruteForceStringHash(StringSegment segment, Mixer mixer, Avalanche avalance)
    {
        Segment = segment;
        Mixer = mixer;
        Avalanche = avalance;
    }

    public HashFunc GetHashFunction() => BuildExpression().Compile();
    public Expression<HashFunc> BuildExpression() => ExpressionHashBuilder.Build([Segment], Mixer, Avalanche);

    public override string ToString() => $"{nameof(Segment)} = {Segment.ToString()}, {nameof(Mixer)} = {Print(Mixer)}, {nameof(Avalanche)} = {Print(Avalanche)}";

    public StringSegment Segment { get; internal set; }
    public Mixer Mixer { get; internal set; }
    public Avalanche Avalanche { get; internal set; }
}
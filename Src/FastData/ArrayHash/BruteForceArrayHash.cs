using System.Linq.Expressions;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Specs;
using Genbox.FastData.Specs.Misc;
using static Genbox.FastData.Internal.Helpers.ExpressionHelper;

namespace Genbox.FastData.ArrayHash;

public sealed record BruteForceArrayHash : IExpressionArrayHash
{
    //We need this ctor when resuing the object
    internal BruteForceArrayHash() { }

    public BruteForceArrayHash(ArraySegment segment, Mixer mixer, Avalanche avalance)
    {
        Segment = segment;
        Mixer = mixer;
        Avalanche = avalance;
    }

    public HashFunc GetHashFunction() => BuildExpression().Compile();
    public Expression<HashFunc> BuildExpression() => ExpressionHashBuilder.Build([Segment], Mixer, Avalanche);

    public override string ToString() => $"{nameof(Segment)} = {Segment.ToString()}, {nameof(Mixer)} = {Print(Mixer)}, {nameof(Avalanche)} = {Print(Avalanche)}";

    public ArraySegment Segment { get; internal set; }
    public Mixer Mixer { get; internal set; }
    public Avalanche Avalanche { get; internal set; }
}
using System.Linq.Expressions;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Specs.Misc;
using static Genbox.FastData.Internal.Helpers.ExpressionHelper;

namespace Genbox.FastData.Specs.Hash;

public sealed record BruteForceStringHash(StringSegment Segment, Mixer Mixer, Avalanche Avalanche) : IExpressionStringHash
{
    public HashFunc GetHashFunction() => BuildExpression().Compile();
    public Expression<HashFunc> BuildExpression() => ExpressionHashBuilder.Build([Segment], Mixer, Avalanche);

    public override string ToString() => $"{nameof(Segment)} = {Segment.ToString()}, {nameof(Mixer)} = {Print(Mixer)}, {nameof(Avalanche)} = {Print(Avalanche)}";

    public StringSegment Segment { get; internal set; } = Segment;
    public Mixer Mixer { get; internal set; } = Mixer;
    public Avalanche Avalanche { get; internal set; } = Avalanche;
}
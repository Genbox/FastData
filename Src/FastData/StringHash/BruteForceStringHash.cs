using System.Linq.Expressions;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Analysis.Expressions;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Misc;
using static Genbox.FastData.Internal.Helpers.ExpressionHelper;

namespace Genbox.FastData.StringHash;

public sealed record BruteForceStringHash : IStringHash
{
    //We need this ctor when resuing the object
    internal BruteForceStringHash() { }

    internal BruteForceStringHash(ArraySegment segment, Mixer mixer, Avalanche avalanche)
    {
        Segment = segment;
        Mixer = mixer;
        Avalanche = avalanche;
    }

    public HashFunc<string> GetHashFunction() => GetExpression().Compile();

    public Expression<HashFunc<string>> GetExpression() => ExpressionHashBuilder.Build([Segment], Mixer, Avalanche);

    internal ArraySegment Segment { get; set; }
    internal Mixer Mixer { get; set; }
    internal Avalanche Avalanche { get; set; }

    public override string ToString() =>
        $"""
         {nameof(Segment)} = {Segment.ToString()}
         {nameof(Mixer)} = {Print(Mixer)}
         {nameof(Avalanche)} = {Print(Avalanche)}
         """;
}
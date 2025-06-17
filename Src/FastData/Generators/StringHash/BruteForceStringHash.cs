using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Expressions;
using Genbox.FastData.Internal.Misc;
using static Genbox.FastData.Internal.Helpers.ExpressionHelper;

namespace Genbox.FastData.Generators.StringHash;

internal sealed record BruteForceStringHash : IStringHash
{
    //We need this ctor when resuing the object
    internal BruteForceStringHash() { }

    internal BruteForceStringHash(ArraySegment segment, Mixer mixer, Avalanche avalanche)
    {
        Segment = segment;
        Mixer = mixer;
        Avalanche = avalanche;
    }

    internal ArraySegment Segment { get; set; }
    internal Mixer Mixer { get; set; }
    internal Avalanche Avalanche { get; set; }
    public State[]? State => null;

    public HashFunc<string> GetHashFunction() => GetExpression().Compile();

    public Expression<HashFunc<string>> GetExpression() => ExpressionHashBuilder.Build([Segment], Mixer, Avalanche);
    public ReaderFunctions Functions => ReaderFunctions.All;

    public override string ToString() =>
        $"""
         {nameof(Segment)} = {Segment.ToString()}
         {nameof(Mixer)} = {Print(Mixer)}
         {nameof(Avalanche)} = {Print(Avalanche)}
         """;
}
using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Expressions;
using Genbox.FastData.Internal.Misc;
using static Genbox.FastData.Generators.Helpers.ExpressionHelper;

namespace Genbox.FastData.Generators.StringHash;

internal sealed record BruteForceStringHash : IStringHash
{
    //We need this ctor when resuing the object
    internal BruteForceStringHash() {}

    internal BruteForceStringHash(ArraySegment segment, Mixer mixer, Avalanche avalanche, int unitSize = 1)
    {
        Segment = segment;
        Mixer = mixer;
        Avalanche = avalanche;
        UnitSize = unitSize;
    }

    internal ArraySegment Segment { get; set; }
    internal Mixer Mixer { get; set; }
    internal Avalanche Avalanche { get; set; }
    internal bool IgnoreCase { get; set; }
    internal int UnitSize { get; set; } = 1;
    public AdditionalData[]? AdditionalData => null;
    public IEnumerable<IEarlyExit> GetMandatoryExits() => Segment.Length == -1 && Segment.Offset == 0 ? [] : [new LengthLessThanEarlyExit(Segment.GetRequiredLength(UnitSize))];

    public Expression<StringHashFunc> GetExpression() => ExpressionHashBuilder.Build([Segment], Mixer, Avalanche, IgnoreCase);

    public override string ToString() =>
        $"""
         {nameof(Segment)} = {Segment.ToString()}
         {nameof(Mixer)} = {Print(Mixer)}
         {nameof(Avalanche)} = {Print(Avalanche)}
         """;
}
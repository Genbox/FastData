using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;
using Genbox.FastData.Specs.Misc;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic;

internal sealed class StringSegmentGene(string name, StringSegment[] segments) : Gene<StringSegment[]>(name, [])
{
    public override void Mutate(IRandom rng)
    {
        Value = [segments[rng.Next(0, segments.Length)]];
    }

    public override IGene Clone() => new StringSegmentGene(Name, segments);
}
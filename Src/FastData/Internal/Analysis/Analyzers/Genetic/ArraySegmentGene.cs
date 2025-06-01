using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic;

internal sealed class ArraySegmentGene(string name, ArraySegment[] segments) : Gene<ArraySegment>(name, segments[0])
{
    public override void Mutate(IRandom rng)
    {
        Value = segments[rng.Next(segments.Length)];
    }

    public override IGene Clone()
    {
        ArraySegmentGene seg = new ArraySegmentGene(Name, segments); //This sets Value, but we don't want that.
        seg.Value = Value; //We override value with the correct value here
        return seg;
    }
}
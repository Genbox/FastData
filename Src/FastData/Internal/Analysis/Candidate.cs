using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis;

internal sealed class Candidate(IStringHash stringHash, double fitness, int collisions)
{
    internal double Fitness { get; } = fitness;
    internal int Collisions { get; } = collisions;
    internal double Time { get; set; }
    internal IStringHash StringHash { get; } = stringHash;
}
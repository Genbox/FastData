using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis;

internal sealed class Candidate(IStringHash stringHash, double fitness, int collisions)
{
    internal double Fitness { get; } = fitness;
    internal int Collisions { get; } = collisions;
    internal IStringHash StringHash { get; } = stringHash;
    internal double Time { get; set; }
}
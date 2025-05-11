using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.Segments;
using Genbox.FastData.Specs.Hash;
using Genbox.FastData.Specs.Misc;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal class BruteForceAnalyzer(StringProperties props, BruteForceAnalyzerConfig _, Simulator simulator) : IHashAnalyzer<BruteForceStringHash>
{
    // This class brute-force all combinations of string segments with all available hash functions.
    // The best one is returned. Brute-force might be a viable solution if the number of combinations are small.

    public Candidate<BruteForceStringHash> Run()
    {
        Candidate<BruteForceStringHash> best = new Candidate<BruteForceStringHash>();

        foreach (StringSegment segment in SegmentManager.Generate(props))
        {
            BruteForceStringHash spec = new BruteForceStringHash(segment);

            Candidate<BruteForceStringHash> candidate = new Candidate<BruteForceStringHash>(spec);
            simulator.Run(ref candidate);

            if (candidate.Fitness > best.Fitness)
                best = candidate;
        }

        return best;
    }
}
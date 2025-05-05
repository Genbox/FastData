using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.Segments;
using Genbox.FastData.Specs.Hash;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal class BruteForceAnalyzer(StringProperties props, BruteForceAnalyzerConfig _, Simulator simulator) : IHashAnalyzer<BruteForceHashSpec>
{
    // This class brute-force all combinations of string segments with all available hash functions.
    // The best one is returned. Brute-force might be a viable solution if the number of combinations are small.

    public Candidate<BruteForceHashSpec> Run()
    {
        Candidate<BruteForceHashSpec> best = new Candidate<BruteForceHashSpec>();

        foreach (StringSegment segment in SegmentManager.Generate(props))
        {
            BruteForceHashSpec spec = new BruteForceHashSpec(segment);

            Candidate<BruteForceHashSpec> candidate = new Candidate<BruteForceHashSpec>(spec);
            simulator.Run(ref candidate);

            if (candidate.Fitness > best.Fitness)
                best = candidate;
        }

        return best;
    }
}
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis.BruteForce;

internal class BruteForceAnalyzer(object[] data, StringProperties props, BruteForceSettings settings, Simulation<BruteForceSettings, BruteForceHashSpec> simulation) : IHashAnalyzer<BruteForceHashSpec>
{
    /*
     * This class brute-force all combinations of string segments with all avaliable hash functions.
     * The best one is returned. Brute-force might be a viable solution if the number of combinations are small.
     */

    public Candidate<BruteForceHashSpec> Run()
    {
        Candidate<BruteForceHashSpec> best = new Candidate<BruteForceHashSpec>();
        HashFunction[] hashFunctions = Enum.GetValues(typeof(HashFunction)).Cast<HashFunction>().ToArray();

        foreach (StringSegment segment in SegmentManager.Generate(props))
        {
            foreach (HashFunction func in hashFunctions)
            {
                BruteForceHashSpec spec = new BruteForceHashSpec(func, [segment]);

                Candidate<BruteForceHashSpec> candidate = new Candidate<BruteForceHashSpec>(spec);
                simulation(data, settings, ref candidate);

                if (candidate.Fitness > best.Fitness)
                    best = candidate;
            }
        }

        return best;
    }
}
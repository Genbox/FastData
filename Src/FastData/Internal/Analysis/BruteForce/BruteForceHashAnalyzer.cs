using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis.BruteForce;

internal class BruteForceAnalyzer(string[] data, StringProperties props, BruteForceSettings settings) : IHashAnalyzer<BFHashSpec>
{
    /*
     * This class brute-force all combinations of string segments with all avaliable hash functions.
     * The best one is returned. Brute-force might be a viable solution if the number of combinations are small.
     */

    public Candidate<BFHashSpec> Run()
    {
        Candidate<BFHashSpec> best = new Candidate<BFHashSpec>();
        HashFunction[] hashFunctions = Enum.GetValues(typeof(HashFunction)).Cast<HashFunction>().ToArray();

        foreach (ISegmentGenerator generator in SegmentManager.GetGenerators())
        {
            foreach (StringSegment segment in generator.Generate(props))
            {
                foreach (HashFunction func in hashFunctions)
                {
                    BFHashSpec spec = new BFHashSpec(func, [segment]);

                    Candidate<BFHashSpec> candidate = new Candidate<BFHashSpec>(spec);
                    AnalysisHelper.RunSimulation(data, settings, ref candidate);

                    Console.WriteLine(candidate.Fitness.ToString("N5") + "\t" + func + "\t" + segment);

                    if (candidate.Fitness > best.Fitness)
                    {
                        Console.WriteLine("New best: " + candidate.Fitness.ToString("N5"));
                        best = candidate;
                    }
                }
            }
        }

        return best;
    }
}
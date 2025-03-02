using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Genetic.Selection;

/// <summary>
/// Rank Selection is a selection method in genetic algorithms where individuals are ranked by fitness rather than selected based on raw fitness values. The selection probability is determined by rank instead of absolute fitness, ensuring a fairer selection pressure and preventing dominance by a few extremely fit individuals.
/// </summary>
/// <param name="seed">RNG seed for deterministic simulations. Set to 0 for random seed.</param>
internal sealed class RankSelection(int seed = 0) : ISelection
{
    private readonly Random _random = seed != 0 ? new Random(seed) : new Random();

    public IEnumerable<int> Select(int generation, Candidate<GeneticHashSpec>[] population)
    {
        int[] indices = Enumerable.Range(0, population.Length).ToArray();
        Array.Sort(indices, (a, b) => population[b].Fitness.CompareTo(population[a].Fitness));

        float totalRank = population.Length * (population.Length + 1) / 2f;

        for (int i = 0; i < population.Length; i++)
        {
            double r = _random.NextDouble() * totalRank;
            float sum = 0;

            for (int j = 0; j < population.Length; j++)
            {
                sum += j + 1;
                if (sum >= r)
                {
                    yield return indices[j];
                    break;
                }
            }
        }
    }

    // public IEnumerable<int> Select2(int generation, Candidate<GeneticHashSpec>[] population)
    // {
    //     int[] indices = Enumerable.Range(0, population.Length).ToArray();
    //
    //     // ✅ Correct sorting: Higher fitness ranks first
    //     Array.Sort(indices, (a, b) => population[b].Fitness.CompareTo(population[a].Fitness));
    //
    //     // ✅ Precompute cumulative ranks (Avoid recalculating sum every time)
    //     float[] cumulativeRanks = new float[population.Length];
    //     cumulativeRanks[0] = 1;  // Rank 1 (highest fitness)
    //
    //     for (int i = 1; i < population.Length; i++)
    //         cumulativeRanks[i] = cumulativeRanks[i - 1] + (i + 1);  // Rank-based sum
    //
    //     float totalRank = cumulativeRanks[^1]; // Last element is total rank sum
    //
    //     Random rand = new();
    //
    //     for (int i = 0; i < population.Length; i++)
    //     {
    //         double r = rand.NextDouble() * totalRank;
    //
    //         // ✅ Efficient binary search instead of O(N) loop
    //         int selectedIndex = Array.BinarySearch(cumulativeRanks, (float)r);
    //         if (selectedIndex < 0)
    //             selectedIndex = ~selectedIndex; // Get correct insertion point
    //
    //         yield return indices[selectedIndex];
    //     }
    // }
}
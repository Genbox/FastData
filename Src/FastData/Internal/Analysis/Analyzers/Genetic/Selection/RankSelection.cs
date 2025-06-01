using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Helpers;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>Rank Selection is a selection method in genetic algorithms where individuals are ranked by fitness rather than selected based on raw fitness values. The selection probability is determined by rank instead of absolute fitness, ensuring a fairer selection pressure and preventing dominance by a few extremely fit individuals.</summary>
internal sealed class RankSelection(IRandom random) : ISelection
{
    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        int[] indices = GeneHelper.GetSortedByFitness(population);

        int count = population.Count;
        float totalRank = count * (count + 1) / 2f;

        double[] rankWheel = new double[count];
        double cumulative = 0.0;

        for (int i = 0; i < count; i++)
        {
            cumulative += (double)(count - i) / totalRank;
            rankWheel[i] = cumulative;
        }

        for (int i = 0; i < maxParents; i++)
        {
            double r = random.NextDouble();

            for (int j = 0; j < count; j++)
            {
                if (rankWheel[j] >= r)
                {
                    parents.Add(indices[j]);
                    break;
                }
            }
        }
    }
}
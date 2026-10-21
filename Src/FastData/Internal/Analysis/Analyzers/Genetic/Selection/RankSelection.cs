using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Helpers;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>
/// Rank Selection is a selection method in genetic algorithms where individuals are ranked by fitness rather than selected based on raw fitness values. The selection probability is determined by rank instead of absolute fitness, ensuring a fairer selection pressure and preventing dominance by
/// a few extremely fit individuals.
/// </summary>
internal sealed class RankSelection(IRandom random) : ISelection
{
    private double[] _rankWheel = [];

    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        int[] indices = GeneHelper.GetSortedByFitness(population);

        int count = population.Count;
        if (_rankWheel.Length < count)
            _rankWheel = new double[count];

        double totalRank = 0;

        for (int i = 0; i < count; i++)
        {
            totalRank += count - i;
            _rankWheel[i] = totalRank;
        }

        for (int i = 0; i < maxParents; i++)
        {
            int index = GeneHelper.FindSlice(_rankWheel, count, random.NextDouble() * totalRank);
            parents.Add(indices[index]);
        }
    }
}
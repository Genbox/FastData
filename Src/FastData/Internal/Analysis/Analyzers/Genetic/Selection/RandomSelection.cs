using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>Selects parents at random</summary>
/// <param name="avoidDuplicates">If set, there is a lower chance that duplicates will be returned</param>
internal sealed class RandomSelection(bool avoidDuplicates, IRandom random) : ISelection
{
    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        if (avoidDuplicates)
        {
            parents.AddRange(Enumerable.Range(0, population.Count)
                                       .OrderBy(_ => random.Next())
                                       .Take(maxParents));
        }
        else
        {
            for (int i = 0; i < maxParents; i++)
                parents.Add(random.Next(population.Count));
        }
    }
}
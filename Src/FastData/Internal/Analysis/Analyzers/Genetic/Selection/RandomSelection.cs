using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>Selects parents at random.</summary>
/// <param name="avoidDuplicates">Whether to lower the chance of selecting the same parent more than once.</param>
/// <param name="random">The random source used to choose parents.</param>
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
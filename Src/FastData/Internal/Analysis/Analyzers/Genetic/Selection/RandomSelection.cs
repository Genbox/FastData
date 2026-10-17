using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>Selects parents at random.</summary>
/// <param name="avoidDuplicates">Whether to lower the chance of selecting the same parent more than once.</param>
/// <param name="random">The random source used to choose parents.</param>
internal sealed class RandomSelection(bool avoidDuplicates, IRandom random) : ISelection
{
    private int[] _indices = [];

    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        if (avoidDuplicates)
        {
            int count = population.Count;
            if (_indices.Length < count)
                _indices = new int[count];

            for (int i = 0; i < count; i++)
                _indices[i] = i;

            int number = Math.Min(count, maxParents);
            for (int i = 0; i < number; i++)
            {
                // Partial Fisher-Yates shuffle: choose each remaining parent uniformly.
                int selected = i + random.Next(count - i);
                parents.Add(_indices[selected]);
                _indices[selected] = _indices[i];
            }
        }
        else
        {
            for (int i = 0; i < maxParents; i++)
                parents.Add(random.Next(population.Count));
        }
    }
}
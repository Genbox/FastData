using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Helpers;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>
/// Select from the top performers.
/// </summary>
internal sealed class EliteSelection : ISelection
{
    private readonly double _topPercent;

    /// <summary>
    /// Select from the top performers.
    /// </summary>
    /// <param name="topPercent">The percent to take. Must be 0 to 1</param>
    public EliteSelection(double topPercent)
    {
        if (topPercent is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(topPercent), "Must be between 0 and 1.");

        _topPercent = topPercent;
    }

    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        //Sort population by fitness
        int[] indices = GeneHelper.GetSortedByFitness(population);

        //We only want N percent
        int number = Math.Min((int)(population.Count * _topPercent), maxParents);

        for (int i = 0; i < number; i++)
            parents.Add(indices[i]);
    }
}
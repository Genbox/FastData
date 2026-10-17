using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Helpers;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Reinsertion;

internal class EliteReinsertion : IReinsertion
{
    private readonly double _topPercent;

    /// <summary>Reinserts the top-performing parents into the new generation.</summary>
    /// <param name="topPercent">The fraction of the configured population size to keep, from 0 to 1.</param>
    public EliteReinsertion(double topPercent)
    {
        if (topPercent is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(topPercent), "Must be between 0 and 1.");

        _topPercent = topPercent;
    }

    public void Process(StaticArray<Entity> population, StaticArray<Entity> newPopulation, int populationSize)
    {
        //We only want N percent. It is relative to the configured size, as the population grows when reinserted entities are added on top of the children.
        int number = Math.Min((int)(populationSize * _topPercent), population.Count);
        if (number == 0)
            return;

        int[] indices = GeneHelper.GetSortedByFitness(population);

        for (int i = 0; i < number; i++)
            newPopulation.Add(population[indices[i]]);
    }
}
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Helpers;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Reinsertion;

internal class EliteReinsertion : IReinsertion
{
    private readonly double _topPercent;

    /// <summary>Reinsert top performer parents into the new generation.</summary>
    /// <param name="topPercent">The percent to take. Must be 0 to 1</param>
    public EliteReinsertion(double topPercent)
    {
        if (topPercent is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(topPercent), "Must be between 0 and 1.");

        _topPercent = topPercent;
    }

    public void Process(StaticArray<Entity> population, StaticArray<Entity> newPopulation)
    {
        //Sort population by fitness
        int[] indices = GeneHelper.GetSortedByFitness(population);

        //We only want N percent
        int number = (int)(population.Count * _topPercent);

        for (int i = 0; i < number; i++)
        {
            newPopulation.Add(population[indices[i]]);
        }
    }
}
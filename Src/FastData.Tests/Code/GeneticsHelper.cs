using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Tests.Code;

internal static class GeneticsHelper
{
    internal static StaticArray<Entity> GeneratePopulation(int size, double minFitness, double maxFitness)
    {
        StaticArray<Entity> population = new StaticArray<Entity>(size);

        for (int i = 0; i < size; i++)
        {
            Entity entity = new Entity([]);
            entity.Fitness = Random.Shared.NextDouble() * (maxFitness - minFitness) + minFitness;
            population.Add(ref entity);
        }

        return population;
    }
}
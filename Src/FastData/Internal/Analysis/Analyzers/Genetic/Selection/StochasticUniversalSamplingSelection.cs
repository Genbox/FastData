using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Helpers;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>
/// Stochastic Universal Sampling (SUS) is a selection method that improves upon Roulette Wheel Selection by ensuring a more even distribution of selected individuals. Instead of selecting parents one-by-one probabilistically, SUS selects multiple individuals at fixed intervals along the
/// cumulative probability distribution.
/// </summary>
internal sealed class StochasticUniversalSamplingSelection(IRandom random) : ISelection
{
    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        double totalFitness = 0;
        for (int i = 0; i < population.Count; i++)
            totalFitness += GeneHelper.GetSelectionWeight(population[i].Fitness);

        //If no entity has any weight, they are all equally likely
        if (totalFitness <= 0)
        {
            for (int i = 0; i < maxParents; i++)
                parents.Add(random.Next(population.Count));

            return;
        }

        //The pointers are evenly spaced so that they cover the entire wheel
        double step = totalFitness / maxParents;
        double start = random.NextDouble() * step;

        double sum = 0;
        int index = 0;

        for (int i = 0; i < maxParents; i++)
        {
            double pointer = start + (i * step);

            //Move to the first entity whose cumulative weight exceeds the pointer. Do the int comparison first for perf
            while (index < population.Count && sum <= pointer)
                sum += GeneHelper.GetSelectionWeight(population[index++].Fitness);

            parents.Add(index - 1);
        }
    }
}
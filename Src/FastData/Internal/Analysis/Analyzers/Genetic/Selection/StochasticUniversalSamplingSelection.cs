using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>
/// Stochastic Universal Sampling (SUS) is a selection method that improves upon Roulette Wheel Selection by ensuring a more even distribution of selected individuals. Instead of selecting parents one-by-one probabilistically, SUS selects multiple individuals at fixed intervals along the cumulative probability distribution.
/// </summary>
internal sealed class StochasticUniversalSamplingSelection(IRandom random) : ISelection
{
    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        double totalFitness = population.Sum(c => c.Fitness);
        double step = totalFitness / population.Count;
        double start = random.NextDouble() * step;

        double sum = 0;
        int index = 0;

        for (int i = 0; i < maxParents; i++)
        {
            double pointer = start + (i * step);

            //Do the int comparison first for perf
            while (index < population.Count && sum < pointer)
                sum += population[index++].Fitness;

            parents.Add(index - 1);
        }
    }
}
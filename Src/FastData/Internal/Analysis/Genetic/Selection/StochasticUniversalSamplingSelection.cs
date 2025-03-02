using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Genetic.Selection;

/// <summary>
/// Stochastic Universal Sampling (SUS) is a selection method that improves upon Roulette Wheel Selection by ensuring a more even distribution of selected individuals. Instead of selecting parents one-by-one probabilistically, SUS selects multiple individuals at fixed intervals along the cumulative probability distribution.
/// </summary>
/// <param name="seed">RNG seed for deterministic simulations. Set to 0 for random seed.</param>
internal sealed class StochasticUniversalSamplingSelection(int seed = 0) : ISelection
{
    private readonly Random _random = seed != 0 ? new Random(seed) : new Random();

    public IEnumerable<int> Select(int generation, Candidate<GeneticHashSpec>[] population)
    {
        double totalFitness = population.Sum(c => c.Fitness);
        double step = totalFitness / population.Length;
        double start = _random.NextDouble() * step;

        double sum = 0;
        int index = 0;

        for (int i = 0; i < population.Length; i++)
        {
            double r = start + (i * step);

            while (sum < r && index < population.Length)
                sum += population[index++].Fitness;

            yield return index - 1;
        }
    }
}
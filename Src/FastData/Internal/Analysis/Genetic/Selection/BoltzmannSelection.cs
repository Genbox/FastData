using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Genetic.Selection;

/// <summary>
/// Boltzmann Selection is a selection method that dynamically adjusts selection pressure using a temperature parameter (T). Unlike traditional selection methods that fix selection probabilities based on fitness, Boltzmann Selection gradually shifts from exploration (randomness) to exploitation (favoring high-fitness individuals) over time.
/// It is inspired by simulated annealing, where higher temperatures allow more randomness, and lower temperatures favor the best solutions.
/// </summary>
/// <param name="temperature">The initial temperature</param>
/// <param name="seed">RNG seed for deterministic simulations. Set to 0 for random seed.</param>
internal sealed class BoltzmannSelection(double temperature, int seed = 0) : ISelection
{
    private readonly Random _random = seed != 0 ? new Random(seed) : new Random();

    public IEnumerable<int> Select(int generation, Candidate<GeneticHashSpec>[] population)
    {
        double maxFitness = population.Max(x => x.Fitness);
        double[] probabilities = population
                                 .Select(x => Math.Exp((x.Fitness - maxFitness) / temperature))
                                 .ToArray();

        double[] cumulative = new double[probabilities.Length];
        cumulative[0] = probabilities[0];

        // Precompute cumulative probabilities
        for (int i = 1; i < probabilities.Length; i++)
            cumulative[i] = cumulative[i - 1] + probabilities[i];

        double sum = cumulative[cumulative.Length - 1]; // Total probability sum

        for (int i = 0; i < population.Length; i++)
        {
            double r = _random.NextDouble() * sum;
            int index = Array.BinarySearch(cumulative, r);

            if (index < 0)
                index = ~index; // Get insertion point

            yield return index;
        }
    }
}
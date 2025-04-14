using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>
/// Boltzmann Selection is a method that dynamically adjusts selection pressure using a temperature parameter. Unlike traditional selection methods that fix selection probabilities based on fitness, Boltzmann Selection gradually shifts from exploration (randomness) to exploitation (favoring high-fitness individuals) over time.
/// It is inspired by simulated annealing, where higher temperatures allow more randomness, and lower temperatures favor the best solutions.
/// </summary>
internal sealed class BoltzmannSelection(double temperature, IRandom random) : ISelection
{
    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
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

        for (int i = 0; i < maxParents; i++)
        {
            double r = random.NextDouble() * sum;
            int index = Array.BinarySearch(cumulative, r);

            if (index < 0)
                index = ~index; // Get insertion point

            parents.Add(index);
        }
    }
}
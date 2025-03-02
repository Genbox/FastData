using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Genetic.Selection;

/// <summary>
/// Roulette Wheel Selection (also called Fitness-Proportionate Selection) is a probabilistic selection method in genetic algorithms where individuals are selected based on their fitness. Individuals with higher fitness values have a higher probability of being selected, similar to a weighted lottery.
/// </summary>
/// <param name="seed">RNG seed for deterministic simulations. Set to 0 for random seed.</param>
internal sealed class RouletteWheelSelection(int seed = 0) : ISelection
{
    private readonly Random _random = seed != 0 ? new Random(seed) : new Random();

    public IEnumerable<int> Select(int generation, Candidate<GeneticHashSpec>[] population)
    {
        //First we create a sum of all fitness
        double sum = population.Sum(p => p.Fitness);

        //Then we create the roulette wheel
        double[] wheel = new double[population.Length];

        double cumulativePercent = 0.0;
        for (int i = 0; i < population.Length; i++)
        {
            cumulativePercent += population[i].Fitness / sum;
            wheel[i] = cumulativePercent;
        }

        //Then we randomly select from the population
        for (int i = 0; i < population.Length; i++)
        {
            //Spin the wheel
            if (wheel[i] >= _random.NextDouble())
                yield return i;
        }
    }
}
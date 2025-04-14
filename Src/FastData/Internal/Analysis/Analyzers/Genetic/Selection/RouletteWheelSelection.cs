using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>
/// Roulette Wheel Selection (also called Fitness-Proportionate Selection) is a probabilistic selection method in genetic algorithms where individuals are selected based on their fitness. Individuals with higher fitness values have a higher probability of being selected, similar to a weighted lottery.
/// </summary>
internal sealed class RouletteWheelSelection(IRandom random) : ISelection
{
    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        //First we create a sum of all fitness
        double sum = population.Sum(p => p.Fitness);

        //Then we create the roulette wheel
        double[] wheel = new double[population.Count];

        double cumulative = 0.0;
        for (int i = 0; i < population.Count; i++)
        {
            cumulative += population[i].Fitness / sum;
            wheel[i] = cumulative;
        }

        //Then we randomly select from the population
        for (int i = 0; i < maxParents; i++)
        {
            //Spin the wheel
            if (wheel[i] >= random.NextDouble())
                parents.Add(i);
        }
    }
}
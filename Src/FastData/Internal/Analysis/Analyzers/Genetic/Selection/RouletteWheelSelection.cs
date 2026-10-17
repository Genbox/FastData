using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Helpers;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>
/// Roulette Wheel Selection (also called Fitness-Proportionate Selection) is a probabilistic selection method in genetic algorithms where individuals are selected based on their fitness. Individuals with higher fitness values have a higher probability of being selected, similar to a weighted
/// lottery.
/// </summary>
internal sealed class RouletteWheelSelection(IRandom random) : ISelection
{
    private double[] _wheel = [];

    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        //First we create the roulette wheel. Each entity gets a slice proportional to its fitness.
        int count = population.Count;
        if (_wheel.Length < count)
            _wheel = new double[count];

        double cumulative = 0.0;
        for (int i = 0; i < population.Count; i++)
        {
            cumulative += GeneHelper.GetSelectionWeight(population[i].Fitness);
            _wheel[i] = cumulative;
        }

        //Then we randomly select from the population
        for (int i = 0; i < maxParents; i++)
        {
            //If no entity has any weight, they are all equally likely
            if (cumulative <= 0)
            {
                parents.Add(random.Next(population.Count));
                continue;
            }

            //Spin the wheel
            parents.Add(GeneHelper.FindSlice(_wheel, count, random.NextDouble() * cumulative));
        }
    }
}
using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Genetic.Selection;

internal sealed class RouletteWheelSelection2 : ISelection
{
    public IEnumerable<int> Select(int generation, Candidate<GeneticHashSpec>[] population)
    {
        double totalFitness = population.Sum(c => c.Fitness);

        for (int i = 0; i < population.Length; i++)
        {
            double r = RandomHelper.NextDouble() * totalFitness;
            double sum = 0;

            for (ushort j = 0; j < population.Length; j++)
            {
                sum += population[j].Fitness;
                if (sum >= r)
                {
                    yield return j;
                    break;
                }
            }
        }
    }
}
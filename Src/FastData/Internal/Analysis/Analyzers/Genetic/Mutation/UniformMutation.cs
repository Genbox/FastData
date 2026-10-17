using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Mutation;

/// <summary>Mutates each gene independently with the configured probability.</summary>
/// <param name="mutationRate">The probability, from 0 to 1, that each gene mutates.</param>
/// <param name="random">The random source used to mutate genes.</param>
internal sealed class UniformMutation(double mutationRate, IRandom random) : IMutation
{
    public void Process(StaticArray<Entity> population)
    {
        for (int i = 0; i < population.Count; i++)
        {
            IGene[] genes = population[i].Genes;

            for (int j = 0; j < genes.Length; j++)
            {
                if (random.NextDouble() < mutationRate)
                    genes[j].Mutate(random);
            }
        }
    }
}
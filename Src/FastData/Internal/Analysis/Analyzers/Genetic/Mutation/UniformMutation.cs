using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Mutation;

/// <summary>Mutates genes with a low chance of mutation</summary>
/// <param name="mutationRate">A number between 0 and 1. 0 means don't mutate. 1 means always mutate.</param>
/// <param name="random">The rng to use</param>
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
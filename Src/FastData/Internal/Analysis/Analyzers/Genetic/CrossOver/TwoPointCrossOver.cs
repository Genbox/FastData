using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.CrossOver;

/// <summary>Swaps a all genes after a random point and up to another random point</summary>
internal sealed class TwoPointCrossOver(IRandom random) : ICrossOver
{
    public void Process(StaticArray<Entity> population, List<int> parents, StaticArray<Entity> newPopulation)
    {
        int geneLength = population[0].Genes.Length;

        for (int i = 0; i < parents.Count; i += 2)
        {
            IGene[] genesA = population[parents[i]].Genes;
            IGene[] genesB = population[parents[i + 1]].Genes;

            IGene[] child1 = new IGene[genesA.Length];
            IGene[] child2 = new IGene[genesB.Length];

            int point1 = random.Next(geneLength - 1);
            int point2 = random.Next(point1 + 1, geneLength);

            for (int j = 0; j < genesA.Length; j++)
            {
                if (j >= point1 && j < point2)
                {
                    child1[j] = genesB[j];
                    child2[j] = genesA[j];
                }
                else
                {
                    child1[j] = genesA[j];
                    child2[j] = genesB[j];
                }
            }

            newPopulation.Add(new Entity(child1));
            newPopulation.Add(new Entity(child2));
        }
    }
}
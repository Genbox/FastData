using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.CrossOver;

/// <summary>Swaps all genes between two random points.</summary>
internal sealed class TwoPointCrossOver(IRandom random) : ICrossOver
{
    public void Process(StaticArray<Entity> population, List<int> parents, StaticArray<Entity> newPopulation)
    {
        int geneLength = population[0].Genes.Length;

        for (int i = 0; i < parents.Count; i += 2)
        {
            IGene[] genesA = population[parents[i]].Genes;
            IGene[] genesB = population[parents[i + 1]].Genes;

            Entity firstChild = new Entity(genesA.Length);
            Entity secondChild = new Entity(genesB.Length);
            IGene[] child1 = firstChild.Genes;
            IGene[] child2 = secondChild.Genes;

            int point1 = random.Next(geneLength - 1);
            int point2 = random.Next(point1 + 1, geneLength);

            for (int j = 0; j < genesA.Length; j++)
            {
                if (j >= point1 && j < point2)
                {
                    child1[j] = genesB[j].Clone();
                    child2[j] = genesA[j].Clone();
                }
                else
                {
                    child1[j] = genesA[j].Clone();
                    child2[j] = genesB[j].Clone();
                }
            }

            newPopulation.Add(firstChild);
            newPopulation.Add(secondChild);
        }
    }
}
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.CrossOver;

/// <summary>Swaps all genes after a random point.</summary>
internal sealed class OnePointCrossOver(IRandom random) : ICrossOver
{
    public void Process(StaticArray<Entity> population, List<int> parents, StaticArray<Entity> newPopulation)
    {
        int geneLength = population[0].Genes.Length;

        for (int i = 0; i < parents.Count; i += 2)
        {
            IGene[] genesA = population[parents[i]].Genes;
            IGene[] genesB = population[parents[i + 1]].Genes;

            //Two parents produce two children for simplicity
            Entity firstChild = new Entity(genesA.Length);
            Entity secondChild = new Entity(genesB.Length);
            IGene[] child1 = firstChild.Genes;
            IGene[] child2 = secondChild.Genes;

            int swapPoint = random.Next(geneLength);

            for (int j = 0; j < swapPoint; j++)
            {
                child1[j] = genesA[j].Clone();
                child2[j] = genesB[j].Clone();
            }

            for (int j = swapPoint; j < genesA.Length; j++) //All entities have the same number of genes, so does not matter which length we use
            {
                child1[j] = genesB[j].Clone();
                child2[j] = genesA[j].Clone();
            }

            //Add the children
            newPopulation.Add(firstChild);
            newPopulation.Add(secondChild);
        }
    }
}
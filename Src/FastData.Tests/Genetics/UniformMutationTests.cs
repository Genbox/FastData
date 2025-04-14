using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Mutation;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Tests.Code;

namespace Genbox.FastData.Tests.Genetics;

public class UniformMutationTests
{
    [Fact]
    public void IsCorrect()
    {
        Queue<double> queue = new Queue<double>([0.05, 0.2, 0.95]);

        DelegatedRandom random = new DelegatedRandom(() => SharedRandom.Instance.Next(), () => queue.Dequeue()); // Only gene[0] < 0.1 threshold
        UniformMutation mutation = new UniformMutation(0.1, random);

        IGene[] originalGenes =
        [
            new IntGene("A", 1),
            new IntGene("B", 2),
            new IntGene("C", 3)
        ];

        Entity child = new Entity(originalGenes);
        StaticArray<Entity> newPopulation = new StaticArray<Entity>(1) { child };

        mutation.Process(newPopulation);

        Assert.NotEqual(1, ((IntGene)newPopulation[0].Genes[0]).Value); // mutated
        Assert.Equal(2, ((IntGene)newPopulation[0].Genes[1]).Value); // not mutated
        Assert.Equal(3, ((IntGene)newPopulation[0].Genes[2]).Value); // not mutated
    }
}
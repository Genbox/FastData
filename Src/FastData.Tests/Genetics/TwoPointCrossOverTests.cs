using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.CrossOver;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Tests.Code;

namespace Genbox.FastData.Tests.Genetics;

public class TwoPointCrossOverTests
{
    [Fact]
    public void IsCorrect()
    {
        StaticArray<Entity> population = new StaticArray<Entity>(2)
        {
            new Entity([
                new TestGene("1"),
                new TestGene("2"),
                new TestGene("3"),
                new TestGene("4"),
                new TestGene("5")
            ]),
            new Entity([
                new TestGene("a"),
                new TestGene("b"),
                new TestGene("c"),
                new TestGene("d"),
                new TestGene("e")
            ])
        };

        StaticArray<Entity> newPopulation = new StaticArray<Entity>(2);

        TwoPointCrossOver crossover = new TwoPointCrossOver(new FixedIntRandom([1, 4]));
        crossover.Process(population, [0, 1], newPopulation);

        Assert.Equal("1", newPopulation[0].Genes[0].Name);
        Assert.Equal("b", newPopulation[0].Genes[1].Name);
        Assert.Equal("c", newPopulation[0].Genes[2].Name);
        Assert.Equal("d", newPopulation[0].Genes[3].Name);
        Assert.Equal("5", newPopulation[0].Genes[4].Name);

        Assert.Equal("a", newPopulation[1].Genes[0].Name);
        Assert.Equal("2", newPopulation[1].Genes[1].Name);
        Assert.Equal("3", newPopulation[1].Genes[2].Name);
        Assert.Equal("4", newPopulation[1].Genes[3].Name);
        Assert.Equal("e", newPopulation[1].Genes[4].Name);
    }
}
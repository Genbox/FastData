using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.CrossOver;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Tests.Code;

namespace Genbox.FastData.Tests.Genetics;

public class OnePointCrossOverTests
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
            ]),
            new Entity([
                new TestGene("a"),
                new TestGene("b"),
                new TestGene("c"),
            ]),
        };

        StaticArray<Entity> newPopulation = new StaticArray<Entity>(2);

        OnePointCrossOver crossOver = new OnePointCrossOver(StaticRandom.Instance);
        crossOver.Process(population, [0, 1], newPopulation);

        Assert.Equal("1", newPopulation[0].Genes[0].Name);
        Assert.Equal("b", newPopulation[0].Genes[1].Name);
        Assert.Equal("c", newPopulation[0].Genes[2].Name);

        Assert.Equal("a", newPopulation[1].Genes[0].Name);
        Assert.Equal("2", newPopulation[1].Genes[1].Name);
        Assert.Equal("3", newPopulation[1].Genes[2].Name);
    }
}
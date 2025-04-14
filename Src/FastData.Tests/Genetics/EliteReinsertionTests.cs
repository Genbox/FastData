using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Reinsertion;

namespace Genbox.FastData.Tests.Genetics;

public class EliteReinsertionTests
{
    [Fact]
    public void IsCorrect()
    {
        StaticArray<Entity> population = new StaticArray<Entity>(4)
        {
            new Entity([]),
            new Entity([]),
            new Entity([]),
            new Entity([])
        };

        StaticArray<Entity> newPopulation = new StaticArray<Entity>(2);
        EliteReinsertion reinsertion = new EliteReinsertion(0.5); // Top 50% -> 2 entities
        reinsertion.Process(population, newPopulation);

        Assert.Equal(2, newPopulation.Count);
    }
}
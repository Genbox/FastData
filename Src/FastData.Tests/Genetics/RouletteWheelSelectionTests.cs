using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Tests.Genetics;

public class RouletteWheelSelectionTests
{
    [Fact]
    public void SelectsByFitnessNotPosition()
    {
        StaticArray<Entity> population = new StaticArray<Entity>(3)
        {
            new Entity([]) { Fitness = 0.9 },
            new Entity([]) { Fitness = 0.05 },
            new Entity([]) { Fitness = 0.05 }
        };

        RouletteWheelSelection selection = new RouletteWheelSelection(new DefaultRandom(42));
        int[] counter = new int[population.Count];

        for (int i = 0; i < 1000; i++)
        {
            List<int> parents = new List<int>();
            selection.Process(population, parents, 3);

            Assert.Equal(3, parents.Count);

            foreach (int parent in parents)
                counter[parent]++;
        }

        Assert.True(counter[0] > counter[1] + counter[2]);
    }

    [Fact]
    public void SelectsEntitiesBeyondMaxParents()
    {
        StaticArray<Entity> population = new StaticArray<Entity>(4)
        {
            new Entity([]) { Fitness = 0.01 },
            new Entity([]) { Fitness = 0.01 },
            new Entity([]) { Fitness = 0.01 },
            new Entity([]) { Fitness = 100 }
        };

        List<int> parents = new List<int>();
        new RouletteWheelSelection(new DefaultRandom(42)).Process(population, parents, 2);

        Assert.Contains(3, parents);
    }
}
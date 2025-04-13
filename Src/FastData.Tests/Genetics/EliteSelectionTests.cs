using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

namespace Genbox.FastData.Tests.Genetics;

public class EliteSelectionTests
{
    [Fact]
    public void IsCorrect()
    {
        StaticArray<Entity> population = new StaticArray<Entity>(10)
        {
            new Entity([]) { Fitness = 0 },
            new Entity([]) { Fitness = 0.1 },
            new Entity([]) { Fitness = 0.2 },
            new Entity([]) { Fitness = 0.3 },
            new Entity([]) { Fitness = 0.4 },
            new Entity([]) { Fitness = 0.5 },
            new Entity([]) { Fitness = 0.6 },
            new Entity([]) { Fitness = 0.7 },
            new Entity([]) { Fitness = 0.8 },
            new Entity([]) { Fitness = 0.9 }
        };

        //We want it to select the 20% best from population
        EliteSelection selection = new EliteSelection(0.2);

        List<int> selected = new List<int>();
        selection.Process(population, selected, 10);

        Assert.Equal(2, selected.Count);
        Assert.Equal(0.9, population[selected[0]].Fitness);
        Assert.Equal(0.8, population[selected[1]].Fitness);
    }
}
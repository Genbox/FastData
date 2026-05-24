using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Tests.Code;

namespace Genbox.FastData.Tests.Genetics;

public class RouletteWheelSelectionTests
{
    [Fact]
    public void SelectsRequestedParentsFromCumulativeFitness()
    {
        Queue<double> doubles = new Queue<double>([0.05, 0.2, 0.7, 0.99]);
        DelegatedRandom random = new DelegatedRandom(() => 0, () => doubles.Dequeue());
        RouletteWheelSelection selection = new RouletteWheelSelection(random);
        StaticArray<Entity> population = new StaticArray<Entity>(3)
        {
            new Entity([]) { Fitness = 0.1 },
            new Entity([]) { Fitness = 0.3 },
            new Entity([]) { Fitness = 0.6 }
        };

        List<int> selected = new List<int>();
        selection.Process(population, selected, 4);

        Assert.Equal([0, 1, 2, 2], selected);
    }

    [Fact]
    public void FallsBackToRandomSelectionWhenFitnessHasNoWeight()
    {
        Queue<int> ints = new Queue<int>([2, 1, 0]);
        DelegatedRandom random = new DelegatedRandom(() => ints.Dequeue(), () => 0);
        RouletteWheelSelection selection = new RouletteWheelSelection(random);
        StaticArray<Entity> population = new StaticArray<Entity>(3)
        {
            new Entity([]),
            new Entity([]),
            new Entity([])
        };

        List<int> selected = new List<int>();
        selection.Process(population, selected, 3);

        Assert.Equal([2, 1, 0], selected);
    }
}
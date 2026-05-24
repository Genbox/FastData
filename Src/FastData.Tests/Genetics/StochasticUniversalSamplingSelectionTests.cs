using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Tests.Code;

namespace Genbox.FastData.Tests.Genetics;

public class StochasticUniversalSamplingSelectionTests
{
    [Fact]
    public void SelectsValidIndexesWhenStartPointerIsZero()
    {
        DelegatedRandom random = new DelegatedRandom(() => 0, () => 0);
        StochasticUniversalSamplingSelection selection = new StochasticUniversalSamplingSelection(random);
        StaticArray<Entity> population = new StaticArray<Entity>(3)
        {
            new Entity([]) { Fitness = 1 },
            new Entity([]) { Fitness = 3 },
            new Entity([]) { Fitness = 6 }
        };

        List<int> selected = new List<int>();
        selection.Process(population, selected, 5);

        Assert.Equal([0, 1, 1, 2, 2], selected);
    }

    [Fact]
    public void FallsBackToRandomSelectionWhenFitnessHasNoWeight()
    {
        Queue<int> ints = new Queue<int>([2, 1, 0]);
        DelegatedRandom random = new DelegatedRandom(() => ints.Dequeue(), () => 0);
        StochasticUniversalSamplingSelection selection = new StochasticUniversalSamplingSelection(random);
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
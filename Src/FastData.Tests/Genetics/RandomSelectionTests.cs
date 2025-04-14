using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Tests.Code;

namespace Genbox.FastData.Tests.Genetics;

public class RandomSelectionTests
{
    [Theory]
    [InlineData(true, new[] { 0, 1, 2 })]
    [InlineData(false, new[] { 1, 1, 1 })]
    public void IsCorrect(bool avoidDuplicates, int[] expected)
    {
        StaticArray<Entity> population = new StaticArray<Entity>(3)
        {
            new Entity([]) { Fitness = 0 },
            new Entity([]) { Fitness = 0.1 },
            new Entity([]) { Fitness = 0.2 }
        };

        RandomSelection selection = new RandomSelection(avoidDuplicates, StaticRandom.Instance);

        List<int> selected = new List<int>();
        selection.Process(population, selected, 3);

        //It should select all 3
        Assert.Equal(3, selected.Count);
        Assert.Equal(expected[0], selected[0]);
        Assert.Equal(expected[1], selected[1]);
        Assert.Equal(expected[2], selected[2]);
    }
}
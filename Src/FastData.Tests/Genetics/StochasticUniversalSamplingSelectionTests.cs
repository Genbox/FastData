using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Tests.Code;

namespace Genbox.FastData.Tests.Genetics;

public class StochasticUniversalSamplingSelectionTests
{
    [Fact]
    public void SelectsFirstEntityWhenStartIsZero()
    {
        StaticArray<Entity> population = GeneticsHelper.GeneratePopulation(4, 1, 1);

        List<int> parents = new List<int>();
        new StochasticUniversalSamplingSelection(new DelegatedRandom(() => 0, () => 0)).Process(population, parents, 4);

        Assert.Equal([0, 1, 2, 3], parents);
    }

    [Fact]
    public void PointersCoverEntirePopulation()
    {
        StaticArray<Entity> population = GeneticsHelper.GeneratePopulation(10, 1, 1);

        //Total fitness is 10 and we select 5, so the step is 2 and the start is 0.5 * 2 = 1
        List<int> parents = new List<int>();
        new StochasticUniversalSamplingSelection(new DelegatedRandom(() => 0, () => 0.5)).Process(population, parents, 5);

        Assert.Equal([1, 3, 5, 7, 9], parents);
    }
}
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Tests.Code;

namespace Genbox.FastData.Tests.Genetics;

public class GenericSelectionTests
{
    [Theory]
    [MemberData(nameof(GetSelectionsWithRandom))]
    public void ShouldSelectHighestFitness(object selection)
    {
        StaticArray<Entity> population = new StaticArray<Entity>(3)
        {
            new Entity([]) { Fitness = 0.1 },
            new Entity([]) { Fitness = 0.5 },
            new Entity([]) { Fitness = 0.9 }
        };

        //We run the selection process 100 times to accumulate the selection pattern
        List<int> selected = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            ((ISelection)selection).Process(population, selected, 3);
        }

        //Setup the counters, starting with a value of 0
        Dictionary<int, int> counter = new Dictionary<int, int>
        {
            { 0, 0 },
            { 1, 0 },
            { 2, 0 }
        };

        foreach (int i in selected)
        {
            counter[i]++;
        }

        //There should only be 3 groups: 0, 1 and 2, and c0 < c1 < c2 must be true
        Assert.Equal(3, counter.Count);
        Assert.True(counter[0] < counter[1]);
        Assert.True(counter[1] < counter[2]);
    }

    [Theory]
    [MemberData(nameof(GetAllSelections))]
    public void AllShouldReturnValidIndexes(object selection)
    {
        StaticArray<Entity> population = GeneticsHelper.GeneratePopulation(10, 10, 100);

        List<int> selectedIndexes = new List<int>();
        ((ISelection)selection).Process(population, selectedIndexes, 10);

        Assert.True(selectedIndexes.Count >= 1, "We expect it to return at least one element");
        Assert.All(selectedIndexes, index => Assert.InRange(index, 0, population.Count - 1));
    }

    [Theory]
    [MemberData(nameof(GetSeededSelections))]
    public void ShouldBeDeterministicWithSameSeed(object a, object b) => TestSeed((ISelection)a, (ISelection)b, true);

    [Theory]
    [MemberData(nameof(GetSeededDiffSelections))]
    public void ShouldProduceDifferentResultsWithDifferentSeeds(object a, object b) => TestSeed((ISelection)a, (ISelection)b, false);

    private static void TestSeed(ISelection a, ISelection b, bool equal)
    {
        StaticArray<Entity> population = GeneticsHelper.GeneratePopulation(10, 0, 10);

        List<int> indexes1 = new List<int>();
        a.Process(population, indexes1, 10);

        List<int> indexes2 = new List<int>();
        b.Process(population, indexes2, 10);

        if (equal)
            Assert.Equal(indexes1, indexes2);
        else
            Assert.NotEqual(indexes1, indexes2);
    }

    public static TheoryData<object> GetAllSelections() =>
    [
        new BoltzmannSelection(10, SharedRandom.Instance),
        new EliteSelection(0.5),
        new RandomSelection(false, SharedRandom.Instance),
        new RandomSelection(true, SharedRandom.Instance),
        new RankSelection(SharedRandom.Instance),
        new RouletteWheelSelection(SharedRandom.Instance),
        new StochasticUniversalSamplingSelection(SharedRandom.Instance),
        new TournamentSelection(4, SharedRandom.Instance)
    ];

    public static TheoryData<object, object> GetSeededSelections()
    {
        DefaultRandom rng1 = new DefaultRandom(42);
        DefaultRandom rng2 = new DefaultRandom(42);

        return new TheoryData<object, object>
        {
            { new BoltzmannSelection(10, rng1), new BoltzmannSelection(10, rng2) },
            { new RandomSelection(false, rng1), new RandomSelection(false, rng2) },
            { new RandomSelection(true, rng1), new RandomSelection(true, rng2) },
            { new RankSelection(rng1), new RankSelection(rng2) },
            { new RouletteWheelSelection(rng1), new RouletteWheelSelection(rng2) },
            { new StochasticUniversalSamplingSelection(rng1), new StochasticUniversalSamplingSelection(rng2) },
            { new TournamentSelection(4, rng1), new TournamentSelection(4, rng2) }
        };
    }

    public static TheoryData<object, object> GetSeededDiffSelections()
    {
        DefaultRandom rng1 = new DefaultRandom(42);
        DefaultRandom rng2 = new DefaultRandom(99);

        return new TheoryData<object, object>
        {
            { new BoltzmannSelection(10, rng1), new BoltzmannSelection(10, rng2) },
            { new RandomSelection(false, rng1), new RandomSelection(false, rng2) },
            { new RandomSelection(true, rng1), new RandomSelection(true, rng2) },
            { new RankSelection(rng1), new RankSelection(rng2) },
            { new RouletteWheelSelection(rng1), new RouletteWheelSelection(rng2) },
            { new StochasticUniversalSamplingSelection(rng1), new StochasticUniversalSamplingSelection(rng2) },
            { new TournamentSelection(4, rng1), new TournamentSelection(4, rng2) }
        };
    }

    public static TheoryData<object> GetSelectionsWithRandom() =>
    [
        new BoltzmannSelection(1, SharedRandom.Instance),
        new RankSelection(SharedRandom.Instance),
        new RouletteWheelSelection(SharedRandom.Instance),
        new StochasticUniversalSamplingSelection(SharedRandom.Instance),
        new TournamentSelection(4, SharedRandom.Instance)
    ];
}
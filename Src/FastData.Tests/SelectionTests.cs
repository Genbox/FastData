using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Genetic.Selection;
using Genbox.FastData.Tests.Code;

namespace Genbox.FastData.Tests;

public class SelectionTests
{
    [Theory]
    [MemberData(nameof(GetAllSelections))]
    public void AllShouldReturnValidIndexes(object obj)
    {
        Candidate<GeneticHashSpec>[] population = GeneticHelper.GeneratePopulation(10, 10, 100);

        List<int> selectedIndexes = ((ISelection)obj).Select(0, population).ToList();

        Assert.True(selectedIndexes.Count >= 1, "We expect it to return at least one element");
        Assert.All(selectedIndexes, index => Assert.InRange(index, 0, population.Length - 1));
    }

    [Theory]
    [MemberData(nameof(GetSeededSelections))]
    public void ShouldBeDeterministicWithSameSeed(object obj1, object obj2)
    {
        var population = GeneticHelper.GeneratePopulation(10, 0, 10);

        var indexes1 = ((ISelection)obj1).Select(0, population);
        var indexes2 = ((ISelection)obj2).Select(0, population);

        Assert.Equal(indexes1, indexes2);
    }

    [Theory]
    [MemberData(nameof(GetSeededDiffSelections))]
    public void ShouldProduceDifferentResultsWithDifferentSeeds(object obj1, object obj2)
    {
        var population = GeneticHelper.GeneratePopulation(10, 0, 10);

        var indexes1 = ((ISelection)obj1).Select(0, population);
        var indexes2 = ((ISelection)obj2).Select(0, population);

        Assert.NotEqual(indexes1, indexes2);
    }

    [Theory]
    [MemberData(nameof(GetBiasedSelections))]
    public void ShouldFavorHigherFitness(object obj)
    {
        Candidate<GeneticHashSpec>[] population =
        [
            new Candidate<GeneticHashSpec> { Fitness = 10 },
            new Candidate<GeneticHashSpec> { Fitness = 50 },
            new Candidate<GeneticHashSpec> { Fitness = 100 }
        ];

        List<int> selectedIndexes = ((ISelection)obj).Select(0, population).ToList();

        int highestFitnessIndex = Array.IndexOf(population, population.OrderByDescending(c => c.Fitness).First());
        int highestSelectedCount = selectedIndexes.Count(i => i == highestFitnessIndex);

        Assert.True(highestSelectedCount > 0, "The highest fitness/rank candidate should be selected at least once.");
    }

    public static TheoryData<object> GetAllSelections() =>
    [
        new BoltzmannSelection(10),
        new RandomSelection(false),
        new RandomSelection(true),
        new RankSelection(),
        new RouletteWheelSelection(),
        new StochasticUniversalSamplingSelection(),
        new TournamentSelection(4),
        new TruncationSelection(0.5)
    ];

    public static TheoryData<object, object> GetSeededSelections() => new TheoryData<object, object>
    {
        { new BoltzmannSelection(10, 42), new BoltzmannSelection(10, 42) },
        { new RandomSelection(false, 42), new RandomSelection(false, 42) },
        { new RandomSelection(true, 42), new RandomSelection(true, 42) },
        { new RankSelection(42), new RankSelection(42) },
        { new RouletteWheelSelection(42), new RouletteWheelSelection(42) },
        { new StochasticUniversalSamplingSelection(42), new StochasticUniversalSamplingSelection(42) },
        { new TournamentSelection(4, 42), new TournamentSelection(4, 42) },
    };

    public static TheoryData<object, object> GetSeededDiffSelections() => new TheoryData<object, object>
    {
        { new BoltzmannSelection(10, 42), new BoltzmannSelection(10, 99) },
        { new RandomSelection(false, 42), new RandomSelection(false, 99) },
        { new RandomSelection(true, 42), new RandomSelection(true, 99) },
        { new RankSelection(42), new RankSelection(99) },
        { new RouletteWheelSelection(42), new RouletteWheelSelection(99) },
        { new StochasticUniversalSamplingSelection(42), new StochasticUniversalSamplingSelection(99) },
        { new TournamentSelection(4, 42), new TournamentSelection(4, 99) },
    };

    public static TheoryData<object> GetBiasedSelections() =>
    [
        new BoltzmannSelection(10),
        new RankSelection(),
        new RouletteWheelSelection(),
        new StochasticUniversalSamplingSelection(),
        new TournamentSelection(4)
    ];
}
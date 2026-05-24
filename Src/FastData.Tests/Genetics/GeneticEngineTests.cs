using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.CrossOver;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Mutation;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Reinsertion;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Tests.Code;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData.Tests.Genetics;

public class GeneticEngineTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void RejectsInvalidPopulationSize(int populationSize)
    {
        GeneticEngineConfig config = new GeneticEngineConfig { PopulationSize = populationSize };
        GeneticEngine engine = new GeneticEngine(config, [new TestGene("A")], NullLogger.Instance);
        DefaultRandom random = new DefaultRandom(42);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => engine.Evolve(
            ReadOnlySpan<string>.Empty,
            static (ReadOnlySpan<string> _, ref Entity entity) => entity.Fitness = 1,
            new TournamentSelection(1, random),
            new OnePointCrossOver(random),
            new UniformMutation(0, random),
            new EliteReinsertion(0),
            new MaxGenerationsTermination(1),
            random));

        Assert.Equal("PopulationSize", ex.ParamName);
    }

    [Fact]
    public void MaxGenerationsCountsEvaluatedGenerations()
    {
        const int populationSize = 2;
        const int maxGenerations = 3;

        GeneticEngineConfig config = new GeneticEngineConfig { PopulationSize = populationSize };
        GeneticEngine engine = new GeneticEngine(config, [new IntGene("A", 0, 0, 1)], NullLogger.Instance);
        DefaultRandom random = new DefaultRandom(42);
        int simulations = 0;

        engine.Evolve(
            ReadOnlySpan<string>.Empty,
            (ReadOnlySpan<string> _, ref Entity entity) =>
            {
                simulations++;
                entity.Fitness = 1;
                entity.Tag = 0;
            },
            new TournamentSelection(1, random),
            new OnePointCrossOver(random),
            new UniformMutation(0, random),
            new EliteReinsertion(0),
            new MaxGenerationsTermination(maxGenerations),
            random);

        Assert.Equal(populationSize * maxGenerations, simulations);
    }

    [Fact]
    public void StaticArrayEnumeratesOnlyAddedItems()
    {
        StaticArray<Entity> population = new StaticArray<Entity>(4)
        {
            new Entity([]) { Fitness = 1 },
            new Entity([]) { Fitness = 2 }
        };

        Assert.Equal([1, 2], population.Select(static entity => entity.Fitness));
    }
}
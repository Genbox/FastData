using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.CrossOver;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Mutation;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Reinsertion;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;
using Genbox.FastData.Internal.Misc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData.Tests.Genetics;

public class GeneticEngineTests
{
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
            (_, ref entity) =>
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
    public void EvolveReturnsDistinctEntities()
    {
        //There are only 3 possible entities, and the best is carried over by reinsertion each generation
        Entity[] result = Evolve(10, 10, new IntGene("A", 0, 0, 3), new EliteReinsertion(0.1));

        Assert.Distinct(result.Select(static entity => ((IntGene)entity.Genes[0]).Value));
    }

    [Fact]
    public void EvolveReturnsBestEntitiesOfWholeGeneration()
    {
        Entity[] result = Evolve(32, 1, new IntGene("A", 0, 0, 1_000_000), new EliteReinsertion(0.1));

        Assert.Equal(new GeneticEngineConfig().MaxReturned, result.Length);
    }

    [Fact]
    public void EvolveWithFullEliteReinsertionDoesNotOverflow()
    {
        Entity[] result = Evolve(10, 20, new IntGene("A", 0, 0, 1_000_000), new EliteReinsertion(1));

        Assert.NotEmpty(result);
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

    private static Entity[] Evolve(int populationSize, int maxGenerations, IntGene gene, EliteReinsertion reinsertion)
    {
        GeneticEngineConfig config = new GeneticEngineConfig { PopulationSize = populationSize };
        GeneticEngine engine = new GeneticEngine(config, [gene], NullLogger.Instance);
        DefaultRandom random = new DefaultRandom(42);

        return engine.Evolve(
            ReadOnlySpan<string>.Empty,
            static (_, ref entity) => entity.Fitness = ((IntGene)entity.Genes[0]).Value,
            new TournamentSelection(4, random),
            new OnePointCrossOver(random),
            new UniformMutation(0.05, random),
            reinsertion,
            new MaxGenerationsTermination(maxGenerations),
            random).ToArray();
    }
}
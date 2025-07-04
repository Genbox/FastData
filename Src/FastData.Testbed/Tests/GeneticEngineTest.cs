using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.CrossOver;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Mutation;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Reinsertion;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;
using Genbox.FastData.Internal.Misc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData.Testbed.Tests;

internal static class GeneticEngineTest
{
    internal static void Run()
    {
        GeneticEngine engine = new GeneticEngine(new GeneticEngineConfig(), [
            new IntGene("test", -1, -1000, 1000),
            new IntGene("test2", 42, -1000, 1000),
            new IntGene("test2", 99, -1000, 1000)
        ], NullLogger.Instance);

        DefaultRandom random = new DefaultRandom();

        Entity entity = engine.Evolve<string>([], Simulation,
            new TournamentSelection(4, random),
            new OnePointCrossOver(random),
            new UniformMutation(0.05, random),
            new EliteReinsertion(0.2),
            new FitnessThresholdTermination(1),
            random).First();

        Console.WriteLine("Result: " + entity.Fitness);
        Console.WriteLine("Gene0: " + ((IntGene)entity.Genes[0]).Value);
        Console.WriteLine("Gene1: " + ((IntGene)entity.Genes[1]).Value);
        Console.WriteLine("Gene2: " + ((IntGene)entity.Genes[2]).Value);
    }

    private static void Simulation(ReadOnlySpan<string> data, ref Entity entity)
    {
        unchecked
        {
            //We want the total value of all genes to be 100
            int target = 100;
            target += ((IntGene)entity.Genes[0]).Value;
            target *= ((IntGene)entity.Genes[1]).Value;
            target -= ((IntGene)entity.Genes[2]).Value;

            entity.Fitness = 1f / (1f + Math.Abs(target));
        }
    }
}
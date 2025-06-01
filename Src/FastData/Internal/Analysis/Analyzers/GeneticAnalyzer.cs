using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.CrossOver;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Mutation;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Reinsertion;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.SegmentGenerators;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.StringHash;
using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal sealed partial class GeneticAnalyzer(StringProperties props, GeneticAnalyzerConfig config, Simulator sim, ILogger<GeneticAnalyzer> logger) : IStringHashAnalyzer
{
    /*
     This is a genetic algorithm that determines the best configuration from a random population, that via evolution is biased
     to converge into a better configuration (if possible).

      ### General structure ###
      We formalize the properties into a specification. This spec is then sent to a generator that builds it. The generated function is then sent for evaluation.
      Doing it this way gives us a clear separation between model and code, which simplifies the model system considerably.

         1. We have the data
         2. Analyze it to get properties like min/max lengths, entropy map, etc.
         3. We hot-start the system by selecting a likely optimum from the very beginning:
            - We start with identity hashing and increase in hash strength for each evolution
            - We use the entropy map in range [0..MinLen] to start hashing at offsets with the highest entropy
            - We avoid places where the difference map says there are identical characters
         4. We create a population of N specs, each (except the identity spec) with a slight mutation
         5. Each hash function candidate is sent to the simulator for evaluation.
         6. Fitness is calculated as fill-rate/collisions and perhaps equidistribution
         7. We select the elite function (highest fitness score) and repeat the steps from point 4.
         8. We do this repetition until we no longer get an improvement to the fitness or we reached the maximum number of evolutions we want

         Notes:
         - We don't have to formalize substring chunk locality as it likely will be expressed in the timings automatically.
         - The number of segments is also expressed in the timings. It is an optimization problem between speed and collisions, so we don't have to account for it.

      ### Hash function ###
      We use a hash table simulation that takes in a hasher. The hasher has the following properties:
      - Complexity: Weak hashing gives us good perf, but might not be enough for some datasets, so we need stronger
                    mixing. Random input data will have high entropy, and we can use an identity function.
         # Scale:
              - Low: Identity hash
              - High: Strong mixing

         # Weight: It is likely that a simpler hash function is better for perf than a shorter substring due to CPU prefetching.
                   As such, I would say this factor is heavy.

      - Sub-length: The default hashing in most runtimes is to mix the entirety of input strings. Even if they are thousands of characters.
                  That is likely not needed to create a hash with high enough entropy. We therefore try with a shorter string.
                  However, it is important to note that we want to avoid branching in the hash function selection, so the hash function
                  needs to take the shortest string into account. We could split data into different lengths here (short vs. long) depending
                  on the number of strings in each set.

                  We would want more than one subsegment for long strings. Let's say there are long stretches of zero-entropy, and small chunks of high
                  entropy. In that case, we want a hash function that hits the small chunks only. That is, we want multiple chunks.
                  It would also be better for cache locality if the chunks are closer than further away from each other.
         # Scale:
             - Low: The minimum length string in the data set
             - High: The longest string in the set, or some maximum factor if there are memory/perf constraints

         # Weight: For very longs strings it matters more than short strings. Weight therefore depends on the number of long strings.

      ### Fitness function ###
      The hash function is only responsible for getting an integer from strings with the following preferences:
      - High (enough) entropy
      - Fast conversion

      Once we have an integer, it needs to be wrapped in the keyspace (mod) and hopefully not cluster around a bias in the data. We would
      want to distribute the keys in the keyspace such that we have as few collisions as possible. This can be expressed as a fill-rate.

      Fitness is therefore calculated as the number of collisions or the fill-rate, which is highly dependant on the hash function and the number
      of extra buckets (overhead) that we are willing to tolerate.
  */

    public bool IsAppropriate() => true;

    public IEnumerable<Candidate> GetCandidates()
    {
        GeneticEngineConfig cfg = new GeneticEngineConfig();
        cfg.PopulationSize = config.PopulationSize;
        cfg.ShuffleParents = config.ShuffleParents;

        ArraySegment[] segments = SegmentManager.Generate(props).ToArray();
        LogSegmentCount(logger, segments.Length);

        GeneticEngine engine = new GeneticEngine(cfg, [
            new ArraySegmentGene(nameof(GeneticStringHash.Segment), segments),
            new IntGene(nameof(GeneticStringHash.MixerSeed), 1, 0, 100),
            new IntGene(nameof(GeneticStringHash.MixerIterations), 1, 0, 3),
            new IntGene(nameof(GeneticStringHash.AvalancheSeed), 1, 0, 100),
            new IntGene(nameof(GeneticStringHash.AvalancheIterations), 1, 0, 3)
        ], logger);

        DefaultRandom random = new DefaultRandom(config.RandomSeed);

        ISelection selection = new TournamentSelection(4, random);
        ICrossOver crossOver = new OnePointCrossOver(random);
        IMutation mutation = new UniformMutation(0.05, random);
        IReinsertion reinsertion = new EliteReinsertion(0.1);
        ITermination termination = new MaxGenerationsTermination(config.MaxGenerations);

        foreach (Entity entity in engine.Evolve(Simulation, selection, crossOver, mutation, reinsertion, termination, random))
        {
            Entity localEntity = entity;
            GeneticStringHash hash = CopyGenes(ref localEntity);

            yield return new Candidate(hash, entity.Fitness, (int)entity.Tag);
        }
    }

    private void Simulation(ref Entity entity)
    {
        //Convert entity to GeneticArrayHash
        GeneticStringHash spec = CopyGenes(ref entity);

        //Run the simulation
        Candidate candidate = sim.Run(spec, () => FitnessHelper.CalculateFitness(props, spec.Segment, spec.GetExpression()));

        //Copy over the fitness value
        entity.Fitness = candidate.Fitness;
        entity.Tag = candidate.Collisions; //We cache collisions here and pull it out later
    }

    private static GeneticStringHash CopyGenes(ref Entity entity)
    {
        return new GeneticStringHash(
            ((ArraySegmentGene)entity.Genes[0]).Value,
            ((IntGene)entity.Genes[1]).Value,
            ((IntGene)entity.Genes[2]).Value,
            ((IntGene)entity.Genes[3]).Value,
            ((IntGene)entity.Genes[4]).Value
        );
    }
}
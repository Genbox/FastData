using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis.Genetic;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
internal sealed class GeneticAnalyzer(object[] data, StringProperties props, GeneticAnalyzerConfig analyzerConfig, Simulation<GeneticAnalyzerConfig, GeneticHashSpec> simulation) : IHashAnalyzer<GeneticHashSpec>
{
    private readonly StringSegment[] _segments = SegmentManager.Generate(props).ToArray();
    private static readonly Random _rng = new Random();

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

    public Candidate<GeneticHashSpec> Run()
    {
        // Create the initial population
        Candidate<GeneticHashSpec>[] population = new Candidate<GeneticHashSpec>[analyzerConfig.PopulationSize];
        for (int i = 0; i < population.Length; i++)
        {
            GeneticHashSpec spec = new GeneticHashSpec();
            MutateSpec(ref spec);
            population[i] = new Candidate<GeneticHashSpec>(spec);
        }

        int evolution = 0;
        Candidate<GeneticHashSpec> top1 = new Candidate<GeneticHashSpec> { Fitness = double.MinValue };
        double[] recent = new double[analyzerConfig.StagnantTopResults];

        while (evolution++ < analyzerConfig.MaxEvolutions)
        {
            int bestIdx = RunPopulation(population);
            ref Candidate<GeneticHashSpec> popBest = ref population[bestIdx];

            if (popBest.Fitness > top1.Fitness)
                top1 = popBest;

            //We early exit on stagnant improvements, but we got to have enough data to correctly determine it
            if (analyzerConfig.StagnantTerminate && evolution > recent.Length)
            {
                double avg = recent.Average();

                if (Math.Abs(popBest.Fitness - avg) <= analyzerConfig.StagnantPercent)
                    break;
            }

            recent[evolution % recent.Length] = popBest.Fitness;
            SelectAndMutate(population);
        }

        return top1;
    }

    private void MutateSpec(ref GeneticHashSpec spec)
    {
        // Length and offset is constrained:
        // - Offset must be within [0..MinLen] where MinLen is the length of the shortest string
        // - Length must be within the remainder of the string. If MinLen is 5, offset is 2, then Length must be within [1..2]

        spec.MixerSeed = _rng.Next();
        spec.MixerIterations = _rng.Next(0, 16);
        spec.AvalancheSeed = _rng.Next();
        spec.AvalancheIterations = _rng.Next(0, 16);

        int numSegments = _rng.Next(0, _segments.Length);

        StringSegment[] segments = new StringSegment[numSegments];

        for (int i = 0; i < numSegments; i++)
            segments[i] = _segments[_rng.Next(0, _segments.Length)];

        spec.Segments = segments;
    }

    private static void Cross(ref GeneticHashSpec a, ref GeneticHashSpec b)
    {
        b.MixerSeed = a.MixerSeed;
        b.MixerIterations = a.MixerIterations;
        b.AvalancheSeed = a.AvalancheSeed;
        b.AvalancheIterations = a.AvalancheIterations;
        b.Segments = a.Segments;
    }

    private int RunPopulation(Candidate<GeneticHashSpec>[] population)
    {
        double maxFit = double.MinValue;
        int bestIdx = 0;

        for (int i = 0; i < population.Length; i++)
        {
            // We ref it to update fitness
            ref Candidate<GeneticHashSpec> wrapper = ref population[i];
            simulation(data, analyzerConfig, ref wrapper);

            // We keep a running max
            if (wrapper.Fitness > maxFit)
            {
                maxFit = wrapper.Fitness;
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    private void SelectAndMutate(Candidate<GeneticHashSpec>[] population)
    {
        int[] indexes = new int[population.Length];
        for (int i = 0; i < indexes.Length; i++)
            indexes[i] = i;

        int Comparer(int a, int b) => population[b].Fitness.CompareTo(population[a].Fitness);
        Array.Sort(indexes, 0, indexes.Length, Comparer<int>.Create(Comparer));

        int topCount = (int)(analyzerConfig.PopulationSize * analyzerConfig.CrossPercent);
        int max = analyzerConfig.CrossEliteOnly ? topCount : indexes.Length;

        for (int i = 0; i < topCount; i++)
        {
            // Cross the elite with a random from population
            ref Candidate<GeneticHashSpec> a = ref population[indexes[i]];
            ref Candidate<GeneticHashSpec> b = ref population[indexes[_rng.Next(0, max)]];
            Cross(ref a.Spec, ref b.Spec);
        }

        for (int i = topCount; i < population.Length; i++)
        {
            ref Candidate<GeneticHashSpec> item = ref population[indexes[i]];
            MutateSpec(ref item.Spec);
        }
    }
}
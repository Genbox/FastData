using BenchmarkDotNet.Order;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class GeneticSelectionBenchmarks
{
    private readonly StaticArray<Entity> _population = new StaticArray<Entity>(100);

    [GlobalSetup]
    public void Setup()
    {
        Random rand = new Random(42);

        for (int i = 0; i < 100; i++)
        {
            Entity entity = new Entity([]) { Fitness = rand.NextDouble() };
            _population.Add(ref entity);
        }
    }

    [Benchmark]
    public void BoltzmannSelection() => new BoltzmannSelection(10.0, SharedRandom.Instance).Process(_population, [], 100);

    [Benchmark]
    public void EliteSelection() => new EliteSelection(0.5).Process(_population, [], 100);

    [Benchmark]
    public void RandomSelection() => new RandomSelection(false, SharedRandom.Instance).Process(_population, [], 100);

    [Benchmark]
    public void RankSelection() => new RankSelection(SharedRandom.Instance).Process(_population, [], 100);

    [Benchmark]
    public void RouletteWheelSelection() => new RouletteWheelSelection(SharedRandom.Instance).Process(_population, [], 100);

    [Benchmark]
    public void StochasticUniversalSamplingSelection() => new StochasticUniversalSamplingSelection(SharedRandom.Instance).Process(_population, [], 100);

    [Benchmark]
    public void TournamentSelection() => new TournamentSelection(2, SharedRandom.Instance).Process(_population, [], 100);
}
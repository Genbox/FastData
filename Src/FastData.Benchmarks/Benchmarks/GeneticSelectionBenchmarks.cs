using BenchmarkDotNet.Order;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Genetic.Selection;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class GeneticSelectionBenchmarks
{
    private Candidate<GeneticHashSpec>[] _population;

    [GlobalSetup]
    public void Setup()
    {
        Random rand = new Random(42);
        _population = Enumerable.Range(0, 100)
                                .Select(_ => new Candidate<GeneticHashSpec> { Fitness = rand.NextDouble() })
                                .ToArray();
    }

    [Benchmark]
    public int BoltzmannSelection() => new BoltzmannSelection(10.0, 42).Select(0, _population).Count();

    [Benchmark]
    public int RandomSelection() => new RandomSelection(false, 42).Select(0, _population).Count();

    [Benchmark]
    public int RankSelection() => new RankSelection(42).Select(0, _population).Count();

    [Benchmark]
    public int RouletteWheelSelection() => new RouletteWheelSelection(42).Select(0, _population).Count();

    [Benchmark]
    public int StochasticUniversalSamplingSelection() => new StochasticUniversalSamplingSelection(42).Select(0, _population).Count();

    [Benchmark]
    public int TournamentSelection() => new TournamentSelection(42).Select(0, _population).Count();

    [Benchmark]
    public int TruncationSelection() => new TruncationSelection(0.5).Select(0, _population).Count();
}